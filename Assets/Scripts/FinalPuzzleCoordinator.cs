using UnityEngine;
using Photon.Pun;

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
        Debug.Log($"Player {playerNumber} completed their puzzle. Total completed: {puzzlesCompleted}");

        if (puzzlesCompleted >= 2)
        {
            Debug.Log("Both puzzles completed! Final objective achieved!");
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
        Debug.Log("All puzzles stopped");
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

        Debug.Log("Skill check failed! Both puzzles reset.");
    }

    private void CheckPuzzleActivation()
    {
        bool shouldActivate = player1InPosition && player2InPosition && !bothPuzzlesActive;

        if (shouldActivate)
        {
            bothPuzzlesActive = true;
            puzzlesCompleted = 0; // Reset completion counter when starting new attempt

            // Activate Player 1's puzzle
            if (player1Puzzle != null)
            {
                player1Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            }

            // Activate Player 2's puzzle  
            if (player2Puzzle != null)
            {
                player2Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
            }

            Debug.Log("Both players in position - puzzles ACTIVATED!");
        }
        else if (!player1InPosition || !player2InPosition)
        {
            bothPuzzlesActive = false;
            // Don't stop puzzles here - let them complete or fail naturally
        }
    }


}