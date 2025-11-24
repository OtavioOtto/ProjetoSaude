// NetworkManager.cs
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private string gameSceneName = "GameScene";

    private Dictionary<int, string> selectedCharacters = new Dictionary<int, string>();


    private const string CHARACTER_MORFEUS = "Morfeus";
    private const string CHARACTER_ALEXCRAXY = "AlexCraxy";
    private bool triedToAddPhotonView = false;
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
        NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);

        // Check if there's already a NetworkManager in DontDestroyOnLoad
        NetworkManager persistentManager = null;
        foreach (var manager in managers)
        {
            if (manager.gameObject.scene.name == "DontDestroyOnLoad")
            {
                persistentManager = manager;
                break;
            }
        }

        // If there's already a persistent manager, destroy this new one
        if (persistentManager != null && persistentManager != this)
        {
            Debug.Log($"[NetworkManager] DESTROYING scene instance: {gameObject.name} (in scene: {gameObject.scene.name})");
            Destroy(gameObject);
            return;
        }

        // If we get here, this should become the persistent instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Debug.Log($"[NetworkManager] DESTROYING duplicate: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }
    private void Start()
    {
        // Periodic connection check
        StartCoroutine(ConnectionWatchdog());

        // Delayed duplicate cleanup as backup
        StartCoroutine(DelayedDuplicateCleanup());
    }

    private IEnumerator DelayedDuplicateCleanup()
    {
        yield return new WaitForSeconds(1f);
        CleanUpDuplicates();
    }

    private void CleanUpDuplicates()
    {
        NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);

        Debug.Log($"Found {managers.Length} NetworkManagers during cleanup");

        foreach (var manager in managers)
        {
            // Destroy any NetworkManager that is NOT this instance AND is in a normal scene (not DontDestroyOnLoad)
            if (manager != Instance && manager.gameObject.scene.name != "DontDestroyOnLoad")
            {
                Debug.Log($"Destroying scene NetworkManager: {manager.gameObject.name} (in scene: {manager.gameObject.scene.name})");
                //Destroy(manager.gameObject);
            }
            else if (manager != Instance)
            {
                Debug.Log($"Found duplicate but it's in DontDestroyOnLoad - keeping: {manager.gameObject.name}");
            }
        }
    }
    private void Update()
    {

        if (!triedToAddPhotonView && PhotonNetwork.IsConnected && GetComponent<PhotonView>() == null)
        {
            PhotonView pv = gameObject.AddComponent<PhotonView>();
            pv.ViewID = 999; //  ID fixo para o NetworkManager (não colide com players)
            triedToAddPhotonView = true;
            Debug.Log("[NetworkManager] PhotonView added with ViewID 999 to persistent instance.");
        }
        // Sync our internal state with Photon's actual state
        if (PhotonNetwork.IsConnected && currentState == ConnectionState.Disconnected)
        {
            // If we're actually connected but think we're disconnected, fix the state
            if (PhotonNetwork.InLobby)
            {
                currentState = ConnectionState.InLobby;
            }
            else if (PhotonNetwork.InRoom)
            {
                currentState = ConnectionState.InRoom;
            }
            else if (PhotonNetwork.IsConnected)
            {
                currentState = ConnectionState.ConnectedToMaster;
            }

            Debug.Log($"Fixed state mismatch: Photon={PhotonNetwork.NetworkClientState}, OurState={currentState}");
        }
    }
    private IEnumerator ConnectionWatchdog()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom &&
                currentState == ConnectionState.InRoom)
            {
                Debug.LogWarning("Unexpectedly left room, reconnecting...");
                currentState = ConnectionState.InLobby;
                PhotonNetwork.JoinLobby();
            }
        }
    }
    void OnDestroy()
    {
        // Unsubscribe from event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, Photon State: {PhotonNetwork.NetworkClientState}");

        if (scene.name == "MainMenu")
        {
            CleanUpDuplicates();

            // Reset state when returning to main menu
            if (!PhotonNetwork.IsConnected)
            {
                currentState = ConnectionState.Disconnected;
                wantsToJoinRoom = false;
                selectedCharacters.Clear();
                Debug.Log("Reset NetworkManager state for MainMenu");
            }
            else if (PhotonNetwork.InLobby)
            {
                currentState = ConnectionState.InLobby;
                Debug.Log("MainMenu loaded - connected to lobby");
            }
            else if (PhotonNetwork.IsConnected)
            {
                currentState = ConnectionState.ConnectedToMaster;
                Debug.Log("MainMenu loaded - connected to master server");
            }

        }
        else if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            currentState = ConnectionState.InRoom;
            Debug.Log("Game scene loaded - setting state to InRoom");
        }
    }

    private void CleanUpAllPersistentObjects()
    {
        Debug.Log("Cleaning up all persistent objects...");

        // Clean up FinalPuzzleCoordinator
        FinalPuzzleCoordinator[] coordinators = FindObjectsByType<FinalPuzzleCoordinator>(FindObjectsSortMode.None);
        foreach (var coordinator in coordinators)
        {
            if (coordinator != null && coordinator.gameObject != null)
            {
                Debug.Log($"Destroying FinalPuzzleCoordinator: {coordinator.gameObject.name}");
                Destroy(coordinator.gameObject);
            }
        }

        // Clean up DialogManager
        DialogManager[] dialogManagers = FindObjectsByType<DialogManager>(FindObjectsSortMode.None);
        foreach (var dm in dialogManagers)
        {
            if (dm != null && dm.gameObject != null && dm != DialogManager.Instance)
            {
                Debug.Log($"Destroying DialogManager: {dm.gameObject.name}");
                Destroy(dm.gameObject);
            }
        }

        // Clean up CharacterSelectionController
        CharacterSelectionController[] charControllers = FindObjectsByType<CharacterSelectionController>(FindObjectsSortMode.None);
        foreach (var controller in charControllers)
        {
            if (controller != null && controller.gameObject != null)
            {
                Debug.Log($"Destroying CharacterSelectionController: {controller.gameObject.name}");
                Destroy(controller.gameObject);
            }
        }
    }

    public IEnumerator HardResetCoroutine()
    {
        Debug.Log("=== HARD RESET STARTED ===");

        // Leave room if inside
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Leaving room...");
            PhotonNetwork.LeaveRoom();

            // Wait with timeout for room leave to complete
            float timeout = 3f;
            float elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!PhotonNetwork.InRoom)
            {
                Debug.Log("Successfully left room");
                currentState = ConnectionState.ConnectedToMaster;
            }
        }

        Debug.Log("Loading MainMenu scene");
        SceneManager.LoadScene("MainMenu");

        // Wait for scene to fully load BEFORE cleaning up objects
        yield return new WaitForSeconds(0.5f);

        // Clean up all persistent objects AFTER scene load
        CleanUpDuplicates();
        CleanUpAllPersistentObjects();

        // After scene load, ensure we rejoin lobby
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            Debug.Log("Rejoining lobby after reset...");
            PhotonNetwork.JoinLobby();

            // Wait a bit for lobby join to complete
            yield return new WaitForSeconds(1f);

            if (PhotonNetwork.InLobby)
            {
                currentState = ConnectionState.InLobby;
                Debug.Log("Successfully rejoined lobby");
            }
            else
            {
                Debug.LogWarning("Failed to rejoin lobby, but still connected to master");
                currentState = ConnectionState.ConnectedToMaster;
            }
        }
        else if (PhotonNetwork.IsConnected && PhotonNetwork.InLobby)
        {
            currentState = ConnectionState.InLobby;
            Debug.Log("Already in lobby after reset");
        }


        Debug.Log("=== HARD RESET COMPLETED ===");
    }

    public void RejoinLobbyAfterReset()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Not connected to Photon, connecting...");
            ConnectToPhoton();
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            currentState = ConnectionState.InLobby;
            Debug.Log("Already in lobby");
            return;
        }

        Debug.Log("Attempting to rejoin lobby...");
        PhotonNetwork.JoinLobby();
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

            // Determine puzzle type based on character name using constants
            int puzzleType = (characterName == CHARACTER_MORFEUS) ? 1 : 2;
            properties["PuzzleType"] = puzzleType;

            player.SetCustomProperties(properties);
        }

        // Check if all players have selected characters and we're ready to start the game
        CheckAllPlayersReady();
    }

    public int GetPuzzleTypeForPlayer(int actorNumber)
    {
        string character = GetSelectedCharacter(actorNumber);

        if (character == CHARACTER_MORFEUS)
        {
            return 1; // FinalPuzzleHandler - Morfeus
        }
        else
        {
            return 2; // SecondPlayerFinalPuzzleHandler - AlexCraxy
        }
    }

    public int GetLocalPlayerPuzzleType()
    {
        return GetPuzzleTypeForPlayer(PhotonNetwork.LocalPlayer.ActorNumber);
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
        yield return new WaitForSeconds(1f);

        // Clean up any duplicate CharacterSelectionController instances
        CharacterSelectionController[] controllers = FindObjectsByType<CharacterSelectionController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            if (controller != controllers[0]) // Keep the first one, destroy duplicates
            {
                Destroy(controller.gameObject);
            }
        }

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
        // This will create a new CharacterSelectionController instance
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
            MainMenuController menuController = FindFirstObjectByType<MainMenuController>();
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
            MainMenuController menuController = FindFirstObjectByType<MainMenuController>();
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

        // If a player leaves during character selection, reset transitioning flag
        CharacterSelectionController charSelection = FindFirstObjectByType<CharacterSelectionController>();
        if (charSelection != null && SceneManager.GetActiveScene().name == "CharacterSelection")
        {
            charSelection.SetTransitioning(false);
        }

        // Remove the player who left from selected characters
        if (selectedCharacters.ContainsKey(otherPlayer.ActorNumber))
        {
            selectedCharacters.Remove(otherPlayer.ActorNumber);
        }

        CheckAllPlayersReady(); // Update ready status
    }

    public override void OnLeftRoom()
    {
        Debug.Log($"OnLeftRoom called. Current scene: {SceneManager.GetActiveScene().name}");

        // Clear selections when leaving room
        selectedCharacters.Clear();

        // Only set to InLobby if we're actually going to stay in lobby
        // Don't automatically rejoin lobby if we're transitioning to game scene
        if (SceneManager.GetActiveScene().name == "MainMenu" || SceneManager.GetActiveScene().name == "CharacterSelection")
        {
            currentState = ConnectionState.InLobby;

            // Rejoin lobby when leaving room
            if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            // We're probably transitioning to game scene, don't change state
            Debug.Log("OnLeftRoom during game transition - keeping current state");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {

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