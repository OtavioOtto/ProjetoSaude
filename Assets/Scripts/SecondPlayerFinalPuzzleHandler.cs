// SecondPlayerFinalPuzzleHandler.cs (Updated with comprehensive debugging)
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

    [Header("Progress")]
    [SerializeField] private Slider progressSlider;

    [Header("UI Feedback")]
    [SerializeField] private TMP_Text feedbackText;

    [Header("Verifiers")]
    public bool isPuzzleActive;
    public bool puzzleComplete;

    private bool isSkillCheckActive = false;
    private float currentAngle = 0f;
    private float successZoneAngle;
    private float lastSkillCheckTime = 0f;
    private float puzzleProgress = 0f;
    private bool firstTime;

    void Start()
    {
        firstTime = true;
        isPuzzleActive = false;
        puzzleComplete = false;

        if (successZone != null)
            successZone.fillAmount = successZoneSize / 360f;

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);

        Debug.Log("SecondPlayerFinalPuzzleHandler initialized for Player 2");
    }

    void Update()
    {
        // Basic checks
        if (photonView == null)
        {
            Debug.LogError("PhotonView is null!");
            return;
        }

        if (!photonView.IsMine)
        {
            // Debug.Log("PhotonView is not mine");
            return;
        }

        if (PhotonNetwork.LocalPlayer == null || PhotonNetwork.LocalPlayer.ActorNumber != 2)
        {
            // Debug.Log("Not Player 2 or local player is null");
            return;
        }

        // SIMPLIFIED: Force activate for testing if we press a key
        if (Input.GetKeyDown(KeyCode.P) && !isPuzzleActive && !puzzleComplete)
        {
            Debug.Log("Force activating puzzle with P key");
            isPuzzleActive = true;
            puzzleProgress = 0f;
            lastSkillCheckTime = Time.time - skillCheckFrequency;
        }

        // Debug puzzle state occasionally
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            Debug.Log($"Puzzle - Active: {isPuzzleActive}, Complete: {puzzleComplete}, SkillCheck: {isSkillCheckActive}, Progress: {puzzleProgress}");
        }

        // Skill check activation - SIMPLIFIED CONDITION
        if (isPuzzleActive && !puzzleComplete && (!isSkillCheckActive || firstTime))
        {
            float timeSinceLastCheck = Time.time - lastSkillCheckTime;
            firstTime = false;

            if (timeSinceLastCheck > skillCheckFrequency)
            {
                Debug.Log($"STARTING SKILL CHECK! Time since last: {timeSinceLastCheck}");
                StartSkillCheck();
            }
        }

        if (isSkillCheckActive)
        {
            UpdateSkillCheck();
        }

        UpdatePuzzleProgress();
    }

    void StartSkillCheck()
    {
        isSkillCheckActive = true;

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
            Debug.Log($"Success zone angle: {successZoneAngle}");
        }
        else
        {
            Debug.LogError("SuccessZone is null!");
        }

        // Reset pointer
        currentAngle = 0f;
        if (pointer != null)
        {
            pointer.rotation = Quaternion.Euler(0, 0, currentAngle);
            Debug.Log("Pointer reset to 0 degrees");
        }
        else
        {
            Debug.LogError("Pointer is null!");
        }

        lastSkillCheckTime = Time.time;
        Debug.Log("Skill check fully started");
    }

    void UpdateSkillCheck()
    {
        // Rotate pointer
        currentAngle += pointerSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        if (pointer != null)
            pointer.rotation = Quaternion.Euler(0, 0, currentAngle);

        // Check for space input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed during skill check");
            CheckSkillCheckResult();
        }

        // Auto-fail if too slow
        if (Time.time - lastSkillCheckTime > 3f)
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
        Debug.Log($"Skill check result - Pointer: {pointerAngle}, Zone: {successStart}-{successEnd}, Success: {isSuccess}");

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

        // Update progress slider
        if (progressSlider != null)
            progressSlider.value = puzzleProgress;

        Debug.Log($"Progress updated: {puzzleProgress} (+{amount})");

        if (puzzleProgress >= 1f)
        {
            puzzleComplete = true;
            isPuzzleActive = false;
            Debug.Log("PUZZLE COMPLETED!");
            photonView.RPC("CompletePuzzle", RpcTarget.All);
        }
    }

    [PunRPC]
    void ShowSkillCheckResult(string message, string color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;

            if (color == "green")
                feedbackText.color = Color.green;
            else if (color == "red")
                feedbackText.color = Color.red;

            feedbackText.gameObject.SetActive(true);
            StartCoroutine(HideFeedbackText());
        }
        Debug.Log($"Skill Check Result: {message}");
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

    void UpdatePuzzleProgress()
    {
        // Update progress slider color based on progress
        if (progressSlider != null)
        {
            Image fillImage = progressSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (puzzleProgress <= 0.2f)
                    fillImage.color = Color.red;
                else if (puzzleProgress <= 0.4f)
                    fillImage.color = new Color(1f, 0.5f, 0f);
                else if (puzzleProgress <= 0.6f)
                    fillImage.color = Color.yellow;
                else if (puzzleProgress <= 0.8f)
                    fillImage.color = new Color(0.5f, 1f, 0f);
                else
                    fillImage.color = Color.green;
            }
        }
    }

    // Called by the collider when player enters
    public void ActivatePuzzle()
    {
        Debug.Log($"ActivatePuzzle called - IsMine: {photonView.IsMine}, PlayerNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (photonView.IsMine && PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            isPuzzleActive = true;
            puzzleProgress = 0f;
            if (progressSlider != null)
                progressSlider.value = 0f;

            Debug.Log("Second player puzzle ACTIVATED");
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, true);
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

    [PunRPC]
    void SyncPuzzleActivation(bool active)
    {
        isPuzzleActive = active;
        Debug.Log($"SyncPuzzleActivation RPC - Active: {active}");

        if (!active)
        {
            if (skillCheck != null)
                skillCheck.SetActive(false);
            isSkillCheckActive = false;
        }
    }

    private System.Collections.IEnumerator HideFeedbackText()
    {
        yield return new WaitForSeconds(1.5f);
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(puzzleProgress);
            stream.SendNext(puzzleComplete);
            stream.SendNext(isPuzzleActive);
        }
        else
        {
            puzzleProgress = (float)stream.ReceiveNext();
            puzzleComplete = (bool)stream.ReceiveNext();
            isPuzzleActive = (bool)stream.ReceiveNext();

            // Update UI for remote clients
            if (progressSlider != null)
                progressSlider.value = puzzleProgress;
        }
    }
}