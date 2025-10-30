// SecondPlayerFinalPuzzleHandler.cs (Fixed with IPunObservable implementation)
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class SecondPlayerFinalPuzzleHandler : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Objects")]
    [SerializeField] private GameObject skillCheckBG;
    [SerializeField] private GameObject skillCheck;
    [SerializeField] private RectTransform pointer;
    [SerializeField] private RectTransform hitArea;
    [SerializeField] private Image successZone;

    [Header("Settings")]
    [SerializeField] private float pointerSpeed = 180f;
    [SerializeField] private float successZoneSize = 30f;
    [SerializeField] private float skillCheckFrequency = 3f;

    [Header("Verifiers")]
    public bool isPuzzleActive;
    public bool puzzleComplete;

    private bool isSkillCheckActive = false;
    private float currentAngle = 0f;
    private float successZoneAngle;
    private float lastSkillCheckTime = 0f;
    private float puzzleProgress = 0f;
    private bool firstTime;

    private PhotonView photonViews;

    void Awake()
    {
        photonViews = GetComponent<PhotonView>();

        // If PhotonView is null, try to find it
        if (photonViews == null)
        {
            photonViews = FindObjectOfType<PhotonView>();
            Debug.Log(photonView != null ? "Found PhotonView" : "PhotonView still null!");
        }
    }

    void Start()
    {
        firstTime = true;
        isPuzzleActive = false;
        puzzleComplete = false;

        // Debug all references
        Debug.Log($"SkillCheckBG: {skillCheckBG != null}");
        Debug.Log($"SkillCheck: {skillCheck != null}");
        Debug.Log($"Pointer: {pointer != null}");
        Debug.Log($"HitArea: {hitArea != null}");
        Debug.Log($"SuccessZone: {successZone != null}");

        if (successZone != null)
        {
            successZone.fillAmount = successZoneSize / 360f;
            Debug.Log($"Success zone fill amount set to: {successZone.fillAmount}");
        }
        else
        {
            Debug.LogError("SuccessZone Image is null!");
        }

        // Initially hide the skill check UI
        if (skillCheck != null)
            skillCheck.SetActive(false);

        Debug.Log("SecondPlayerFinalPuzzleHandler initialized");
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (PhotonNetwork.LocalPlayer == null || PhotonNetwork.LocalPlayer.ActorNumber != 2) return;

        // Force activate for testing
        if (Input.GetKeyDown(KeyCode.P) && !isPuzzleActive && !puzzleComplete)
        {
            Debug.Log("Force activating puzzle with P key");
            isPuzzleActive = true;
            puzzleProgress = 0f;
            lastSkillCheckTime = Time.time - skillCheckFrequency;
        }

        // Skill check activation
        if (isPuzzleActive && !puzzleComplete && !isSkillCheckActive)
        {
            float timeSinceLastCheck = Time.time - lastSkillCheckTime;

            if (timeSinceLastCheck > skillCheckFrequency || firstTime)
            {
                firstTime = false;
                Debug.Log($"Starting skill check! Time since last: {timeSinceLastCheck}");
                StartSkillCheck();
            }
        }

        if (isSkillCheckActive)
        {
            UpdateSkillCheck();
        }
    }

    void StartSkillCheck()
    {
        isSkillCheckActive = true;

        // Show the skill check UI
        if (skillCheck != null)
        {
            skillCheck.SetActive(true);
            Debug.Log("Skill check UI activated");
        }
        else
        {
            Debug.LogError("SkillCheck GameObject is null!");
        }

        // Randomize success zone position
        successZoneAngle = Random.Range(0f, 360f);
        if (successZone != null)
        {
            successZone.transform.rotation = Quaternion.Euler(0, 0, successZoneAngle);
            Debug.Log($"Success zone rotated to: {successZoneAngle} degrees");
        }
        else
        {
            Debug.LogError("SuccessZone is null - cannot rotate!");
        }

        // Reset pointer position
        currentAngle = 0f;
        if (pointer != null)
        {
            pointer.rotation = Quaternion.Euler(0, 0, currentAngle);
            Debug.Log("Pointer reset to 0 degrees");
        }
        else
        {
            Debug.LogError("Pointer is null - cannot rotate!");
        }

        lastSkillCheckTime = Time.time;
    }

    void UpdateSkillCheck()
    {
        // Rotate pointer
        currentAngle += pointerSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        if (pointer != null)
        {
            pointer.rotation = Quaternion.Euler(0, 0, currentAngle);
        }

        // Check for space input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed during skill check");
            CheckSkillCheckResult();
        }

        // Auto-fail if too slow
        if (Time.time - lastSkillCheckTime > 5f) // Increased timeout
        {
            Debug.Log("Skill check auto-failed (timeout)");
            FailSkillCheck();
        }
    }

    void CheckSkillCheckResult()
    {
        float pointerAngle = currentAngle;
        float successStart = successZoneAngle - successZoneSize / 2f;
        float successEnd = successZoneAngle + successZoneSize / 2f;

        // Handle circular overlap
        if (successStart < 0)
        {
            successStart += 360f;
            pointerAngle = (pointerAngle < 180f) ? pointerAngle + 360f : pointerAngle;
        }
        else if (successEnd > 360f)
        {
            successEnd -= 360f;
            pointerAngle = (pointerAngle > 180f) ? pointerAngle - 360f : pointerAngle;
        }

        bool isSuccess = pointerAngle >= successStart && pointerAngle <= successEnd;
        Debug.Log($"Skill check - Pointer: {pointerAngle}, Zone: {successStart}-{successEnd}, Success: {isSuccess}");

        if (isSuccess)
        {
            SuccessSkillCheck();
        }
        else
        {
            FailSkillCheck();
        }
    }

    void SuccessSkillCheck()
    {
        Debug.Log("Skill check SUCCESS!");
        photonView.RPC("AddProgress", RpcTarget.All, 0.15f);
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Good!", "green");
        EndSkillCheck();
    }

    void FailSkillCheck()
    {
        Debug.Log("Skill check FAILED!");
        photonView.RPC("AddProgress", RpcTarget.All, -0.1f);
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Failed!", "red");
        EndSkillCheck();
    }

    void EndSkillCheck()
    {
        isSkillCheckActive = false;
        if (skillCheck != null)
        {
            skillCheck.SetActive(false);
            Debug.Log("Skill check ended and UI hidden");
        }
    }

    [PunRPC]
    void AddProgress(float amount)
    {
        puzzleProgress = Mathf.Clamp01(puzzleProgress + amount);
        Debug.Log($"Progress updated: {puzzleProgress} (+{amount})");

        if (puzzleProgress >= 1f)
        {
            Debug.Log("PUZZLE COMPLETED!");
            photonView.RPC("CompletePuzzle", RpcTarget.All);
        }
    }

    [PunRPC]
    void ShowSkillCheckResult(string message, string color)
    {
        // Implement your result display here
        Debug.Log($"{message} - Color: {color}");
    }

    [PunRPC]
    void CompletePuzzle()
    {
        puzzleComplete = true;
        isPuzzleActive = false;
        if (skillCheck != null)
            skillCheck.SetActive(false);

        Debug.Log("Second Player Puzzle Completed - RPC Called!");

        // Notify coordinator
        if (PuzzleCoordinator.Instance != null)
        {
            PuzzleCoordinator.Instance.photonView.RPC("ReportPuzzleComplete", RpcTarget.All, 2);
        }
    }

    // Called by the collider when player enters
    // In SecondPlayerFinalPuzzleHandler.cs - Update the ActivatePuzzle method:
    public void ActivatePuzzle()
    {
        Debug.Log($"ActivatePuzzle called - IsMine: {photonView.IsMine}, PlayerNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            // Use RPC to ensure all clients get the activation
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, true);

            // Also set locally for immediate response
            isPuzzleActive = true;
            puzzleProgress = 0f;

            Debug.Log("Second player puzzle ACTIVATED");
        }
    }

    [PunRPC]
    void SyncPuzzleActivation(bool active)
    {
        isPuzzleActive = active;
        Debug.Log($"SyncPuzzleActivation RPC - Active: {active}, Called by Player: {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (!active)
        {
            if (skillCheck != null)
                skillCheck.SetActive(false);
            isSkillCheckActive = false;
        }
    }

    public void DeactivatePuzzle()
    {
        Debug.Log("DeactivatePuzzle called");

        if (photonView.IsMine && PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            isPuzzleActive = false;
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, false);

            // Hide skill check UI
            if (skillCheck != null)
                skillCheck.SetActive(false);
            isSkillCheckActive = false;

            Debug.Log("Second player puzzle DEACTIVATED");
        }
    }


    // FIX: Added the missing OnPhotonSerializeView method
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(isPuzzleActive);
            stream.SendNext(puzzleComplete);
            stream.SendNext(puzzleProgress);
            stream.SendNext(isSkillCheckActive);
        }
        else
        {
            // Network player, receive data
            this.isPuzzleActive = (bool)stream.ReceiveNext();
            this.puzzleComplete = (bool)stream.ReceiveNext();
            this.puzzleProgress = (float)stream.ReceiveNext();
            this.isSkillCheckActive = (bool)stream.ReceiveNext();
        }
    }

    // Add these methods to SecondPlayerFinalPuzzleHandler.cs

    [PunRPC]
    void RequestActivationFromOwner()
    {
        Debug.Log($"Activation requested from owner. Current owner: {photonView.Owner?.ActorNumber}, IsMine: {photonView.IsMine}");

        // If this client is the owner, activate the puzzle
        if (photonView.IsMine && PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            Debug.Log("Owner is activating puzzle via RPC request");
            ActivatePuzzle();
        }
    }
}