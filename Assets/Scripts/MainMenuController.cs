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
    private AudioSource musicBg;

    void Start()
    {
        musicBg = GetComponent<AudioSource>();

        if (NetworkManager.Instance == null)
            return;

        startButton.onClick.AddListener(StartCutscene);
        controlsButton.onClick.AddListener(ShowControls);

        if (!PhotonNetwork.IsConnected)
            NetworkManager.Instance.ConnectToPhoton();

        else if (!PhotonNetwork.InLobby)
            NetworkManager.Instance.RejoinLobbyAfterReset();

        UpdateConnectionStatus();

        errorPanel.SetActive(false);

        lastConnectionCheckTime = Time.time;
    }

    void Update()
    {
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

        switch (NetworkManager.Instance.GetCurrentState())
        {
            case NetworkManager.ConnectionState.InLobby:
                connectionStatusText.color = Color.green;
                startButton.interactable = true;
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.ConnectedToMaster:
                connectionStatusText.color = Color.yellow;
                connectionStatusText.text = "Status: Connected"; 
                startButton.interactable = true; 
                loadingIndicator.SetActive(false);
                break;

            case NetworkManager.ConnectionState.Connecting:
                connectionStatusText.color = Color.yellow;
                startButton.interactable = false;
                loadingIndicator.SetActive(true);
                break;

            case NetworkManager.ConnectionState.InRoom:
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
        musicBg.Stop();
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
            ShowError("Not connected yet. Please wait...");

            NetworkManager.Instance.ConnectToPhoton();

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

    public void RetryConnection()
    {
        errorPanel.SetActive(false);
        NetworkManager.Instance.ConnectToPhoton();
        UpdateConnectionStatus();
    }
}