using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalPuzzleCoordinator : MonoBehaviourPunCallbacks
{
    public static FinalPuzzleCoordinator Instance;

    [Header("Puzzle References")]
    public FinalPuzzleHandler player1Puzzle;
    public SecondPlayerFinalPuzzleHandler player2Puzzle;

    public int player1PuzzleViewID = 49;
    public int player2PuzzleViewID = 19;

    [Header("Puzzle States")]
    public bool player1InPosition;
    public bool player2InPosition;
    public bool bothPuzzlesActive;
    public int puzzlesCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null)
            {
                pv = gameObject.AddComponent<PhotonView>();
                pv.ViewID = 999;
                pv.OwnershipTransfer = OwnershipOption.Takeover;
                pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master client forcing game end via 0 key");
                EndGame();
            }
            else
            {
                Debug.Log("Non-master client requesting game end via 0 key");
                photonView.RPC("RequestGameEnd", RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    public void RequestGameEnd()
    {
        Debug.Log("Master client received game end request from another player");
        if (PhotonNetwork.IsMasterClient)
        {
            EndGame();
        }
    }
    [PunRPC]
    public void ReportPlayerInPosition(int playerNumber, bool inPosition)
    {
        if (playerNumber == 1)
            player1InPosition = inPosition;
        else if (playerNumber == 2)
            player2InPosition = inPosition;

        CheckPuzzleActivation();
    }

    [PunRPC]
    public void ReportPuzzleComplete(int playerNumber)
    {
        puzzlesCompleted++;

        Debug.Log($"Puzzle completed by Player {playerNumber}. Total completed: {puzzlesCompleted}");

        if (puzzlesCompleted >= 2)
        {
            Debug.Log("Both puzzles completed! Ending game...");
            EndGame();
        }
    }

    public void EndGame()
    {
        Debug.Log($"EndGame called by player {PhotonNetwork.LocalPlayer.ActorNumber} - Initiating complete game reset");

        player1InPosition = false;
        player2InPosition = false;
        bothPuzzlesActive = false;
        puzzlesCompleted = 0;

        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("ResetAllPlayers", RpcTarget.All);
            Debug.Log("ResetAllPlayers RPC sent to all players");
        }
        else
        {
            Debug.Log("EndGame called but no valid photonView, resetting locally");
            ResetAllPlayers();
        }
    }

    [PunRPC]
    public void ResetAllPlayers()
    {
        Debug.Log($"ResetAllPlayers RPC received by player {PhotonNetwork.LocalPlayer?.ActorNumber} - Resetting game completely");

        player1InPosition = false;
        player2InPosition = false;
        bothPuzzlesActive = false;
        puzzlesCompleted = 0;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.HardResetCoroutine());
        }
        else
        {
            Debug.LogWarning("NetworkManager instance not found, loading main menu directly");
            SceneManager.LoadScene("MainMenu");
        }
    }

    void StopAllPuzzles()
    {
        if (player1Puzzle != null && player1Puzzle.photonView != null && player1Puzzle.photonView.ViewID > 0)
        {
            player1Puzzle.photonView.RPC("StopPuzzle", RpcTarget.All);
        }

        if (player2Puzzle != null && player2Puzzle.photonView != null && player2Puzzle.photonView.ViewID > 0)
        {
            player2Puzzle.photonView.RPC("StopPuzzle", RpcTarget.All);
        }

        bothPuzzlesActive = false;
    }

    [PunRPC]
    public void ReportSkillCheckFailure()
    {
        Debug.Log("Coordinator: Resetting both puzzles due to skill check failure");

        PhotonView pv1 = PhotonView.Find(player1PuzzleViewID);
        if (pv1 != null)
        {
            pv1.RPC("ResetPuzzleProgress", RpcTarget.All);
            Debug.Log("Reset Player 1 puzzle via ViewID");
        }
        else
        {
            Debug.LogWarning($"Player 1 puzzle with ViewID {player1PuzzleViewID} not found");
        }

        PhotonView pv2 = PhotonView.Find(player2PuzzleViewID);
        if (pv2 != null)
        {
            pv2.RPC("ResetPuzzle", RpcTarget.All);
            Debug.Log("Reset Player 2 puzzle via ViewID");
        }
        else
        {
            Debug.LogWarning($"Player 2 puzzle with ViewID {player2PuzzleViewID} not found");
        }
    }

    private void CheckPuzzleActivation()
    {
        bool shouldActivate = player1InPosition && player2InPosition && !bothPuzzlesActive;
        bool shouldDeactivate = (!player1InPosition || !player2InPosition) && bothPuzzlesActive;

        if (shouldActivate)
        {
            bothPuzzlesActive = true;
            puzzlesCompleted = 0;

            Debug.Log("Coordinator: Activating both puzzles simultaneously");

            StartCoroutine(ActivateBothPuzzles());
        }
        else if (shouldDeactivate)
        {
            bothPuzzlesActive = false;
            StopAllPuzzles();
            Debug.Log("Coordinator: Player left - stopping all puzzles");
        }
    }

    private IEnumerator ActivateBothPuzzles()
    {
        Debug.Log("Coordinator: Starting simultaneous puzzle activation");

        yield return new WaitForSeconds(0.3f);

        if (player2Puzzle != null && player2Puzzle.photonView != null)
        {


            player2Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            Debug.Log("Coordinator: Activated Player 2 puzzle");
        }

        yield return new WaitForSeconds(0.2f);

        if (player1Puzzle != null && player1Puzzle.photonView != null)
        {
            player1Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            Debug.Log("Coordinator: Activated Player 1 puzzle");
        }

        Debug.Log("Coordinator: Both puzzles activated");
    }




}