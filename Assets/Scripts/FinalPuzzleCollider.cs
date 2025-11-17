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

        // Only enable for players who selected Alex (character1)
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType != 1) // Not Alex
        {
            enabled = false;
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        Debug.Log($"Trigger entered by: {collision.gameObject.name}, Tag: {collision.tag}, Puzzle type: {puzzleType}");
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (puzzleType == 1)
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
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            if (puzzleType == 1)
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