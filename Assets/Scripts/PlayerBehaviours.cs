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
    [SerializeField] private WiresHandler wirePuzzle;

    // Network synced variables
    private Vector2 networkInput;
    private Vector2 networkLastMoveDirection;
    private bool networkFacingRight = true;
    private Vector3 networkScale;

    public bool myPuzzleActive;
    public bool wirePuzzleActive;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Only enable physics and input for local player
        if (!photonView.IsMine)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;// Disable physics for remote players
        }
    }
    void Start()
    {
        myPuzzleActive = false;
        anim = GetComponent<Animator>();
        networkScale = transform.localScale;

        // Find puzzle handlers based on character selection
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (puzzleType == 1) // Alex - FinalPuzzleHandler
        {
            finalPuzzle = FindFirstObjectByType<FinalPuzzleHandler>();
            wirePuzzle = FindFirstObjectByType<WiresHandler>();
        }
        else // Morfeus - SecondPlayerFinalPuzzleHandler
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

    void PlayerMovement()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        // Update puzzle references if null
        if (puzzleType == 1 && finalPuzzle == null)
            finalPuzzle = FindFirstObjectByType<FinalPuzzleHandler>();

        if (puzzleType == 1 && wirePuzzle == null)
            wirePuzzle = FindFirstObjectByType<WiresHandler>();

        if (puzzleType == 2 && secondPlayerFinalPuzzle == null)
            secondPlayerFinalPuzzle = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();

        // Check puzzle state with null checks - MORE ROBUST CHECK
        bool wasPuzzleActive = myPuzzleActive;

        // Check if ANY puzzle is active (including skill check)
        bool isAnyPuzzleActive = false;
        bool isWirePuzzleActive = false;

        if (puzzleType == 1 && finalPuzzle != null)
        {
            isAnyPuzzleActive = finalPuzzle.isPuzzleActive;
        }
        else if (puzzleType == 2 && secondPlayerFinalPuzzle != null)
        {
            // Check both puzzle active AND skill check active for Player 2
            isAnyPuzzleActive = secondPlayerFinalPuzzle.isPuzzleActive || secondPlayerFinalPuzzle.isSkillCheckActive;
        }

        if (puzzleType == 1 && wirePuzzle != null)
            isWirePuzzleActive = wirePuzzle.isPuzzleActive;

        myPuzzleActive = isAnyPuzzleActive;
        wirePuzzleActive = isWirePuzzleActive;

        bool shouldBlockMovement = myPuzzleActive || wirePuzzleActive;

        if (wasPuzzleActive != myPuzzleActive)
        {
            Debug.Log($"Puzzle active state changed: {myPuzzleActive}, Wire: {wirePuzzleActive}, Block Movement: {shouldBlockMovement}");
        }

        if (!shouldBlockMovement)
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
            rb.linearVelocity = Vector2.zero; // Ensure physics stops too
            Debug.Log($"Movement blocked - Puzzle: {myPuzzleActive}, Wire: {wirePuzzleActive}");
        }
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            rb.linearVelocity = input * speed;
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