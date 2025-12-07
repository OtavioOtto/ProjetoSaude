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

        NetworkManager persistentManager = null;
        foreach (var manager in managers)
        {
            if (manager.gameObject.scene.name == "DontDestroyOnLoad")
            {
                persistentManager = manager;
                break;
            }
        }

        if (persistentManager != null && persistentManager != this)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    private void Start()
    {
        StartCoroutine(ConnectionWatchdog());
    }


    private void Update()
    {

        if (!triedToAddPhotonView && PhotonNetwork.IsConnected && GetComponent<PhotonView>() == null)
        {
            PhotonView pv = gameObject.AddComponent<PhotonView>();
            pv.ViewID = 999;
            triedToAddPhotonView = true;
        }

        if (PhotonNetwork.IsConnected && currentState == ConnectionState.Disconnected)
        {
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
                currentState = ConnectionState.InLobby;
                PhotonNetwork.JoinLobby();
            }
        }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (scene.name == "MainMenu")
        {

            if (!PhotonNetwork.IsConnected)
            {
                currentState = ConnectionState.Disconnected;
                wantsToJoinRoom = false;
                selectedCharacters.Clear();
            }
            else if (PhotonNetwork.InLobby)
            {
                currentState = ConnectionState.InLobby;
            }
            else if (PhotonNetwork.IsConnected)
            {
                currentState = ConnectionState.ConnectedToMaster;
            }

        }
        else if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            currentState = ConnectionState.InRoom;
        }
    }

    private void CleanUpAllPersistentObjects()
    {

        FinalPuzzleCoordinator[] coordinators = FindObjectsByType<FinalPuzzleCoordinator>(FindObjectsSortMode.None);
        foreach (var coordinator in coordinators)
        {
            if (coordinator != null && coordinator.gameObject != null)
            {
                Destroy(coordinator.gameObject);
            }
        }

        DialogManager[] dialogManagers = FindObjectsByType<DialogManager>(FindObjectsSortMode.None);
        foreach (var dm in dialogManagers)
        {
            if (dm != null && dm.gameObject != null && dm != DialogManager.Instance)
            {
                Destroy(dm.gameObject);
            }
        }

        CharacterSelectionController[] charControllers = FindObjectsByType<CharacterSelectionController>(FindObjectsSortMode.None);
        foreach (var controller in charControllers)
        {
            if (controller != null && controller.gameObject != null)
            {
                Destroy(controller.gameObject);
            }
        }
    }

    public IEnumerator HardResetCoroutine()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();

            float timeout = 3f;
            float elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!PhotonNetwork.InRoom)
            {
                currentState = ConnectionState.ConnectedToMaster;
            }
        }

        SceneManager.LoadScene("MainMenu");

        yield return new WaitForSeconds(0.5f);

        CleanUpAllPersistentObjects();

        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();

            yield return new WaitForSeconds(1f);

            if (PhotonNetwork.InLobby)
            {
                currentState = ConnectionState.InLobby;
            }
            else
            {
                currentState = ConnectionState.ConnectedToMaster;
            }
        }
        else if (PhotonNetwork.IsConnected && PhotonNetwork.InLobby)
        {
            currentState = ConnectionState.InLobby;
        }

    }

    public void RejoinLobbyAfterReset()
    {
        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            currentState = ConnectionState.InLobby;
            return;
        }

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
        currentState = ConnectionState.ConnectedToMaster;
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        currentState = ConnectionState.InLobby;

        if (wantsToJoinRoom)
        {
            wantsToJoinRoom = false;
            CreateOrJoinRoomInternal();
        }
    }
    [PunRPC]
    private void RPC_SelectCharacter(int actorNumber, string characterName)
    {

        if (selectedCharacters.ContainsKey(actorNumber))
        {
            selectedCharacters[actorNumber] = characterName;
        }
        else
        {
            selectedCharacters.Add(actorNumber, characterName);
        }


        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null)
        {
            var properties = new ExitGames.Client.Photon.Hashtable();
            properties["Character"] = characterName;

            int puzzleType = (characterName == CHARACTER_MORFEUS) ? 1 : 2;
            properties["PuzzleType"] = puzzleType;

            player.SetCustomProperties(properties);
        }

        CheckAllPlayersReady();
    }

    public int GetPuzzleTypeForPlayer(int actorNumber)
    {
        string character = GetSelectedCharacter(actorNumber);

        if (character == CHARACTER_MORFEUS)
        {
            return 1;
        }
        else
        {
            return 2;
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


        if (playerCount >= 2 && selectedCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartGameAfterDelay());
        }
    }

    private IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        CharacterSelectionController[] controllers = FindObjectsByType<CharacterSelectionController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            if (controller != controllers[0])
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
            CreateOrJoinRoomInternal();
        }
        else
        {
            wantsToJoinRoom = true;

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
            wantsToJoinRoom = true;
            PhotonNetwork.JoinLobby();
        }
    }

    private IEnumerator WaitAndRetryRoomJoin()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

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
        currentState = ConnectionState.InRoom;
        wantsToJoinRoom = false;

        if (PhotonNetwork.LocalPlayer.CustomProperties["Character"] == null)
        {
            var properties = new ExitGames.Client.Photon.Hashtable();
            properties["Character"] = "";
            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }

        PhotonNetwork.LoadLevel("CharacterSelection");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        currentState = ConnectionState.InLobby;
        wantsToJoinRoom = false;

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            MainMenuController menuController = FindFirstObjectByType<MainMenuController>();
            if (menuController != null)
            {
                menuController.OnJoinRoomFailed(message);
            }
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        currentState = ConnectionState.InLobby;
        wantsToJoinRoom = false;

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

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {

        CharacterSelectionController charSelection = FindFirstObjectByType<CharacterSelectionController>();
        if (charSelection != null && SceneManager.GetActiveScene().name == "CharacterSelection")
        {
            charSelection.SetTransitioning(false);
        }

        if (selectedCharacters.ContainsKey(otherPlayer.ActorNumber))
        {
            selectedCharacters.Remove(otherPlayer.ActorNumber);
        }

        CheckAllPlayersReady();
    }

    public override void OnLeftRoom()
    {

        selectedCharacters.Clear();

        if (SceneManager.GetActiveScene().name == "MainMenu" || SceneManager.GetActiveScene().name == "CharacterSelection")
        {
            currentState = ConnectionState.InLobby;

            if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {

        currentState = ConnectionState.Disconnected;
        wantsToJoinRoom = false;

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