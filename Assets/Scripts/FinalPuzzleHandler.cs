using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class FinalPuzzleHandler : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Texts")]
    public TMP_Text buttonTxt;
    public TMP_Text feedbackTxt;
    [Header("Slider Components")]
    public Slider progress;
    public Image fillImage;
    [Header("First time checker")]
    public bool firstTime;
    [Header("UI")]
    public GameObject ui;
    [Header("Verifiers")]
    public bool isPuzzleActive;
    public bool puzzleComplete;

    private string[] _buttons = { "E", "F", "J", "G", "H", "R", "T" };
    private float timeLeft;
    private Coroutine puzzleCoroutine;
    private string currentButton;
    private bool waitingForInput;

    // Colors
    Color orange = new Color(249f / 255f, 129f / 255f, 40f / 255f);
    Color yellowGreen = new Color(154f / 255f, 205f / 255f, 50f / 255f);

    // Network synchronization
    private float syncedProgress = 0f;
    private bool syncedComplete = false;
    private bool syncedActive = false;

    // Reference to the collider that activates this puzzle
    private FinalPuzzleCollider activationCollider;

    private void Start()
    {
        firstTime = true;
        progress.value = 0f;
        isPuzzleActive = false;
        puzzleComplete = false;

        // Find the activation collider
        activationCollider = FindObjectOfType<FinalPuzzleCollider>();

        // Only enable for Player 1
        if (PhotonNetwork.LocalPlayer.ActorNumber != 1)
        {
            enabled = false;
            return;
        }

        Debug.Log("FinalPuzzleHandler initialized for Player 1");
    }

    void Update()
    {
        // Only Player 1 can activate and control this puzzle
        if (!photonView.IsMine || PhotonNetwork.LocalPlayer.ActorNumber != 1) return;

        // Add null check for activationCollider
        if (activationCollider == null)
        {
            // Try to find it again if it's null
            activationCollider = FindObjectOfType<FinalPuzzleCollider>();
            if (activationCollider == null)
            {
                Debug.LogError("FinalPuzzleCollider not found!");
                return;
            }
        }

        if (!isPuzzleActive && !puzzleComplete && ShouldActivatePuzzle())
        {
            isPuzzleActive = true;
            syncedActive = true;
            puzzleCoroutine = StartCoroutine(FinalPuzzle());
        }

        if (puzzleComplete)
            StartCoroutine(HideUI());

        UpdateProgressColor();
    }

    bool ShouldActivatePuzzle()
    {
        // Use the collider to determine if we should activate
        return activationCollider != null && activationCollider.playerInside;
    }

    // Rest of your FinalPuzzleHandler methods remain the same...
    IEnumerator FinalPuzzle()
    {
        int lastNumber = -1;
        bool missed = false;

        while (syncedProgress < 1f && isPuzzleActive)
        {
            if (!firstTime)
                yield return new WaitForSeconds(.5f);
            else
                firstTime = false;

            int buttonIndex = Random.Range(0, 7);
            if (lastNumber != -1)
                while (buttonIndex == lastNumber)
                    buttonIndex = Random.Range(0, 7);

            currentButton = _buttons[buttonIndex];
            photonView.RPC("UpdateButtonText", RpcTarget.All, "[" + currentButton + "]");
            lastNumber = buttonIndex;

            timeLeft = 3f;
            waitingForInput = true;

            CancelInvoke("SubtractOne");
            InvokeRepeating("SubtractOne", .5f, .5f);
            missed = false;
            bool success = false;
            KeyCode targetKeyCode = GetKeyCodeFromString(currentButton);

            while (waitingForInput && timeLeft > 0)
            {
                if (Input.GetKeyDown(targetKeyCode))
                {
                    success = true;
                    waitingForInput = false;
                }
                else if (Input.anyKeyDown)
                {
                    if (!Input.GetKeyDown(targetKeyCode))
                    {
                        success = false;
                        missed = true;
                        waitingForInput = false;
                    }
                }
                yield return null;
            }

            CancelInvoke("SubtractOne");

            if (success)
            {
                syncedProgress += 0.1f;
                photonView.RPC("UpdateProgress", RpcTarget.All, syncedProgress);
                photonView.RPC("UpdateButtonText", RpcTarget.All, "");

                int resposta = Random.Range(0, 3);
                string feedback = "";
                switch (resposta)
                {
                    case 0: feedback = "Boa!"; break;
                    case 1: feedback = "Certo!"; break;
                    case 2: feedback = "Isso!"; break;
                }
                photonView.RPC("ShowFeedback", RpcTarget.All, feedback);
            }
            else
            {
                syncedProgress = Mathf.Max(0f, syncedProgress - 0.1f);
                photonView.RPC("UpdateProgress", RpcTarget.All, syncedProgress);
                photonView.RPC("UpdateButtonText", RpcTarget.All, "");

                string feedback = !missed ? "Muito Lento..." : "Errou...";
                photonView.RPC("ShowFeedback", RpcTarget.All, feedback);
            }

            yield return new WaitForSeconds(.5f);
        }

        if (syncedProgress >= 1f)
        {
            isPuzzleActive = false;
            syncedActive = false;
            syncedComplete = true;
            puzzleComplete = true;
            photonView.RPC("CompletePuzzle", RpcTarget.All);

            // Notify coordinator
            FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPuzzleComplete", RpcTarget.All, 1);
        }

        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }
    }

    [PunRPC]
    void UpdateProgress(float newProgress)
    {
        progress.value = newProgress;
        syncedProgress = newProgress;
    }

    [PunRPC]
    void UpdateButtonText(string text)
    {
        if (buttonTxt != null)
            buttonTxt.SetText(text);
    }

    [PunRPC]
    void ShowFeedback(string feedback)
    {
        if (feedbackTxt != null)
        {
            feedbackTxt.gameObject.SetActive(true);
            feedbackTxt.SetText(feedback);
            StartCoroutine(HideFeedbackText());
        }
    }

    [PunRPC]
    void CompletePuzzle()
    {
        isPuzzleActive = false;
        puzzleComplete = true;
        syncedComplete = true;
        waitingForInput = false;

        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }

        if (feedbackTxt != null)
            feedbackTxt.SetText("Completou!");

        Debug.Log("Player 1 puzzle completed and stopped");
    }

    void SubtractOne()
    {
        timeLeft -= 1;
        if (timeLeft <= 0)
        {
            waitingForInput = false;
            CancelInvoke("SubtractOne");
        }
    }

    IEnumerator HideFeedbackText()
    {
        yield return new WaitForSeconds(1f);
        if (feedbackTxt != null)
            feedbackTxt.gameObject.SetActive(false);
    }

    IEnumerator HideUI()
    {
        yield return new WaitForSeconds(1.5f);
        if (ui != null)
            ui.SetActive(false);
    }

    void UpdateProgressColor()
    {
        if (fillImage != null && progress != null)
        {
            if (progress.value <= .2f)
                fillImage.color = Color.red;
            else if (progress.value > .2f && progress.value <= .4f)
                fillImage.color = orange;
            else if (progress.value > .4f && progress.value <= .6f)
                fillImage.color = Color.yellow;
            else if (progress.value > .6f && progress.value <= .8f)
                fillImage.color = yellowGreen;
            else
                fillImage.color = Color.green;
        }
    }

    private KeyCode GetKeyCodeFromString(string keyString)
    {
        switch (keyString.ToUpper())
        {
            case "E": return KeyCode.E;
            case "F": return KeyCode.F;
            case "J": return KeyCode.J;
            case "G": return KeyCode.G;
            case "H": return KeyCode.H;
            case "R": return KeyCode.R;
            case "T": return KeyCode.T;
            default: return KeyCode.None;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(syncedProgress);
            stream.SendNext(syncedComplete);
            stream.SendNext(syncedActive);
        }
        else
        {
            syncedProgress = (float)stream.ReceiveNext();
            syncedComplete = (bool)stream.ReceiveNext();
            syncedActive = (bool)stream.ReceiveNext();

            progress.value = syncedProgress;
            puzzleComplete = syncedComplete;
            isPuzzleActive = syncedActive;
        }
    }

    [PunRPC]
    public void ResetPuzzleProgress()
    {
        if (photonView.IsMine)
        {
            syncedProgress = 0f;
            progress.value = 0f;
            photonView.RPC("UpdateProgress", RpcTarget.All, 0f);

            if (puzzleCoroutine != null)
            {
                StopCoroutine(puzzleCoroutine);
                puzzleCoroutine = null;
            }

            isPuzzleActive = false;
            Debug.Log("Player 1 puzzle progress reset");
        }
    }

    [PunRPC]
    public void ForceActivatePuzzle()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1 && !puzzleComplete)
        {
            isPuzzleActive = true;
            if (puzzleCoroutine == null)
            {
                puzzleCoroutine = StartCoroutine(FinalPuzzle());
            }
        }
    }

    [PunRPC]
    public void StopPuzzle()
    {
        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }

        isPuzzleActive = false;
        waitingForInput = false;

        // Clear UI
        if (buttonTxt != null)
            buttonTxt.SetText("");
        if (feedbackTxt != null)
            feedbackTxt.gameObject.SetActive(false);

        Debug.Log("Player 1 puzzle stopped");
    }
}