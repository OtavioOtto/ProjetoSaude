using Photon.Pun;
using UnityEngine;


public class MapPuzzleCollider : MonoBehaviour
{
    [SerializeField] private MapPuzzleHandler handler;
    [SerializeField] private GameObject warningTxt;
    public GameObject puzzle;
    public bool playerInside;

    void Start()
    {
        if (handler == null)
        {
            handler = FindFirstObjectByType<MapPuzzleHandler>();
        }

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (puzzleType != 2)
        {
            enabled = false;
            return;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();

        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType == 2 && handler != null && !handler.puzzleComplete)
        {
            puzzle.SetActive(true);

            // Sincronizar com o sync manager
            if (PuzzleEnergySyncManager.Instance != null)
            {
                PuzzleEnergySyncManager.Instance.LocalPlayerActivatedPuzzle(2);
            }

            playerInside = true;
            handler.puzzleActive = true;
        }
        else if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType != 2)
        {
            warningTxt.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType == 1)
            warningTxt.SetActive(false);
    }
}
