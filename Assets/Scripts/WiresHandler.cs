using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class WiresHandler : MonoBehaviourPunCallbacks
{
    [Header("Wire Scripts")]
    [SerializeField] private PoweredWireStats blueWire;
    [SerializeField] private PoweredWireStats redWire;
    [SerializeField] private PoweredWireStats yellowWire;
    [SerializeField] private PoweredWireStats greenWire;

    [Header("Game Objects")]
    [SerializeField] private GameObject wirePuzzle;
    [SerializeField] private GameObject incompletePanel;
    [SerializeField] private GameObject completePanel;
    [SerializeField] private GameObject pathBlock;

    [Header("Verifiers")]
    public bool puzzleComplete;
    public bool isPuzzleActive;

    [Header("Outside Scripts")]
    [SerializeField] private WirePuzzleCollider puzzleCollider;

    private const string PUZZLE_COMPLETE_KEY = "WirePuzzleComplete";

    private void Start()
    {
        incompletePanel.SetActive(true);
        completePanel.SetActive(false);
        puzzleComplete = false;
        Debug.Log($"WiresHandler Start - puzzleComplete: {puzzleComplete}");
        Debug.Log($"incompletePanel reference: {incompletePanel != null}");
        Debug.Log($"completePanel reference: {completePanel != null}");
        Debug.Log($"pathBlock reference: {pathBlock != null}");
        Debug.Log($"photonView reference: {photonView != null}");

        // Check if puzzle was already completed
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PUZZLE_COMPLETE_KEY))
        {
            bool isComplete = (bool)PhotonNetwork.CurrentRoom.CustomProperties[PUZZLE_COMPLETE_KEY];
            Debug.Log($"Room property found - PuzzleComplete: {isComplete}");
            if (isComplete)
            {
                ApplyPuzzleCompletion();
            }
        }
    }

    void Update()
    {
        if (puzzleCollider.playerInside && !puzzleComplete)
            isPuzzleActive = true;

        // Check local completion
        if (!puzzleComplete && blueWire.connected && yellowWire.connected && redWire.connected && greenWire.connected)
        {
            Debug.Log("Puzzle completed locally! Updating room properties...");
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {
        Debug.Log("CompletePuzzle called");

        // Update room properties
        Hashtable props = new Hashtable();
        props[PUZZLE_COMPLETE_KEY] = true;
        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log($"SetCustomProperties called. Success: {success}");

        // Use RPC
        if (photonView != null)
        {
            photonView.RPC("RPC_CompletePuzzle", RpcTarget.AllBuffered);
            Debug.Log("RPC called");
        }
        else
        {
            Debug.LogError("photonView is null!");
        }

        // Apply locally
        ApplyPuzzleCompletion();

        // Close puzzle UI if this player is the puzzle type
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        Debug.Log($"Player puzzle type: {puzzleType}");
        if (puzzleType == 1)
        {
            wirePuzzle.SetActive(false);
            Debug.Log("Wire puzzle UI closed");
        }
    }

    [PunRPC]
    void RPC_CompletePuzzle()
    {
        Debug.Log($"RPC_CompletePuzzle received by player {PhotonNetwork.LocalPlayer.ActorNumber}");
        ApplyPuzzleCompletion();

        // Also deactivate pathBlock via RPC
        photonView.RPC("RPC_DeactivatePathBlock", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_DeactivatePathBlock()
    {
        if (pathBlock != null)
        {
            pathBlock.SetActive(false);
            Debug.Log($"PathBlock deactivated via RPC for player: {PhotonNetwork.LocalPlayer.ActorNumber}");
        }
        else
        {
            Debug.LogError("pathBlock is null in RPC_DeactivatePathBlock!");
        }
    }

    void ApplyPuzzleCompletion()
    {
        Debug.Log($"ApplyPuzzleCompletion called. Current puzzleComplete: {puzzleComplete}");

        if (puzzleComplete)
        {
            Debug.Log("Puzzle already completed, skipping...");
            return;
        }

        puzzleComplete = true;
        isPuzzleActive = false;

        Debug.Log($"Setting incompletePanel active: false");
        Debug.Log($"Setting completePanel active: true");

        if (incompletePanel != null)
        {
            incompletePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("incompletePanel is null!");
        }

        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("completePanel is null!");
        }

        // Don't deactivate pathBlock here anymore - let RPC handle it
        Debug.Log($"Puzzle completion applied successfully for player: {PhotonNetwork.LocalPlayer.ActorNumber}");
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Debug.Log("OnRoomPropertiesUpdate called");

        if (propertiesThatChanged.ContainsKey(PUZZLE_COMPLETE_KEY))
        {
            bool isComplete = (bool)propertiesThatChanged[PUZZLE_COMPLETE_KEY];
            Debug.Log($"Room property updated - PuzzleComplete: {isComplete}");

            if (isComplete && !puzzleComplete)
            {
                Debug.Log("Applying puzzle completion from room properties");
                ApplyPuzzleCompletion();
            }
        }
    }
}