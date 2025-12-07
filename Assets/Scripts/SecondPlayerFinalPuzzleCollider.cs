using UnityEngine;
using Photon.Pun;
using System.Collections;

public class SecondPlayerFinalPuzzleCollider : MonoBehaviourPunCallbacks
{
    [SerializeField] private SecondPlayerFinalPuzzleHandler handler;
    [SerializeField] private GameObject warningTxt;
    public GameObject ui;
    public bool playerInside;

    private void Start()
    {
        ui.SetActive(false);
        if (handler == null)
        {
            handler = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();
        }

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType != 2)
        {
            enabled = false;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {

            if (puzzleType == 2)
            {

                if (handler != null && handler.photonView != null && !handler.puzzleComplete)
                {
                    ui.SetActive(true);
                    playerInside = true;

                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 2, true);
                    }

                    if (!handler.photonView.IsMine)
                    {
                        handler.photonView.RequestOwnership();
                    }
                }
            }
            else if (puzzleType == 1)
                warningTxt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (puzzleType == 2)
            {
                if (handler != null)
                {
                    ui.SetActive(false);
                    playerInside = false;

                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 2, false);
                    }

                    if (handler.photonView.IsMine)
                    {
                        handler.DeactivatePuzzle();
                    }
                }
            }

            else if (puzzleType == 1)
                warningTxt.SetActive(false);
        }
    }
}