using UnityEngine;

public class MapPuzzleHandler : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private GameObject puzzle;
    [SerializeField] private float speed = 2;
    private Vector2 input;
    void Start()
    {
        
    }

    void Update()
    {        
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input.Normalize();
        playerRb.linearVelocity = input * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerPuzzle"))
            puzzle.SetActive(false);
    }

}
