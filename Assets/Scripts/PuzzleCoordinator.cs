// PuzzleCoordinator.cs
using UnityEngine;
using Photon.Pun;

public class PuzzleCoordinator : MonoBehaviourPunCallbacks
{
    [Header("Puzzle References")]
    [SerializeField] private FinalPuzzleHandler player1Puzzle;
    [SerializeField] private SecondPlayerFinalPuzzleHandler player2Puzzle;

    public static PuzzleCoordinator Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        // Assign puzzles based on player number
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            if (playerNumber == 1)
            {
                player1Puzzle.gameObject.SetActive(true);
                player2Puzzle.gameObject.SetActive(false);
            }
            else
            {
                player1Puzzle.gameObject.SetActive(false);
                player2Puzzle.gameObject.SetActive(true);
            }
        }
    }

    [PunRPC]
    public void ReportPuzzleComplete(int puzzleType)
    {
        if (puzzleType == 1)
        {
            Debug.Log("Player 1 puzzle completed!");
        }
        else if (puzzleType == 2)
        {  
            Debug.Log("Player 2 puzzle completed!");
        }

    }


}