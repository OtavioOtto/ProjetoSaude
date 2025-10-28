// MainMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject errorPanel;

    private float lastConnectionCheckTime;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        UpdateConnectionStatus();

        // Hide error panel initially
        errorPanel.SetActive(false);

        lastConnectionCheckTime = Time.time;
    }

    void Update()
    {
        // Update connection status regularly
        if (Time.time - lastConnectionCheckTime > 1f)
        {
            UpdateConnectionStatus();
            lastConnectionCheckTime = Time.time;
        }
    }

    void UpdateConnectionStatus()
    {
        string status = NetworkManager.Instance.GetConnectionStatus();
        connectionStatusText.text = "Status: " + status;

        switch (NetworkManager.Instance.GetCurrentState())
        {
            case NetworkManager.ConnectionState.InLobby:
                connectionStatusText.color = Color.green;
                startButton.interactable = true;
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.ConnectedToMaster:
            case NetworkManager.ConnectionState.Connecting:
                connectionStatusText.color = Color.yellow;
                startButton.interactable = false;
                loadingIndicator.SetActive(true);
                break;

            case NetworkManager.ConnectionState.Disconnected:
            default:
                connectionStatusText.color = Color.red;
                startButton.interactable = false;
                loadingIndicator.SetActive(true);
                break;
        }
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