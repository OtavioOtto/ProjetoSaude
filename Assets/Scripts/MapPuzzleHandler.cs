using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MapPuzzleHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private GameObject colliderPuzzle;
    [SerializeField] private float speed = 2;
    public bool puzzleComplete;
    public bool puzzleActive;
    public GameObject puzzle;

    public PhotonView photonView;

    [Header("Object References")]
    [SerializeField] private GameObject generatorOne;
    [SerializeField] private GameObject generatorTwo;
    [SerializeField] private GameObject wires;
    [SerializeField] private Light2D lightGlobal;
    

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (playerRb == null)
        {
            GameObject mapPlayer = GameObject.Find("Morfeus");
            if (mapPlayer != null)
            {
                playerRb = mapPlayer.GetComponent<Rigidbody2D>();
            }
        }
    }

    public void ReceiveMovementInput(Vector2 movementInput)
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = movementInput * speed;
        }
    }

    [PunRPC]
    void RPC_ReceiveMovement(Vector2 movementInput)
    {
        ReceiveMovementInput(movementInput);
    }
    public void SyncPuzzleCompletion() 
    {
        photonView.RPC("SyncPuzzleComplete", RpcTarget.All, true);
    }

    [PunRPC]
    void SyncPuzzleComplete(bool complete)
    {
        puzzleComplete = complete;
        if (complete)
        {
            lightGlobal.intensity = 1f;
            generatorOne.SetActive(false);
            generatorTwo.SetActive(true);
            wires.GetComponent<Collider2D>().enabled = true;
            puzzle.SetActive(false);
            colliderPuzzle.GetComponent<Collider2D>().enabled = false;
        }
    }
}