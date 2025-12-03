using Photon.Pun;
using UnityEngine;

public class ReactivateEnergyHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private float speed = 2;
    public bool puzzleComplete;
    public bool puzzleActive;
    private Vector2 input;
    public PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        // Apenas Morfeus executa
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType != 1)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {

        if (photonView.IsMine && puzzleActive && !puzzleComplete)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            input.x = moveX;
            input.y = moveY;
            input.Normalize();

            photonView.RPC("SendMovementToMap", RpcTarget.Others, input);
        }
    }

    [PunRPC]
    void SendMovementToMap(Vector2 movementInput)
    {
        MapPuzzleHandler mapHandler = FindFirstObjectByType<MapPuzzleHandler>();
        if (mapHandler != null)
        {
            mapHandler.ReceiveMovementInput(movementInput);
        }
    }

    public void FinishPuzzle() 
    {
        photonView.RPC("EndPuzzle", RpcTarget.All);
    }

    [PunRPC]
    private void EndPuzzle() 
    {
        puzzleActive = false;
        puzzleComplete = true;
        GameObject puzzle = GameObject.Find("ReactivateEnergyCover");
        puzzle.SetActive(false);
    }
}