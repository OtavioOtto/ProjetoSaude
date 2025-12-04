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
            Debug.Log($"DistanceVolumeControl: Player puzzle type = {playerPuzzleType}");
        }
        else
        {
            Debug.LogWarning("NetworkManager not found. Disabling distance volume control.");
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

        if (playerClone == null)
        {
            Debug.LogWarning($"DistanceVolumeControl: Could not find player clone for puzzle type {playerPuzzleType}");
        }
        else
        {
            Debug.Log($"DistanceVolumeControl: Found player clone: {playerClone.name}");
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
                Debug.Log($"DistanceVolumeControl: AudioListener found on {playerClone.name}");
            }
            else
            {
                audioListenerTransform = playerClone.transform;
                Debug.Log($"DistanceVolumeControl: Using player transform as audio source (no AudioListener found)");
            }
        }
        else
        {
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener != null)
            {
                audioListenerTransform = listener.transform;
                Debug.Log("DistanceVolumeControl: Using first available AudioListener in scene");
            }
            else
            {
                Debug.LogWarning("No AudioListener found in the scene. Distance-based volume will not work.");
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