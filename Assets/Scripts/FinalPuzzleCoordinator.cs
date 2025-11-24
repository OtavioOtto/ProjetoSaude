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

    public int player1PuzzleViewID = 1;
    public int player2PuzzleViewID = 2;

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
            DontDestroyOnLoad(gameObject);

            // Ensure we have a PhotonView component
            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null)
            {
                pv = gameObject.AddComponent<PhotonView>();
                pv.ViewID = 999; // Use a fixed ViewID for the coordinator
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
        // Allow manual reset with 0 key (for testing)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master client forcing game end via 0 key");
                EndGame();
            }
            else
            {
                // Non-master client requests game end from master
                Debug.Log("Non-master client requesting game end via 0 key");
                photonView.RPC("RequestGameEnd", RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    void RequestGameEnd()
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

    void EndGame()
    {
        Debug.Log($"EndGame called by player {PhotonNetwork.LocalPlayer.ActorNumber} - Initiating complete game reset");

        // Reset coordinator state immediately
        player1InPosition = false;
        player2InPosition = false;
        bothPuzzlesActive = false;
        puzzlesCompleted = 0;

        // Notify all clients to reset - but don't try to call RPCs on puzzle objects
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("ResetAllPlayers", RpcTarget.All);
            Debug.Log("ResetAllPlayers RPC sent to all players");
        }
        else
        {
            Debug.Log("EndGame called but no valid photonView, resetting locally");
            // Fallback: reset locally
            ResetAllPlayers();
        }
    }

    [PunRPC]
    public void ResetAllPlayers()
    {
        Debug.Log($"ResetAllPlayers RPC received by player {PhotonNetwork.LocalPlayer?.ActorNumber} - Resetting game completely");

        // Stop trying to call RPCs on puzzle handlers - they're being destroyed anyway
        // Instead, just proceed with the main reset

        // Reset coordinator state locally
        player1InPosition = false;
        player2InPosition = false;
        bothPuzzlesActive = false;
        puzzlesCompleted = 0;

        // Use the NetworkManager's hard reset coroutine
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.HardResetCoroutine());
        }
        else
        {
            // Fallback: Load main menu directly if NetworkManager isn't available
            Debug.LogWarning("NetworkManager instance not found, loading main menu directly");
            SceneManager.LoadScene("MainMenu");
        }
    }

    void StopAllPuzzles()
    {
        // Player 1
        if (player1Puzzle != null && player1Puzzle.photonView != null && player1Puzzle.photonView.ViewID > 0)
        {
            player1Puzzle.photonView.RPC("StopPuzzle", RpcTarget.All);
        }

        // Player 2
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

        // Reset by PhotonView ID (most reliable method)
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

            // Use a coroutine to ensure both puzzles activate at the same time
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

        // Small delay to ensure both players are ready
        yield return new WaitForSeconds(0.3f);

        // Activate Player 2's puzzle first with ownership handling
        if (player2Puzzle != null && player2Puzzle.photonView != null)
        {


            player2Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            Debug.Log("Coordinator: Activated Player 2 puzzle");
        }

        // Small delay to ensure Player 2 puzzle is ready
        yield return new WaitForSeconds(0.2f);

        // Activate Player 1's puzzle
        if (player1Puzzle != null && player1Puzzle.photonView != null)
        {
            player1Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            Debug.Log("Coordinator: Activated Player 1 puzzle");
        }

        Debug.Log("Coordinator: Both puzzles activated");
    }




}