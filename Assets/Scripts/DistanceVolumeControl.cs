using UnityEngine;

public class DistanceVolumeControl : MonoBehaviour
{
    public float maxDistance = 50f;
    private AudioSource audioSource;
    private Transform audioListenerTransform;
    private float defaultVolume;
    private int playerPuzzleType;
    private GameObject playerClone;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (NetworkManager.Instance != null)
        {
            playerPuzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        }
        else
        {
            enabled = false;
            return;
        }

        FindPlayerClone();
        SetupAudioListener();

        if (audioSource != null)
        {
            defaultVolume = audioSource.volume;
        }
    }

    void FindPlayerClone()
    {

        if (playerPuzzleType == 1)
        {
            if (playerClone == null)
            {
                playerClone = GameObject.Find("MorfeusClone)");
            }
        }
        else if (playerPuzzleType == 2)
        {
            if (playerClone == null)
            {
                playerClone = GameObject.Find("AlexCraxyClone)");
            }
        }

    }

    void SetupAudioListener()
    {
        if (playerClone != null)
        {
            AudioListener listener = playerClone.GetComponentInChildren<AudioListener>();

            if (listener != null)
            {
                audioListenerTransform = listener.transform;
            }
            else
            {
                audioListenerTransform = playerClone.transform;
            }
        }
        else
        {
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener != null)
            {
                audioListenerTransform = listener.transform;
            }
            else
            {
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (audioSource == null || audioListenerTransform == null) return;

        Vector2 audioSourcePos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 audioListenerPos2D = new Vector2(audioListenerTransform.position.x, audioListenerTransform.position.y);

        float distance = Vector2.Distance(audioSourcePos2D, audioListenerPos2D);

        float volume = defaultVolume - Mathf.Clamp01(distance / maxDistance);

        audioSource.volume = volume;

    }

}