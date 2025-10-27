// PlayerBehaviours.cs (Updated)
using UnityEngine;
using Photon.Pun;

public class PlayerBehaviours : MonoBehaviourPunCallbacks
{
    [Header("Player Movement")]
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 input;

    [Header("Player Animations")]
    private Animator anim;
    private Vector2 lastMoveDirection;
    private bool facingLeft = true;

    [Header("Puzzle References")]
    [SerializeField] private FinalPuzzleHandler finalPuzzle;
    [SerializeField] private SecondPlayerFinalPuzzleHandler secondPlayerFinalPuzzle;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Only enable for local player
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // Find puzzle handlers based on player number
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            finalPuzzle = FindObjectOfType<FinalPuzzleHandler>();
        }
        else
        {
            secondPlayerFinalPuzzle = FindObjectOfType<SecondPlayerFinalPuzzleHandler>();
        }
    }

    void Update()
    {
        PlayerMovement();
        Animate();
        if ((input.x < 0 && !facingLeft) || (input.x > 0 && facingLeft))
            Flip();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
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
        facingLeft = !facingLeft;
    }
}