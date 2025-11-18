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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        // Allow manual reset with 0 key (for testing)
        if (Input.GetKeyDown(KeyCode.Alpha0) && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master client forcing game end via 0 key");
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
        // Notify all clients to reset
        photonView.RPC("ResetAllPlayers", RpcTarget.All);
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