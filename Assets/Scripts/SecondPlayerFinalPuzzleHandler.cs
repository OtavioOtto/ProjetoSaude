// SecondPlayerFinalPuzzleHandler.cs
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

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

    [Header("Verifiers")]
    public bool isPuzzleActive;
    public bool puzzleComplete;

    private bool isSkillCheckActive = false;
    private float currentAngle = 0f;
    private float successZoneAngle;
    private float lastSkillCheckTime = 0f;
    private float puzzleProgress = 0f;

    void Start()
    {
        isPuzzleActive = false;
        puzzleComplete = false;
        skillCheck.SetActive(false);
        successZone.fillAmount = successZoneSize / 360f;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Random skill check activation
        if (!puzzleComplete && isPuzzleActive && !isSkillCheckActive &&
            Time.time - lastSkillCheckTime > skillCheckFrequency)
        {
            StartSkillCheck();
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
        skillCheck.SetActive(true);

        // Randomize success zone position
        successZoneAngle = Random.Range(0f, 360f);
        successZone.transform.rotation = Quaternion.Euler(0, 0, successZoneAngle);

        // Reset pointer
        currentAngle = 0f;
        pointer.rotation = Quaternion.Euler(0, 0, currentAngle);

        lastSkillCheckTime = Time.time;
    }

    void UpdateSkillCheck()
    {
        // Rotate pointer
        currentAngle += pointerSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;
        pointer.rotation = Quaternion.Euler(0, 0, currentAngle);

        // Check for space input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckSkillCheckResult();
        }

        // Auto-fail if too slow
        if (Time.time - lastSkillCheckTime > 3f)
        {
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

        if (pointerAngle >= successStart && pointerAngle <= successEnd)
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
        photonView.RPC("AddProgress", RpcTarget.All, 0.15f);
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Good!", Color.green);
        EndSkillCheck();
    }

    void FailSkillCheck()
    {
        photonView.RPC("AddProgress", RpcTarget.All, -0.1f);
        photonView.RPC("ShowSkillCheckResult", RpcTarget.All, "Failed!", Color.red);
        EndSkillCheck();
    }

    void EndSkillCheck()
    {
        isSkillCheckActive = false;
        skillCheck.SetActive(false);
    }

    [PunRPC]
    void AddProgress(float amount)
    {
        puzzleProgress = Mathf.Clamp01(puzzleProgress + amount);
        progressSlider.value = puzzleProgress;

        if (puzzleProgress >= 1f)
        {
            puzzleComplete = true;
            isPuzzleActive = false;
            photonView.RPC("CompletePuzzle", RpcTarget.All);
        }
    }

    [PunRPC]
    void ShowSkillCheckResult(string message, Color color)
    {
        // You can implement a UI feedback system here
        Debug.Log(message);
    }

    [PunRPC]
    void CompletePuzzle()
    {
        puzzleComplete = true;
        isPuzzleActive = false;
        skillCheck.SetActive(false);
    }

    void UpdatePuzzleProgress()
    {
        // Visual updates for progress slider color, etc.
    }

    // Called by the collider when player enters
    public void ActivatePuzzle()
    {
        if (photonView.IsMine)
        {
            isPuzzleActive = true;
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, true);
        }
    }

    public void DeactivatePuzzle()
    {
        if (photonView.IsMine)
        {
            isPuzzleActive = false;
            photonView.RPC("SyncPuzzleActivation", RpcTarget.All, false);
        }
    }

    [PunRPC]
    void SyncPuzzleActivation(bool active)
    {
        isPuzzleActive = active;
        if (!active)
        {
            skillCheck.SetActive(false);
            isSkillCheckActive = false;
        }
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

            progressSlider.value = puzzleProgress;
        }
    }
}