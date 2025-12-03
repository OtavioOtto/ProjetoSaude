using Photon.Pun;
using UnityEngine;

public class MapPuzzleHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private GameObject colliderPuzzle;
    [SerializeField] private float speed = 2;
    public bool puzzleComplete;
    public bool puzzleActive;
    public GameObject puzzle;

    public PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        // Encontrar o Rigidbody do objeto no mapa
        if (playerRb == null)
        {
            GameObject mapPlayer = GameObject.Find("Morfeus");
            if (mapPlayer != null)
            {
                playerRb = mapPlayer.GetComponent<Rigidbody2D>();
            }
        }
    }

    // Método simples para receber movimento
    public void ReceiveMovementInput(Vector2 movementInput)
    {
        Debug.Log($"[Alex] Recebeu movimento: {movementInput}");
        if (playerRb != null)
        {
            playerRb.linearVelocity = movementInput * speed;
            Debug.Log($"[Alex] Aplicou velocidade: {movementInput * speed}");
        }
        else
        {
            Debug.LogWarning("[Alex] playerRb é null!");
        }
    }

    // Receber movimento via RPC
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
            puzzle.SetActive(false);
            colliderPuzzle.GetComponent<Collider2D>().enabled = false;
        }
    }
}