using Photon.Pun;
using UnityEngine;


public class MapPuzzleCollider : MonoBehaviour
{
    [SerializeField] private MapPuzzleHandler handler;
    [SerializeField] private GameObject warningTxt;
    public GameObject puzzle;
    public bool playerInside;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
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

            if (PuzzleEnergySyncManager.Instance != null)
            {
                PuzzleEnergySyncManager.Instance.LocalPlayerActivatedPuzzle(2);
            }
            GameObject player = GameObject.Find("AlexCraxy(Clone)");
            puzzle.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, puzzle.transform.position.z);
            playerInside = true;
            handler.puzzleActive = true;
            audioSource.Play();
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
