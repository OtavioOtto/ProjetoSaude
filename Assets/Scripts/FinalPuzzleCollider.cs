// FinalPuzzleCollider.cs (Updated)
using UnityEngine;
using Photon.Pun;

public class FinalPuzzleCollider : MonoBehaviourPunCallbacks
{
    [SerializeField] private FinalPuzzleHandler handler;
    public GameObject ui;
    public bool playerInside;

    private void Start()
    {
        // Ensure we have a reference to the handler
        if (handler == null)
        {
            handler = FindFirstObjectByType<FinalPuzzleHandler>();
        }

        // Only enable for Player 1
        if (PhotonNetwork.LocalPlayer.ActorNumber != 1)
        {
            enabled = false;
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                if (handler != null && !handler.puzzleComplete)
                {
                    ui.SetActive(true);
                    playerInside = true;

                    // Report to coordinator
                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 1, true);
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                if (handler != null)
                {
                    ui.SetActive(false);
                    playerInside = false;

                    // Report to coordinator
                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 1, false);
                    }
                }
            }
        }
    }
}