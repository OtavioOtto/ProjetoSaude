// SecondPlayerFinalPuzzleCollider.cs (Updated)
using UnityEngine;
using Photon.Pun;
using System.Collections;

public class SecondPlayerFinalPuzzleCollider : MonoBehaviourPunCallbacks
{
    [SerializeField] private SecondPlayerFinalPuzzleHandler handler;
    public GameObject ui;
    public bool playerInside;
    private bool ownershipRequestInProgress = false;

    private void Start()
    {
        if (handler == null)
        {
            handler = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();
        }

        // Only enable for Player 2
        if (PhotonNetwork.LocalPlayer.ActorNumber != 2)
        {
            enabled = false;
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

                    // Report to coordinator that Player 2 is in position
                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 2, true);
                        Debug.Log("Reported Player 2 in position to coordinator");
                    }
                    else
                    {
                        Debug.LogError("FinalPuzzleCoordinator instance not found!");
                    }

                    // Request ownership immediately when entering
                    if (!handler.photonView.IsMine)
                    {
                        Debug.Log("Player 2 requesting puzzle ownership on trigger enter");
                        handler.photonView.RequestOwnership();
                    }
                }
                else
                {
                    Debug.LogError("Cannot activate puzzle - handler issues");
                }
            }
        }
    }

    private IEnumerator OwnershipAndActivationProcess()
    {
        if (ownershipRequestInProgress) yield break;

        ownershipRequestInProgress = true;

        // If we already own it, just wait for coordinator to activate
        if (handler.photonView.IsMine)
        {
            ownershipRequestInProgress = false;
            yield break;
        }

        // Request ownership so we can handle activation when coordinator triggers it
        handler.photonView.RequestOwnership();

        // Wait for ownership with timeout
        float timeout = 2f;
        float elapsed = 0f;

        while (!handler.photonView.IsMine && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (handler.photonView.IsMine)
        {
        }
        else
        {

            // Fallback: Try to request activation via RPC (but coordinator should handle this)
            handler.photonView.RPC("RequestActivationFromOwner", RpcTarget.All);
        }

        ownershipRequestInProgress = false;
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

                    // Report to coordinator that Player 2 left position
                    if (FinalPuzzleCoordinator.Instance != null)
                    {
                        FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPlayerInPosition", RpcTarget.All, 2, false);
                    }

                    // Only deactivate if we own it
                    if (handler.photonView.IsMine)
                    {
                        handler.DeactivatePuzzle();
                    }
                }
            }
        }
    }

    // Optional: Add a method to check if both players are ready and activate manually
    // This can be useful for testing without the coordinator
    private void TryManualActivation()
    {
        if (handler != null && handler.photonView.IsMine && !handler.puzzleComplete)
        {
            handler.ActivatePuzzle();
        }
    }
}