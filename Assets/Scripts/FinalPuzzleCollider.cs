using UnityEngine;

public class FinalPuzzleCollider : MonoBehaviour
{
    [SerializeField] private FinalPuzzleHandler handler;
    public GameObject ui;
    public bool playerInside;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!handler.puzzleComplete)
            if (collision.CompareTag("Player"))
                ui.SetActive(true);

        if (collision.CompareTag("Player"))
            playerInside = true;

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ui.SetActive(false);
            playerInside = false;
        }
    }
}
