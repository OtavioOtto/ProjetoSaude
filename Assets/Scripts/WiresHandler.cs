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

    [Header("Outside Objects")]
    [SerializeField] private WirePuzzleCollider puzzleCollider;
    [SerializeField] private AudioSource finalPuzzleSfx;

    private const string PUZZLE_COMPLETE_KEY = "WirePuzzleComplete";

    private void Start()
    {
        incompletePanel.SetActive(true);
        completePanel.SetActive(false);
        puzzleComplete = false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PUZZLE_COMPLETE_KEY))
        {
            bool isComplete = (bool)PhotonNetwork.CurrentRoom.CustomProperties[PUZZLE_COMPLETE_KEY];
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

        if (!puzzleComplete && blueWire.connected && yellowWire.connected && redWire.connected && greenWire.connected)
        {
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {

        Hashtable props = new Hashtable();
        props[PUZZLE_COMPLETE_KEY] = true;
        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        if (photonView != null)
        {
            photonView.RPC("RPC_CompletePuzzle", RpcTarget.AllBuffered);
        }


        ApplyPuzzleCompletion();

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType == 1)
        {
            wirePuzzle.SetActive(false);
        }
    }

    [PunRPC]
    void RPC_CompletePuzzle()
    {
        ApplyPuzzleCompletion();

        photonView.RPC("RPC_DeactivatePathBlock", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_DeactivatePathBlock()
    {
        if (pathBlock != null)
        {
            finalPuzzleSfx.Play();
            pathBlock.SetActive(false);
        }
    }

    void ApplyPuzzleCompletion()
    {

        if (puzzleComplete){ return; }

        puzzleComplete = true;
        isPuzzleActive = false;


        if (incompletePanel != null)
        {
            incompletePanel.SetActive(false);
        }

        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }

    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {

        if (propertiesThatChanged.ContainsKey(PUZZLE_COMPLETE_KEY))
        {
            bool isComplete = (bool)propertiesThatChanged[PUZZLE_COMPLETE_KEY];

            if (isComplete && !puzzleComplete)
            {
                ApplyPuzzleCompletion();
            }
        }
    }
}