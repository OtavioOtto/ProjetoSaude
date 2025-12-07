using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        string selectedCharacter = NetworkManager.Instance.GetLocalPlayerCharacter();

        if (!string.IsNullOrEmpty(selectedCharacter))
        {
            GameObject playerPrefab = null;
            foreach (var prefab in characterPrefabs)
            {
                if (prefab.name.Contains(selectedCharacter))
                {
                    playerPrefab = prefab;
                    break;
                }
            }

            if (playerPrefab != null)
            {
                int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
                Vector3 spawnPosition = spawnPoints[spawnIndex].position;


                PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            }
        }
        else
        {
            SpawnFallbackCharacter();
        }
    }

    private void SpawnFallbackCharacter()
    {
        if (characterPrefabs.Length > 0)
        {
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;

            GameObject fallbackPrefab = characterPrefabs[0];
            PhotonNetwork.Instantiate(fallbackPrefab.name, spawnPosition, Quaternion.identity);
        }
    }
}