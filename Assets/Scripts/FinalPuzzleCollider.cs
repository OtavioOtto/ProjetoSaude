using UnityEngine;

public class FinalPuzzleCollider : MonoBehaviour
{
    public GameObject ui;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            ui.SetActive(true);

    }
}
