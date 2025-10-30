// SecondPlayerFinalPuzzleCollider.cs
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

                    // Start ownership and activation process
                    StartCoroutine(OwnershipAndActivationProcess());
                    Debug.Log("Puzzle activation process started");
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

        // If we already own it, just activate
        if (handler.photonView.IsMine)
        {
            handler.ActivatePuzzle();
            ownershipRequestInProgress = false;
            yield break;
        }

        // Request ownership
        handler.photonView.RequestOwnership();
        Debug.Log("Ownership requested");

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
            Debug.Log("Ownership acquired successfully!");
            handler.ActivatePuzzle();
        }
        else
        {
            Debug.LogError($"Could not acquire ownership after {timeout} seconds. Current owner: {handler.photonView.Owner?.ActorNumber}");

            // Fallback: Try to activate anyway using RPC
            Debug.Log("Trying fallback activation via RPC");
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

                    // Only deactivate if we own it
                    if (handler.photonView.IsMine)
                    {
                        handler.DeactivatePuzzle();
                    }
                    Debug.Log("Puzzle deactivated");
                }
            }
        }
    }
}