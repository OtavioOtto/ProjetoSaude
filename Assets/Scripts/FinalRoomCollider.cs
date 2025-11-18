using UnityEngine;
using Photon.Pun;

public class FinalRoomCollider : MonoBehaviourPunCallbacks
{
    [Header("Dialog Settings")]
    [SerializeField] private DialogManager.Dialog[] finalRoomDialog;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    private DialogManager dialogManager;
    private bool hasTriggeredGlobally = false;

    void Start()
    {
        dialogManager = DialogManager.Instance;

        // Only enable for players
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType == 0) // Not a valid player
        {
            enabled = false;
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggeredGlobally && triggerOnce) return;

        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            photonView.RPC("TriggerDialogGlobally", RpcTarget.All);
        }
    }

    [PunRPC]
    private void TriggerDialogGlobally()
    {
        if (hasTriggeredGlobally && triggerOnce) return;

        if (dialogManager != null && finalRoomDialog.Length > 0)
        {
            dialogManager.StartDialog(finalRoomDialog);
            hasTriggeredGlobally = true;
        }
    }

    // Method to reset the trigger (useful for testing)
    public void ResetTrigger()
    {
        hasTriggeredGlobally = false;
    }
}