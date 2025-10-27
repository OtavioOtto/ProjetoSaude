// PlayerSpawner.cs
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
            // Find the correct prefab for the selected character
            GameObject playerPrefab = null;
            foreach (var prefab in characterPrefabs)
            {
                if (prefab.name == selectedCharacter)
                {
                    playerPrefab = prefab;
                    break;
                }
            }

            if (playerPrefab != null)
            {
                // Determine spawn point based on player number
                int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
                if (spawnIndex >= spawnPoints.Length) spawnIndex = 0;

                Vector3 spawnPosition = spawnPoints[spawnIndex].position;

                // Instantiate the player
                PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogError($"Prefab for character {selectedCharacter} not found!");
            }
        }
        else
        {
            Debug.LogError("No character selected for local player!");
        }
    }
}