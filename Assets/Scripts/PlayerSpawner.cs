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
                // Use Contains instead of exact name match for flexibility
                if (prefab.name.Contains(selectedCharacter))
                {
                    playerPrefab = prefab;
                    break;
                }
            }

            if (playerPrefab != null)
            {
                // Determine spawn point based on player number
                int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
                Vector3 spawnPosition = spawnPoints[spawnIndex].position;

                Debug.Log($"Spawning {selectedCharacter} at position {spawnIndex} for player {PhotonNetwork.LocalPlayer.ActorNumber}");

                // Instantiate the player
                PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogError($"Prefab for character {selectedCharacter} not found! Available prefabs:");
                foreach (var prefab in characterPrefabs)
                {
                    Debug.LogError($" - {prefab.name}");
                }
            }
        }
        else
        {
            Debug.LogError("No character selected for local player!");

            // Fallback: spawn default character
            SpawnFallbackCharacter();
        }
    }

    private void SpawnFallbackCharacter()
    {
        // Fallback to first character if selection failed
        if (characterPrefabs.Length > 0)
        {
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;

            GameObject fallbackPrefab = characterPrefabs[0];
            PhotonNetwork.Instantiate(fallbackPrefab.name, spawnPosition, Quaternion.identity);
            Debug.LogWarning($"Spawned fallback character: {fallbackPrefab.name}");
        }
    }
}