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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                EndGame();
            }
            else
            {
                photonView.RPC("RequestGameEnd", RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    public void RequestGameEnd()
    {
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

        if (puzzlesCompleted >= 2)
        {
            EndGame();
        }
    }

    public void EndGame()
    {

        player1InPosition = false;
        player2InPosition = false;
        bothPuzzlesActive = false;
        puzzlesCompleted = 0;

        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("ResetAllPlayers", RpcTarget.All);
        }
        else
        {
            ResetAllPlayers();
        }
    }

    [PunRPC]
    public void ResetAllPlayers()
    {
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

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        StopAllPuzzles();

        puzzlesCompleted = 0;

        if (player1Puzzle != null)
        {
            player1Puzzle.ResetPuzzleProgress();
        }

        if (player2Puzzle != null)
        {
            player2Puzzle.ResetPuzzle();
        }

        photonView.RPC("ResetAllPuzzles", RpcTarget.Others);

        bothPuzzlesActive = false;

        ReactivateAfterReset();
    }

    private void ReactivateAfterReset()
    {
        if (player1InPosition && player2InPosition)
        {
            bothPuzzlesActive = true;
            StartCoroutine(ActivateBothPuzzles());
        }
    }

    [PunRPC]
    private void ResetAllPuzzles()
    {
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

            StartCoroutine(ActivateBothPuzzles());
        }
        else if (shouldDeactivate)
        {
            bothPuzzlesActive = false;
            StopAllPuzzles();
        }
    }

    private IEnumerator ActivateBothPuzzles()
    {

        yield return new WaitForSeconds(0.3f);

        if (player2Puzzle != null && player2Puzzle.photonView != null)
        {


            player2Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
        }

        yield return new WaitForSeconds(0.2f);

        if (player1Puzzle != null && player1Puzzle.photonView != null)
        {
            player1Puzzle.photonView.RPC("ForceActivatePuzzle", RpcTarget.All);
        }
    }




}