// CharacterSelectionController.cs (Updated)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

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
    [SerializeField] private string character1Name = "Alex";
    [SerializeField] private string character2Name = "Sam";

    private string selectedCharacter;
    private bool hasSelected = false;

    void Start()
    {
        Debug.Log("CharacterSelectionController Started");

        // Setup UI
        character1Text.text = character1Name;
        character2Text.text = character2Name;

        // Add button listeners
        character1Button.onClick.AddListener(() => SelectCharacter(character1Name));
        character2Button.onClick.AddListener(() => SelectCharacter(character2Name));

        // Check connection status
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

        // We're connected and in a room - show selection UI
        selectionPanel.SetActive(true);
        waitingPanel.SetActive(false);
        connectionErrorPanel.SetActive(false);

        UpdateCharacterAvailability();
    }

    void ShowConnectionError(string message)
    {
        selectionPanel.SetActive(false);
        waitingPanel.SetActive(false);
        connectionErrorPanel.SetActive(true);
        errorText.text = message;
        Debug.LogError(message);
    }

    void ReturnToMainMenu()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room in character selection scene");
        CheckConnectionStatus();
    }

    void SelectCharacter(string characterName)
    {
        if (hasSelected) return;

        // Check if character is available
        if (!NetworkManager.Instance.IsCharacterAvailable(characterName))
        {
            Debug.Log(characterName + " is already selected by another player!");
            return;
        }

        selectedCharacter = characterName;
        hasSelected = true;

        // Visual feedback
        UpdateSelectionUI();

        // Notify network
        NetworkManager.Instance.SelectCharacter(characterName);

        // Show waiting panel
        waitingPanel.SetActive(true);

        UpdateWaitingText();
    }

    void UpdateSelectionUI()
    {
        // Reset both buttons first
        character1Image.color = availableColor;
        character2Image.color = availableColor;
        character1Button.interactable = true;
        character2Button.interactable = true;

        // Update based on availability and selection
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

        Debug.Log("Updating character availability...");
        Debug.Log($"Character 1 available: {NetworkManager.Instance.IsCharacterAvailable(character1Name)}");
        Debug.Log($"Character 2 available: {NetworkManager.Instance.IsCharacterAvailable(character2Name)}");

        // Reset both buttons first
        character1Image.color = availableColor;
        character2Image.color = availableColor;
        character1Button.interactable = true;
        character2Button.interactable = true;

        // Update UI based on what characters are already taken
        if (!NetworkManager.Instance.IsCharacterAvailable(character1Name))
        {
            character1Image.color = takenColor;
            character1Button.interactable = false;
            Debug.Log($"Character 1 ({character1Name}) is taken");
        }

        if (!NetworkManager.Instance.IsCharacterAvailable(character2Name))
        {
            character2Image.color = takenColor;
            character2Button.interactable = false;
            Debug.Log($"Character 2 ({character2Name}) is taken");
        }

        // If we've already selected a character, update that too
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
        Debug.Log("Jogado entrou na sala: " + newPlayer.ActorNumber);
        UpdateCharacterAvailability();
        UpdateWaitingText();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Jogador saiu da sala: " + otherPlayer.ActorNumber);
        UpdateCharacterAvailability();
        UpdateWaitingText();
    }

    // Add this method to handle when player properties change (character selections)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"Player {targetPlayer.ActorNumber} properties updated");
        if (changedProps.ContainsKey("Character"))
        {
            string character = (string)changedProps["Character"];
            Debug.Log($"Player {targetPlayer.ActorNumber} selected character: {character}");
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

        // Optional: Update availability periodically to catch any sync issues
        if (Time.frameCount % 60 == 0) // Update every 60 frames
        {
            UpdateCharacterAvailability();
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowConnectionError("Disconnected: " + cause);
        Invoke("ReturnToMainMenu", 2f);
    }
}