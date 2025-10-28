// PlayerBehaviours.cs (Updated with proper flip sync)
using UnityEngine;
using Photon.Pun;

public class PlayerBehaviours : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Player Movement")]
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 input;

    [Header("Player Animations")]
    private Animator anim;
    private Vector2 lastMoveDirection;
    private bool facingRight = true;

    [Header("Puzzle References")]
    [SerializeField] private FinalPuzzleHandler finalPuzzle;
    [SerializeField] private SecondPlayerFinalPuzzleHandler secondPlayerFinalPuzzle;

    // Network synced variables
    private Vector2 networkInput;
    private Vector2 networkLastMoveDirection;
    private bool networkFacingRight = true;
    private Vector3 networkScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        networkScale = transform.localScale;

        // Only enable physics and input for local player
        if (!photonView.IsMine)
        {
            rb.isKinematic = true; // Disable physics for remote players
        }

        // Find puzzle handlers based on player number
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            finalPuzzle = FindFirstObjectByType<FinalPuzzleHandler>();
        }
        else
        {
            secondPlayerFinalPuzzle = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            // Local player: handle input and movement
            PlayerMovement();
            if ((input.x < 0 && facingRight) || (input.x > 0 && !facingRight))
                Flip();
        }
        else
        {
            // Remote player: use network synced values
            input = networkInput;
            lastMoveDirection = networkLastMoveDirection;

            // Apply the synced scale for flipping
            if (transform.localScale != networkScale)
            {
                transform.localScale = networkScale;
                facingRight = networkFacingRight;
            }
        }

        // Always update animations for all players
        Animate();
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            rb.linearVelocity = input * speed;
        }
    }

    void PlayerMovement()
    {
        // Check if current player's puzzle is active
        bool myPuzzleActive = false;

        if (PhotonNetwork.LocalPlayer.ActorNumber == 1 && finalPuzzle != null)
        {
            myPuzzleActive = finalPuzzle.isPuzzleActive;
        }
        else if (secondPlayerFinalPuzzle != null)
        {
            myPuzzleActive = secondPlayerFinalPuzzle.isPuzzleActive;
        }

        if (!myPuzzleActive)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if ((moveX == 0 && moveY == 0) && (input.x != 0 || input.y != 0))
            {
                lastMoveDirection = input;
            }

            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            input.Normalize();
        }
        else
        {
            // Puzzle is active - stop movement
            input = Vector2.zero;
        }
    }

    void Animate()
    {
        anim.SetFloat("MoveX", input.x);
        anim.SetFloat("MoveY", input.y);
        anim.SetFloat("MoveMagnitude", input.magnitude);
        anim.SetFloat("LastMoveX", lastMoveDirection.x);
        anim.SetFloat("LastMoveY", lastMoveDirection.y);
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        facingRight = !facingRight;

        // Update network scale so other players see the flip
        networkScale = scale;
        networkFacingRight = facingRight;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(input);
            stream.SendNext(lastMoveDirection);
            stream.SendNext(facingRight);
            stream.SendNext(transform.localScale);
        }
        else
        {
            // Network player, receive data
            networkInput = (Vector2)stream.ReceiveNext();
            networkLastMoveDirection = (Vector2)stream.ReceiveNext();
            networkFacingRight = (bool)stream.ReceiveNext();
            networkScale = (Vector3)stream.ReceiveNext();
        }
    }
}