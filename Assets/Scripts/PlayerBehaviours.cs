using Photon.Pun;
using System.Linq;
using UnityEngine;

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

    [Header("Other Objects References")]
    [SerializeField] private FinalPuzzleHandler finalPuzzle;
    [SerializeField] private SecondPlayerFinalPuzzleHandler secondPlayerFinalPuzzle;
    [SerializeField] private WiresHandler wirePuzzle;
    [SerializeField] private MapPuzzleHandler mapPuzzle;
    [SerializeField] private ReactivateEnergyHandler energyPuzzle;
    [SerializeField] private GameObject uiDialog;

    private Vector2 networkInput;
    private Vector2 networkLastMoveDirection;
    private bool networkFacingRight = true;
    private Vector3 networkScale;
    private AudioSource footsteps;

    public bool myPuzzleActive;
    public bool wirePuzzleActive;
    public bool mapPuzzleActive;
    public bool energyPuzzleActive;
    private void Awake()
    {
        footsteps = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        if (!photonView.IsMine)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }   
            
    }
    void Start()
    {
        if (uiDialog == null)
            uiDialog = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "DialogCanvas");
        myPuzzleActive = false;
        anim = GetComponent<Animator>();
        networkScale = transform.localScale;

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (puzzleType == 1)
        {
            finalPuzzle = FindFirstObjectByType<FinalPuzzleHandler>();
            wirePuzzle = FindFirstObjectByType<WiresHandler>();
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
            PlayerMovement();
            if ((input.x < 0 && facingRight) || (input.x > 0 && !facingRight))
                Flip();
        }
        else
        {
            input = networkInput;
            lastMoveDirection = networkLastMoveDirection;

            if (transform.localScale != networkScale)
            {
                transform.localScale = networkScale;
                facingRight = networkFacingRight;
            }
        }
        if(rb.linearVelocity == new Vector2(0,0))
            footsteps.mute = true;

        Animate();
    }

    void PlayerMovement()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (puzzleType == 1 && finalPuzzle == null)
            finalPuzzle = FindFirstObjectByType<FinalPuzzleHandler>();

        if (puzzleType == 1 && wirePuzzle == null)
            wirePuzzle = FindFirstObjectByType<WiresHandler>();

        if (puzzleType == 1 && energyPuzzle == null)
            energyPuzzle = FindFirstObjectByType<ReactivateEnergyHandler>();

        if (puzzleType == 2 && secondPlayerFinalPuzzle == null)
            secondPlayerFinalPuzzle = FindFirstObjectByType<SecondPlayerFinalPuzzleHandler>();

        if (puzzleType == 2 && mapPuzzle == null)
            mapPuzzle = FindFirstObjectByType<MapPuzzleHandler>();

        bool wasPuzzleActive = myPuzzleActive;

        bool isAnyPuzzleActive = false;
        bool isWirePuzzleActive = false;
        bool isMapPuzzleActive = false;
        bool isEnergyPuzzleActive = false;

        if (puzzleType == 1 && finalPuzzle != null)
        {
            isAnyPuzzleActive = finalPuzzle.isPuzzleActive;
        }
        else if (puzzleType == 2 && secondPlayerFinalPuzzle != null)
        {
            isAnyPuzzleActive = secondPlayerFinalPuzzle.isPuzzleActive || secondPlayerFinalPuzzle.isSkillCheckActive;
        }

        if (puzzleType == 1 && wirePuzzle != null)
            isWirePuzzleActive = wirePuzzle.isPuzzleActive;

        if (puzzleType == 2 && mapPuzzle != null)
            isMapPuzzleActive = mapPuzzle.puzzleActive;

        if(puzzleType == 1 && energyPuzzle != null)
            isEnergyPuzzleActive = energyPuzzle.puzzleActive;

        myPuzzleActive = isAnyPuzzleActive;
        wirePuzzleActive = isWirePuzzleActive;
        mapPuzzleActive = isMapPuzzleActive;
        energyPuzzleActive = isEnergyPuzzleActive;
        bool shouldBlockMovement = myPuzzleActive || wirePuzzleActive || mapPuzzleActive || energyPuzzleActive;


        if (!shouldBlockMovement && !uiDialog.activeSelf)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if ((moveX == 0 && moveY == 0) && (input.x != 0 || input.y != 0))
            {
                lastMoveDirection = input;
            }
            footsteps.mute = false;
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            input.Normalize();
        }
        else
        {
            input = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
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

        networkScale = scale;
        networkFacingRight = facingRight;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(input);
            stream.SendNext(lastMoveDirection);
            stream.SendNext(facingRight);
            stream.SendNext(transform.localScale);
        }
        else
        {
            networkInput = (Vector2)stream.ReceiveNext();
            networkLastMoveDirection = (Vector2)stream.ReceiveNext();
            networkFacingRight = (bool)stream.ReceiveNext();
            networkScale = (Vector3)stream.ReceiveNext();
        }
    }
}