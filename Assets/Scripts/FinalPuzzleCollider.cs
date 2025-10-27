// FinalPuzzleCollider.cs (Updated)
using UnityEngine;
using Photon.Pun;

public class FinalPuzzleCollider : MonoBehaviourPunCallbacks
{
    [SerializeField] private FinalPuzzleHandler handler;
    public GameObject ui;
    public bool playerInside;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            // Only activate for Player 1
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                if (!handler.puzzleComplete)
                    ui.SetActive(true);

                playerInside = true;
                handler.photonView.RequestOwnership();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                ui.SetActive(false);
                playerInside = false;
            }
        }
    }
}