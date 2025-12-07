using Photon.Pun;
using UnityEngine;

public class ReactivateEnergyCollider : MonoBehaviour
{
    [SerializeField] private ReactivateEnergyHandler handler;
    [SerializeField] private GameObject warningTxt;
    public GameObject puzzle;
    public bool playerInside;
    private AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (handler == null)
        {
            handler = FindFirstObjectByType<ReactivateEnergyHandler>();
        }

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (puzzleType != 1)
        {
            enabled = false;
            return;
        }
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType == 1 && handler != null && !handler.puzzleComplete)
        {
            puzzle.SetActive(true);

            if (PuzzleEnergySyncManager.Instance != null)
            {
                PuzzleEnergySyncManager.Instance.LocalPlayerActivatedPuzzle(1);
            }

            playerInside = true;
            handler.puzzleActive = true;
            audioSource.Play();
        }
        else if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType != 1)
        {
            warningTxt.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType == 2)
            warningTxt.SetActive(false);
    }
}
