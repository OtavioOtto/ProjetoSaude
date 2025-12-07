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
    }
}