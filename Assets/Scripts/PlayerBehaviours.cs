using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerBehaviours : MonoBehaviour
{
    [Header("Player Movement")]
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 input;

    [Header("Player Animations")]
    private Animator anim;
    private Vector2 lastMoveDirection;
    private bool facingLeft = true;

    [Header("Other Scripts")]
    [SerializeField] private FinalPuzzleHandler finalPuzzle;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }
    void Update()
    {
        PlayerMovement();
        Animate();
        if((input.x < 0 && !facingLeft) || (input.x  > 0 && facingLeft))
            Flip();
    }
    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }

    void PlayerMovement() 
    {
        if (!finalPuzzle.isPuzzleActive)
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
        else if (!finalPuzzle.puzzleComplete)
            speed = 0;


        if (finalPuzzle.puzzleComplete && speed != 5)
                speed = 5;

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
