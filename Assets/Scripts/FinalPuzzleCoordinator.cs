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

    public int player1PuzzleViewID;
    public int player2PuzzleViewID;

    [Header("Puzzle States")]
    public bool player1InPosition;
    public bool player2InPosition;
    public bool bothPuzzlesActive;
    public int puzzlesCompleted;

    private void Awake()
    {

        FinalPuzzleHandler p1 = FindFirstObjectByType<FinalPuzzleHandler>();
        SecondPlayerFinalPuzzleHandler p2 = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();

        if (p1 != null && p1.photonView != null)
            player1PuzzleViewID = p1.photonView.ViewID;

        if (p2 != null && p2.photonView != null)
            player2PuzzleViewID = p2.photonView.ViewID;

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

    private void Start()
    {
        player2Puzzle.gameObject.SetActive(false);
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
        Debug.Log($"[Coordinator] ReportSkillCheckFailure received by player {PhotonNetwork.LocalPlayer.ActorNumber} (IsMaster: {PhotonNetwork.IsMasterClient})");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[Coordinator] ReportSkillCheckFailure was received by non-master client. Ignoring.");
            return;
        }

        Debug.Log("Coordinator: Resetting both puzzles due to skill check failure");

        // First, stop both puzzles
        StopAllPuzzles();

        // Reset the completion counter
        puzzlesCompleted = 0;

        // Reset Player 1 puzzle
        if (player1Puzzle != null)
        {
            Debug.Log("[Coordinator] Directly resetting Player uzzle");
            player1Puzzle.ResetPuzzleProgress();
        }

        // Reset Player 2 puzzle
        if (player2Puzzle != null)
        {
            Debug.Log("[Coordinator] Directly resetting Player 2 puzzle");
            player2Puzzle.ResetPuzzle();
        }

        // Also send RPCs for other clients
        photonView.RPC("ResetAllPuzzles", RpcTarget.Others);

        // CRITICAL: Reactivate both puzzles after reset
        // Set bothPuzzlesActive to false to allow reactivation
        bothPuzzlesActive = false;

        // Small delay to ensure reset is complete, then reactivate
        ReactivateAfterReset();
    }

    private void ReactivateAfterReset()
    {

        // Check if both players are still in position
        if (player1InPosition && player2InPosition)
        {
            Debug.Log("Coordinator: Reactivating puzzles after reset");
            bothPuzzlesActive = true;
            StartCoroutine(ActivateBothPuzzles());
        }
        else
        {
            Debug.Log("Coordinator: Players not in position, not reactivating");
        }
    }

    [PunRPC]
    private void ResetAllPuzzles()
    {
        // Reset local puzzle states
        puzzlesCompleted = 0;

        if (player1Puzzle != null)
            player1Puzzle.ResetPuzzleProgress();
        if (player2Puzzle != null)
            player2Puzzle.ResetPuzzle();
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