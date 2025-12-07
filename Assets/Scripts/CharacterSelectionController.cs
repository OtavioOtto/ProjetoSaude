using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionController : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject connectionErrorPanel;
    [SerializeField] private Button character1Button;
    [SerializeField] private Button character2Button;
    [SerializeField] private TMP_Text character1Text;
    [SerializeField] private TMP_Text character2Text;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Image character1Image;
    [SerializeField] private Image character2Image;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color takenColor = Color.red;

    [Header("Character Info")]
    [SerializeField] private string character1Name = "Morfeus";
    [SerializeField] private string character2Name = "AlexCraxy";

    private string selectedCharacter;
    private bool hasSelected = false;
    private bool isTransitioning = false;

    void Start()
    {

        character1Text.text = character1Name;
        character2Text.text = character2Name;

        character1Button.onClick.AddListener(() => SelectCharacter(character1Name));
        character2Button.onClick.AddListener(() => SelectCharacter(character2Name));

        StartCoroutine(DelayedConnectionCheck());
    }

    private IEnumerator DelayedConnectionCheck()
    {
        yield return new WaitForSeconds(0.5f);
        CheckConnectionStatus();
    }

    void CheckConnectionStatus()
    {
        if (!PhotonNetwork.IsConnected)
        {
            ShowConnectionError("Disconnected from server. Returning to main menu.");
            Invoke("ReturnToMainMenu", 2f);
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            ShowConnectionError("Not in a room. Returning to main menu.");
            Invoke("ReturnToMainMenu", 2f);
            return;
        }

        selectionPanel.SetActive(true);
        waitingPanel.SetActive(false);
        connectionErrorPanel.SetActive(false);

        UpdateCharacterAvailability();

    }
    public string GetCharacter1Name() { return character1Name; }
    public string GetCharacter2Name() { return character2Name; }


    void ShowConnectionError(string message)
    {
        selectionPanel.SetActive(false);
        waitingPanel.SetActive(false);
        connectionErrorPanel.SetActive(true);
        errorText.text = message;
    }

    void ReturnToMainMenu()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }

    public override void OnJoinedRoom()
    {
        CheckConnectionStatus();
    }

    void SelectCharacter(string characterName)
    {
        if (hasSelected) return;

        if (!NetworkManager.Instance.IsCharacterAvailable(characterName))
        {
            return;
        }

        selectedCharacter = characterName;
        hasSelected = true;

        UpdateSelectionUI();

        NetworkManager.Instance.SelectCharacter(characterName);

        waitingPanel.SetActive(true);

        UpdateWaitingText();
    }

    void UpdateSelectionUI()
    {
        character1Image.color = availableColor;
        character2Image.color = availableColor;
        character1Button.interactable = true;
        character2Button.interactable = true;

        if (selectedCharacter == character1Name)
        {
            character1Image.color = selectedColor;
            character1Button.interactable = false;
        }
        else if (!NetworkManager.Instance.IsCharacterAvailable(character1Name))
        {
            character1Image.color = takenColor;
            character1Button.interactable = false;
        }

        if (selectedCharacter == character2Name)
        {
            character2Image.color = selectedColor;
            character2Button.interactable = false;
        }
        else if (!NetworkManager.Instance.IsCharacterAvailable(character2Name))
        {
            character2Image.color = takenColor;
            character2Button.interactable = false;
        }
    }

    void UpdateCharacterAvailability()
    {
        if (!PhotonNetwork.InRoom) return;

        character1Image.color = availableColor;
        character2Image.color = availableColor;
        character1Button.interactable = true;
        character2Button.interactable = true;

        if (!NetworkManager.Instance.IsCharacterAvailable(character1Name))
        {
            character1Image.color = takenColor;
            character1Button.interactable = false;
        }

        if (!NetworkManager.Instance.IsCharacterAvailable(character2Name))
        {
            character2Image.color = takenColor;
            character2Button.interactable = false;
        }

        if (hasSelected)
        {
            if (selectedCharacter == character1Name)
            {
                character1Image.color = selectedColor;
                character1Button.interactable = false;
            }
            else if (selectedCharacter == character2Name)
            {
                character2Image.color = selectedColor;
                character2Button.interactable = false;
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateCharacterAvailability();
        UpdateWaitingText();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateCharacterAvailability();
        UpdateWaitingText();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Character"))
        {
            string character = (string)changedProps["Character"];
            UpdateCharacterAvailability();
            UpdateWaitingText();
        }
    }

    void UpdateWaitingText()
    {
        if (waitingPanel.activeInHierarchy && PhotonNetwork.InRoom)
        {
            int playersInRoom = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            int selectedCount = NetworkManager.Instance.GetSelectedCharacterCount();

            waitingText.text = $"Selecionou: {selectedCharacter}\n" +
                              $"Jogadores(as): {playersInRoom}/{maxPlayers}\n" +
                              $"Prontos(as): {selectedCount}/{maxPlayers}";
        }
    }

    void Update()
    {
        UpdateWaitingText();

        if (Time.frameCount % 60 == 0)
        {
            UpdateCharacterAvailability();
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowConnectionError("Disconnected: " + cause);
        Invoke("ReturnToMainMenu", 2f);
    }

    public void OnGameStarting()
    {
        isTransitioning = true;

        selectionPanel.SetActive(false);
        waitingPanel.SetActive(false);
        connectionErrorPanel.SetActive(false);
    }

    public void SetTransitioning(bool transitioning)
    {
        isTransitioning = transitioning;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    { 

        if (scene.name == "GameScene")
        {
            StartCoroutine(DestroyAfterDelay());
        }
        else if (scene.name == "MainMenu")
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);

        if (SceneManager.GetActiveScene().name != "CharacterSelection")
        {
            Destroy(gameObject);
        }
    }
}