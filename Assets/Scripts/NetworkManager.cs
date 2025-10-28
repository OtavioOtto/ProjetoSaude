// NetworkManager.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private string gameSceneName = "GameScene";

    private Dictionary<int, string> selectedCharacters = new Dictionary<int, string>();

    // Connection states
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        ConnectedToMaster,
        InLobby,
        InRoom
    }

    private ConnectionState currentState = ConnectionState.Disconnected;
    private bool wantsToJoinRoom = false;

    public static NetworkManager Instance { get; private set; }

    void Awake()
    {
        // Handle singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure we have a PhotonView component
            if (GetComponent<PhotonView>() == null)
            {
                gameObject.AddComponent<PhotonView>();
            }
        }
        else
        {
            Destroy(gameObject);
            return; // Add this return statement
        }

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        // Start connection process automatically
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            if (currentState == ConnectionState.ConnectedToMaster)
            {
                PhotonNetwork.JoinLobby();
            }
            return;
        }

        currentState = ConnectionState.Connecting;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        currentState = ConnectionState.ConnectedToMaster;
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby - Ready for room operations");
        currentState = ConnectionState.InLobby;

        // If we were waiting to join a room, do it now
        if (wantsToJoinRoom)
        {
            wantsToJoinRoom = false;
            CreateOrJoinRoomInternal();
        }
    }
    [PunRPC]
    private void RPC_SelectCharacter(int actorNumber, string characterName)
    {
        Debug.Log($"RPC_SelectCharacter called: Player {actorNumber} selected {characterName}");

        // Update the selected characters dictionary
        if (selectedCharacters.ContainsKey(actorNumber))
        {
            selectedCharacters[actorNumber] = characterName;
        }
        else
        {
            selectedCharacters.Add(actorNumber, characterName);
        }

        Debug.Log($"Player {actorNumber} selected character: {characterName}");

        // Also update the player's custom properties
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null)
        {
            var properties = new ExitGames.Client.Photon.Hashtable();
            properties["Character"] = characterName;
            player.SetCustomProperties(properties);
        }

        // Check if all players have selected characters and we're ready to start the game
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.InRoom) return;

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int selectedCount = GetSelectedCharacterCount();

        Debug.Log($"Players: {playerCount}, Selected: {selectedCount}");

        // If all players have selected characters, start the game
        if (playerCount >= 2 && selectedCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("All players ready! Starting game...");
            StartCoroutine(StartGameAfterDelay());
        }
    }

    private IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(1f); // Small delay to ensure everything is synced
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    public void CreateOrJoinRoom()
    {
        if (currentState == ConnectionState.InLobby)
        {
            // Ready to join room immediately
            CreateOrJoinRoomInternal();
        }
        else
        {
            // Not ready yet, set flag to join room when ready
            wantsToJoinRoom = true;

            // Make sure we're connecting
            if (currentState == ConnectionState.Disconnected)
            {
                ConnectToPhoton();
            }
            else if (currentState == ConnectionState.ConnectedToMaster)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    private void CreateOrJoinRoomInternal()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError($"Cannot create/join room - not connected and ready. State: {PhotonNetwork.NetworkClientState}");

            // Try again when we're ready
            StartCoroutine(WaitAndRetryRoomJoin());
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 2;
            roomOptions.EmptyRoomTtl = 1000;
            PhotonNetwork.JoinOrCreateRoom("CoopRoom", roomOptions, TypedLobby.Default);
        }
        else
        {
            Debug.LogError($"Cannot create/join room - not in lobby. Current state: {PhotonNetwork.NetworkClientState}");

            // Try to join lobby first, then retry room join
            wantsToJoinRoom = true;
            PhotonNetwork.JoinLobby();
        }
    }

    private IEnumerator WaitAndRetryRoomJoin()
    {
        // Wait until we're connected and ready
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

        // Small additional delay to ensure stability
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InLobby)
        {
            CreateOrJoinRoomInternal();
        }
        else
        {
            wantsToJoinRoom = true;
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        currentState = ConnectionState.InRoom;
        wantsToJoinRoom = false;

        // Initialize player properties if not set
        if (PhotonNetwork.LocalPlayer.CustomProperties["Character"] == null)
        {
            var properties = new ExitGames.Client.Photon.Hashtable();
            properties["Character"] = ""; // Empty initially
            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }

        // Load character selection scene for all players
        PhotonNetwork.LoadLevel("CharacterSelection");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to join room: " + message);
        currentState = ConnectionState.InLobby;
        wantsToJoinRoom = false;

        // Show error to user or try again
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            // Notify MainMenu about the failure
            MainMenuController menuController = FindObjectOfType<MainMenuController>();
            if (menuController != null)
            {
                menuController.OnJoinRoomFailed(message);
            }
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to create room: " + message);
        currentState = ConnectionState.InLobby;
        wantsToJoinRoom = false;

        // Show error to user or try again
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            MainMenuController menuController = FindObjectOfType<MainMenuController>();
            if (menuController != null)
            {
                menuController.OnJoinRoomFailed(message);
            }
        }
    }

    public void SelectCharacter(string characterName)
    {
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_SelectCharacter", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, characterName);
        }
    }



    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.ActorNumber} entered room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.ActorNumber} left the room");

        // Remove the player who left from selected characters
        if (selectedCharacters.ContainsKey(otherPlayer.ActorNumber))
        {
            selectedCharacters.Remove(otherPlayer.ActorNumber);
        }

        CheckAllPlayersReady(); // Update ready status
    }

    public override void OnLeftRoom()
    {
        // Clear selections when leaving room
        selectedCharacters.Clear();
        currentState = ConnectionState.InLobby;

        // Rejoin lobby when leaving room
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected from Photon: " + cause);
        currentState = ConnectionState.Disconnected;
        wantsToJoinRoom = false;

        // Return to main menu if we get disconnected
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public string GetSelectedCharacter(int actorNumber)
    {
        return selectedCharacters.ContainsKey(actorNumber) ? selectedCharacters[actorNumber] : null;
    }

    public string GetLocalPlayerCharacter()
    {
        return GetSelectedCharacter(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public bool IsCharacterAvailable(string characterName)
    {
        // Check if character is already selected by another player
        foreach (var selectedChar in selectedCharacters.Values)
        {
            if (selectedChar == characterName)
                return false;
        }
        return true;
    }

    public int GetSelectedCharacterCount()
    {
        return selectedCharacters.Count;
    }

    public bool IsReadyForRoomOperations()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;
    }

    // Helper method to get connection status for UI
    public string GetConnectionStatus()
    {
        return currentState.ToString();
    }

    public ConnectionState GetCurrentState()
    {
        return currentState;
    }

    public void SetWantsToJoinRoom(bool wantToJoin)
    {
        wantsToJoinRoom = wantToJoin;
    }

}