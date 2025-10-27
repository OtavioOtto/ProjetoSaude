// PlayerManager.cs
using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private GameObject cameraObject;
    [SerializeField] private MonoBehaviour[] playerScripts;

    private void Start()
    {
        if (photonView.IsMine)
        {
            // Enable camera and scripts for local player
            cameraObject.SetActive(true);
            foreach (var script in playerScripts)
            {
                script.enabled = true;
            }
        }
        else
        {
            // Disable for remote players
            cameraObject.SetActive(false);
            foreach (var script in playerScripts)
            {
                script.enabled = false;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Photon will handle position/rotation sync automatically
        // Add custom synchronization here if needed
    }
}