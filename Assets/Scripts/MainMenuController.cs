// MainMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private GameObject controlsCanvas;
    [SerializeField] private GameObject cutsceneUI;
    [SerializeField] private CutsceneController cutscene;

    private float lastConnectionCheckTime;

    void Start()
    {

        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[MainMenuController] NetworkManager.Instance is null — something went wrong.");
            return;
        }
        startButton.onClick.AddListener(StartCutscene);
        controlsButton.onClick.AddListener(ShowControls);

        // Auto-attempt connection/reconnection when returning to main menu
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("MainMenu loaded - not connected, attempting connection");
            NetworkManager.Instance.ConnectToPhoton();
        }
        else if (!PhotonNetwork.InLobby)
        {
            Debug.Log("MainMenu loaded - connected but not in lobby, rejoining lobby");
            NetworkManager.Instance.RejoinLobbyAfterReset();
        }
        else
        {
            Debug.Log("MainMenu loaded - already in lobby");
        }

        UpdateConnectionStatus();

        // Hide error panel initially
        errorPanel.SetActive(false);

        lastConnectionCheckTime = Time.time;
    }

    void Update()
    {
        // Update connection status regularly
        if (Time.time - lastConnectionCheckTime > 1f && !NetworkManager.Instance.GetConnectionStatus().Equals(NetworkManager.ConnectionState.InLobby))
        {
            UpdateConnectionStatus();
            lastConnectionCheckTime = Time.time;
        }

        if (controlsCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            controlsCanvas.SetActive(false);
    }

    void UpdateConnectionStatus()
    {
        string status = NetworkManager.Instance.GetConnectionStatus();
        connectionStatusText.text = "Status: " + status;

        // More accurate debug info
        Debug.Log($"UpdateConnectionStatus: UI Status={status}, Photon State={PhotonNetwork.NetworkClientState}, InRoom={PhotonNetwork.InRoom}, InLobby={PhotonNetwork.InLobby}");

        switch (NetworkManager.Instance.GetCurrentState())
        {
            case NetworkManager.ConnectionState.InLobby:
                connectionStatusText.color = Color.green;
                startButton.interactable = true;
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.ConnectedToMaster:
                connectionStatusText.color = Color.yellow;
                connectionStatusText.text = "Status: Connected"; // More user-friendly
                startButton.interactable = true; // Allow starting even if just connected to master
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.Connecting:
                connectionStatusText.color = Color.yellow;
                startButton.interactable = false;
                loadingIndicator.SetActive(true);
                break;

            case NetworkManager.ConnectionState.InRoom:
                // This shouldn't happen in main menu, but if it does, show appropriate status
                connectionStatusText.color = Color.blue;
                connectionStatusText.text = "Status: In Game";
                startButton.interactable = false;
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.Disconnected:
            default:
                connectionStatusText.color = Color.red;
                startButton.interactable = false;
                loadingIndicator.SetActive(true);
                break;
        }
    }
    public void StartCutscene() 
    {
        cutsceneUI.SetActive(true);
        cutscene.StartCutscene();
    }
    public void StartGame()
    {
        if (NetworkManager.Instance.IsReadyForRoomOperations())
        {
            startButton.interactable = false;
            connectionStatusText.text = "Joining Room...";
            loadingIndicator.SetActive(true);
            errorPanel.SetActive(false);

            NetworkManager.Instance.CreateOrJoinRoom();
        }
        else
        {
            Debug.LogError("Not ready for room operations yet! Status: " + NetworkManager.Instance.GetConnectionStatus() +
                          ", Photon State: " + PhotonNetwork.NetworkClientState);

            // Show error to user
            ShowError("Not connected yet. Please wait...");

            // Try to continue connection process
            NetworkManager.Instance.ConnectToPhoton();

            // Set flag to join room when ready
            NetworkManager.Instance.SetWantsToJoinRoom(true);
        }
    }

    public void ShowControls() {controlsCanvas.SetActive(true);}

    public void OnJoinRoomFailed(string message)
    {
        ShowError("Failed to join room: " + message);
        startButton.interactable = true;
        loadingIndicator.SetActive(false);
        UpdateConnectionStatus();
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorPanel.SetActive(true);

        // Auto-hide error after 3 seconds
        Invoke("HideError", 3f);
    }

    private void HideError()
    {
        errorPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Button handler for retrying connection
    public void RetryConnection()
    {
        errorPanel.SetActive(false);
        NetworkManager.Instance.ConnectToPhoton();
        UpdateConnectionStatus();
    }
}