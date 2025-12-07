using UnityEngine;
using Photon.Pun;

public class FinalRoomCollider : MonoBehaviourPunCallbacks
{
    [Header("Dialog Settings")]
    [SerializeField] private DialogManager.Dialog[] finalRoomDialog;

    [Header("Trigger Settings")]
    public bool triggerOnce = true;
    public bool hasTriggeredGlobally = false;

    private DialogManager dialogManager;

    void Start()
    {
        dialogManager = DialogManager.Instance;

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType == 0)
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

    public void ResetTrigger()
    {
        hasTriggeredGlobally = false;
    }
}