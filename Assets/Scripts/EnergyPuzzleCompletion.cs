using Photon.Pun;
using UnityEngine;

public class EnergyPuzzleCompletion : MonoBehaviour
{
    [SerializeField] private MapPuzzleHandler mapHandler;
    private ReactivateEnergyHandler energyHandler;
    [SerializeField] private AudioSource sfx;
    private void Start()
    {
        energyHandler = FindFirstObjectByType<ReactivateEnergyHandler>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerPuzzle") && mapHandler.puzzleActive)
        {
            mapHandler.puzzleComplete = true;
            mapHandler.puzzleActive = false;
            gameObject.GetComponent<Collider2D>().enabled = false;
            mapHandler.puzzle.SetActive(false);
            sfx.Play();

            if(energyHandler == null)
                energyHandler = FindFirstObjectByType<ReactivateEnergyHandler>();

            energyHandler.FinishPuzzle();
            mapHandler.SyncPuzzleCompletion();
        }
    }
}
