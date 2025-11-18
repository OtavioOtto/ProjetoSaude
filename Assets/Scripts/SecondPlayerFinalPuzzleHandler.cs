using System.Collections;
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
    [SerializeField] private TMP_Text playerFeedback;
    [SerializeField] private GameObject playerFeedbackGO;

    [Header("Settings")]
    [SerializeField] private float pointerSpeed = 180f;
    [SerializeField] private float successZoneSize = 30f;
    [SerializeField] private float skillCheckFrequency = 3f;

    [Header("Verifiers")]
    public bool isPuzzleActive;
    public bool puzzleComplete;

    public bool isSkillCheckActive = false;
    private float currentAngle = 0f;
    private float successZoneAngle;
    private float lastSkillCheckTime = 0f;
    private bool firstTime;

    private PhotonView _photonView;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        // If PhotonView is null, try to find it
        if (_photonView == null)
        {
            _photonView = FindFirstObjectByType<PhotonView>();
        }
    }

    void Start()
    {
        firstTime = true;
        isPuzzleActive = false;
        puzzleComplete = false;

        if (successZone != null)
        {
            successZone.fillAmount = successZoneSize / 360f;
        }
        else
        {
        }

        // Initially hide the skill check UI
        if (skillCheck != null)
            skillCheck.SetActive(false);

    }

    void Update()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (!photonView.IsMine || puzzleType != 2) return;

        // Debug log to track puzzle state
        if (isPuzzleActive && !puzzleComplete && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Puzzle Active: {isPuzzleActive}, Skill Check Active: {isSkillCheckActive}, Time since last: {Time.time - lastSkillCheckTime}");
        }

        if (puzzleComplete && isPuzzleActive)
        {
            StopPuzzle();
            return;
        }

        // Skill check activation - improved logic
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
        }

        // Randomize success zone position
        successZoneAngle = Random.Range(65f, 290f);
        if (successZone != null)
        {
            successZone.transform.rotation = Quaternion.Euler(0, 0, successZoneAngle);
        }

        // Reset pointer position
        currentAngle = 0f;
        if (pointer != null)
        {
            pointer.rotation = Quaternion.Euler(0, 0, currentAngle);
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
            CheckSkillCheckResult();
        }

        // Auto-fail if too slow
        if (Time.time - lastSkillCheckTime > 5f) // Increased timeout
        {
            FailSkillCheck();
        }
    }

    void CheckSkillCheckResult()
    {
        float pointerAngle = currentAngle;
        float successStart = successZoneAngle - 35f;
        float successEnd = successZoneAngle + 40f;

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

        if (isSuccess)
        {
            SuccessSkillCheck();
        }
        else
        {
            FailSkillCheck();
        }
    }

    void FailSkillCheck()
    {
        Debug.Log("Skill check FAILED!");
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Falhou!");
        EndSkillCheck();

        if (FinalPuzzleCoordinator.Instance != null)
        {
            FinalPuzzleCoordinator.Instance.photonView.RPC("ReportSkillCheckFailure", RpcTarget.All);
        }
    }

    void SuccessSkillCheck()
    {
        Debug.Log("Skill check SUCCESS!");
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Boa!");
        EndSkillCheck();
    }

    void EndSkillCheck()
    {
        isSkillCheckActive = false;
        if (skillCheck != null)
        {
            skillCheck.SetActive(false);
        }
    }

    [PunRPC]
    void ShowSkillCheckResult(string message)
    {
        playerFeedback.text = message;
        playerFeedbackGO.SetActive(true);
        StartCoroutine(FeedbackReset());
    }

    [PunRPC]
    void CompletePuzzle()
    {
        puzzleComplete = true;
        isPuzzleActive = false;
        isSkillCheckActive = false;

        if (skillCheck != null)
            skillCheck.SetActive(false);

    }

    // Called by the collider when player enters
    // In SecondPlayerFinalPuzzleHandler.cs - Update the ActivatePuzzle method:
    public void ActivatePuzzle()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType == 2 && photonView.IsMine)
        {
            Debug.Log("Player 2 manually activating puzzle");
            ActivatePuzzleImmediately();
        }
    }

    [PunRPC]
    void SyncPuzzleActivation(bool active)
    {
        Debug.Log($"SyncPuzzleActivation: {active}, IsMine: {photonView.IsMine}");

        isPuzzleActive = active;

        if (!active)
        {
            if (skillCheck != null)
                skillCheck.SetActive(false);
            isSkillCheckActive = false;
        }
        else
        {
            // When activated, ensure we start the skill check cycle
            firstTime = true;
            lastSkillCheckTime = Time.time - skillCheckFrequency;
        }
    }

    public void DeactivatePuzzle()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (photonView.IsMine && puzzleType == 2)
        {
            isPuzzleActive = false;
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, false);

            // Hide skill check UI
            if (skillCheck != null)
                skillCheck.SetActive(false);
            isSkillCheckActive = false;

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
            stream.SendNext(isSkillCheckActive);
        }
        else
        {
            // Network player, receive data
            this.isPuzzleActive = (bool)stream.ReceiveNext();
            this.puzzleComplete = (bool)stream.ReceiveNext();
            this.isSkillCheckActive = (bool)stream.ReceiveNext();
        }
    }

    [PunRPC]
    void RequestActivationFromOwner()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        // If this client is the owner, activate the puzzle
        if (photonView.IsMine && puzzleType == 2)
        {
            ActivatePuzzle();
        }
    }

    IEnumerator FeedbackReset() 
    {
        yield return new WaitForSeconds(1.5f);
        playerFeedback.gameObject.SetActive(false);
    }

    [PunRPC]
    public void ResetPuzzle()
    {
        if (photonView.IsMine)
        {
            isSkillCheckActive = false;
            firstTime = true;
            lastSkillCheckTime = Time.time;

            if (skillCheck != null)
                skillCheck.SetActive(false);
        }
    }

    [PunRPC]
    public void ForceActivatePuzzle()
    {
        Debug.Log($"ForceActivatePuzzle RPC received - Player: {PhotonNetwork.LocalPlayer?.ActorNumber}, IsMine: {photonView.IsMine}");

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (PhotonNetwork.LocalPlayer != null && puzzleType == 2 && !puzzleComplete)
        {
            // Request ownership if we don't have it
            if (!photonView.IsMine)
            {
                Debug.Log("Player 2 requesting ownership for puzzle");
                photonView.RequestOwnership();
                // Small delay to ensure ownership transfer
                StartCoroutine(DelayedActivation());
            }
            else
            {
                ActivatePuzzleImmediately();
            }
        }
        else
        {
            Debug.Log($"ForceActivatePuzzle rejected - Player: {PhotonNetwork.LocalPlayer?.ActorNumber}, Complete: {puzzleComplete}, PuzzleType: {puzzleType}");
        }
    }

    private IEnumerator DelayedActivation()
    {
        yield return new WaitForSeconds(0.2f);
        ActivatePuzzleImmediately();
    }

    private void ActivatePuzzleImmediately()
    {
        isPuzzleActive = true;
        firstTime = true;
        lastSkillCheckTime = Time.time - skillCheckFrequency; // Start immediately

        Debug.Log($"Player 2 puzzle ACTIVATED - isPuzzleActive: {isPuzzleActive}, firstTime: {firstTime}");
    }

    [PunRPC]
    public void StopPuzzle()
    {
        isPuzzleActive = false;
        isSkillCheckActive = false;
        puzzleComplete = true;

        // Hide skill check UI
        if (skillCheck != null)
            skillCheck.SetActive(false);

        // Clear feedback
        if (playerFeedbackGO != null)
            playerFeedbackGO.SetActive(false);

    }
}