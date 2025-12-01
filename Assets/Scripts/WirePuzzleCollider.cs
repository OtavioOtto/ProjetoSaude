using Photon.Pun;
using Unity.XR.OpenVR;
using UnityEngine;

public class WirePuzzleCollider : MonoBehaviour
{
    [SerializeField] private WiresHandler handler;
    [SerializeField] private GameObject warningTxt;
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
        else if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType != 1)
            warningTxt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine && puzzleType == 2)
            warningTxt.SetActive(false);
    }
}
