// PlayerManager.cs (Simple fix)
using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private GameObject cameraObject;

    private void Start()
    {
        if (photonView.IsMine)
        {
            cameraObject.SetActive(true);
        }
        else
        {
            cameraObject.SetActive(false);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Empty implementation - we don't need to sync anything here
        // Photon Transform View handles position/rotation sync
    }
}