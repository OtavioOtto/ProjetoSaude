using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ReactivateEnergyHandler : MonoBehaviourPunCallbacks
{
    public bool puzzleComplete;
    public bool puzzleActive;
    private Vector2 input;
    public PhotonView photonView;

    private GameObject generatorOne;
    private GameObject generatorTwo;
    private GameObject wires;
    private Light2D lightGlobal;
    void Start()
    {
        photonView = GetComponent<PhotonView>();

        generatorOne = GameObject.Find("ReactivateEnergyPuzzle");
        generatorTwo = GameObject.Find("SecondPlayerFinalPuzzleCollider");
        wires = GameObject.Find("WirePuzzleCollider");
        generatorTwo.SetActive(false);
        lightGlobal = GameObject.Find("Global Light 2D").GetComponent<Light2D>();
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
        lightGlobal.intensity = 1f;
        generatorOne.SetActive(false);
        generatorTwo.SetActive(true);
        wires.GetComponent<Collider2D>().enabled = true;
        puzzleActive = false;
        puzzleComplete = true;
        GameObject puzzle = GameObject.Find("ReactivateEnergyCover");
        puzzle.SetActive(false);
    }
}