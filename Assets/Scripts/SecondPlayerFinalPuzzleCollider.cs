// SecondPlayerFinalPuzzleCollider.cs (Make sure it calls ActivatePuzzle)
using UnityEngine;
using Photon.Pun;

public class SecondPlayerFinalPuzzleCollider : MonoBehaviourPunCallbacks
{
    [SerializeField] private SecondPlayerFinalPuzzleHandler handler;
    public GameObject ui;
    public bool playerInside;

    private void Start()
    {
        if (handler == null)
        {
            handler = FindObjectOfType<SecondPlayerFinalPuzzleHandler>();
            Debug.Log("Handler found via FindObjectOfType: " + (handler != null));
        }

        if (handler == null)
        {
            Debug.LogError("SecondPlayerFinalPuzzleHandler not found!");
        }
        else if (handler.photonView == null)
        {
            Debug.LogError("Handler is missing PhotonView component!");
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber != 2)
        {
            enabled = false;
            Debug.Log("Collider disabled - not Player 2");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Trigger entered by: {collision.gameObject.name}, Tag: {collision.tag}");

        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            Debug.Log("Local player entered trigger");

            if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
            {
                Debug.Log("Player 2 entered puzzle area");

                if (handler != null && handler.photonView != null && !handler.puzzleComplete)
                {
                    ui.SetActive(true);
                    playerInside = true;
                    handler.photonView.RequestOwnership();
                    handler.ActivatePuzzle(); // MAKE SURE THIS IS CALLED!
                    handler.isPuzzleActive = true;
                    Debug.Log("Puzzle activation called");
                }
                else
                {
                    Debug.LogError("Cannot activate puzzle - handler issues");
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
            {
                if (handler != null)
                {
                    ui.SetActive(false);
                    playerInside = false;
                    handler.DeactivatePuzzle();
                    Debug.Log("Puzzle deactivated");
                }
            }
        }
    }
}