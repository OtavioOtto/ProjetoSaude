using Photon.Pun;
using UnityEngine;

public class WirePuzzleCollider : MonoBehaviour
{
    [SerializeField] private WiresHandler handler;
    public GameObject puzzle;
    public bool playerInside;
    void Start()
    {
        if (handler == null)
        {
            handler = FindFirstObjectByType<WiresHandler>();
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
            GameObject player = GameObject.Find("Morfeus(Clone)");
            puzzle.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, puzzle.transform.position.z);
            playerInside = true;
        }
    }
}
