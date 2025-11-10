using UnityEngine;
using Photon.Pun;
using System.Collections;

public class FinalPuzzleCoordinator : MonoBehaviourPunCallbacks
{
    public static FinalPuzzleCoordinator Instance;

    [Header("Puzzle References")]
    public FinalPuzzleHandler player1Puzzle;
    public SecondPlayerFinalPuzzleHandler player2Puzzle;

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

        if (puzzlesCompleted >= 2)
        {
            // Stop both puzzles
            StopAllPuzzles();
            // Trigger your win condition here
        }
        else
        {
            // If one puzzle completed, stop the other one too since they need to end together
            StopAllPuzzles();
        }
    }

    private void StopAllPuzzles()
    {
        // Stop Player 1's puzzle
        if (player1Puzzle != null)
        {
            player1Puzzle.photonView.RPC("StopPuzzle", RpcTarget.All);
        }

        // Stop Player 2's puzzle  
        if (player2Puzzle != null)
        {
            player2Puzzle.photonView.RPC("StopPuzzle", RpcTarget.All);
        }

        bothPuzzlesActive = false;
    }

    [PunRPC]
    public void ReportSkillCheckFailure()
    {
        // Reset both puzzles when Player 2 fails skill check
        if (player1Puzzle != null)
        {
            player1Puzzle.ResetPuzzleProgress();
        }

        if (player2Puzzle != null)
        {
            player2Puzzle.ResetPuzzle();
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

        // First, ensure Player 2 has ownership of their puzzle
        if (player2Puzzle != null && player2Puzzle.photonView != null)
        {
            // If Player 2 doesn't own their puzzle, request ownership
            if (!player2Puzzle.photonView.IsMine && PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == 2)
            {
                Debug.Log("Coordinator: Player 2 requesting ownership of their puzzle");
                player2Puzzle.photonView.RequestOwnership();
                yield return new WaitForSeconds(0.1f); // Small delay for ownership transfer
            }

            // Activate Player 2's puzzle
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