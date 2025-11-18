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
        activationCollider = FindFirstObjectByType<FinalPuzzleCollider>();

        // Only enable for players who selected Alex (character1)
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (puzzleType != 1)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (!photonView.IsMine || puzzleType != 1) return;

        // Handle puzzle completion
        if (puzzleComplete)
            StartCoroutine(HideUI());

        UpdateProgressColor();

        // Input handling during active puzzle
        if (isPuzzleActive && waitingForInput)
        {
            // Input is already handled in the coroutine, but we need to ensure
            // the puzzle stops if conditions change
            if (!ShouldKeepPuzzleActive())
            {
                StopPuzzle();
            }
        }
    }

    bool ShouldKeepPuzzleActive()
    {
        return FinalPuzzleCoordinator.Instance != null &&
               FinalPuzzleCoordinator.Instance.bothPuzzlesActive;
    }

    [PunRPC]
    public void ForceActivatePuzzle()
    {
        Debug.Log($"ForceActivatePuzzle RPC received for Player 1 - IsMine: {photonView.IsMine}");

        int puzzleType = NetworkManager.Instance.GetLocalPlayerPuzzleType();
        if (PhotonNetwork.LocalPlayer != null && puzzleType == 1 && !puzzleComplete && !isPuzzleActive)
        {
            isPuzzleActive = true;
            syncedActive = true;

            if (puzzleCoroutine == null)
            {
                puzzleCoroutine = StartCoroutine(FinalPuzzle());
                Debug.Log("Player 1 puzzle STARTED via ForceActivatePuzzle");
            }
        }
        else
        {
            Debug.Log($"ForceActivatePuzzle rejected - Player: {PhotonNetwork.LocalPlayer?.ActorNumber}, Complete: {puzzleComplete}, Active: {isPuzzleActive}");
        }
    }

    bool ShouldActivatePuzzle()
    {
        // Use the coordinator to determine if both players are ready
        return activationCollider != null &&
               activationCollider.playerInside &&
               FinalPuzzleCoordinator.Instance != null &&
               FinalPuzzleCoordinator.Instance.bothPuzzlesActive;
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

            // Generate new button (ensure it's different from last)
            int buttonIndex;
            do
            {
                buttonIndex = Random.Range(0, _buttons.Length);
            } while (buttonIndex == lastNumber && _buttons.Length > 1);

            currentButton = _buttons[buttonIndex];
            lastNumber = buttonIndex;

            // Update UI and reset state
            photonView.RPC("UpdateButtonText", RpcTarget.All, "[" + currentButton + "]");
            timeLeft = 3f;
            waitingForInput = true;
            missed = false;

            // Clear any previous invoke
            CancelInvoke("SubtractOne");
            InvokeRepeating("SubtractOne", 1f, 1f); // Changed to 1 second intervals

            KeyCode targetKeyCode = GetKeyCodeFromString(currentButton);
            bool success = false;
            float inputStartTime = Time.time;

            // Input loop - simplified
            while (waitingForInput && timeLeft > 0)
            {
                // Check for the correct key
                if (Input.GetKeyDown(targetKeyCode))
                {
                    success = true;
                    waitingForInput = false;
                    break;
                }
                // Check for any wrong key press
                else if (Input.anyKeyDown)
                {
                    // Check if any of the valid buttons were pressed (wrong one)
                    foreach (string button in _buttons)
                    {
                        KeyCode keyCode = GetKeyCodeFromString(button);
                        if (Input.GetKeyDown(keyCode) && keyCode != targetKeyCode)
                        {
                            success = false;
                            missed = true;
                            waitingForInput = false;
                            break;
                        }
                    }
                }

                yield return null;
            }

            CancelInvoke("SubtractOne");

            // Handle result
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

            // Clear the button display and wait before next round
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
            if (FinalPuzzleCoordinator.Instance != null)
            {
                FinalPuzzleCoordinator.Instance.photonView.RPC("ReportPuzzleComplete", RpcTarget.All, 1);
            }
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

       
        UpdateProgressColor();
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
        if (!photonView.IsMine) return;

        syncedProgress = 0f;
        progress.value = 0f;
        puzzleComplete = false;
        isPuzzleActive = false;

        // Stop any running puzzle
        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }

        // Update UI
        UpdateProgressColor();
        feedbackTxt.SetText("");
        buttonTxt.SetText("");
    }

    [PunRPC]
    public void StopPuzzle()
    {
        if (this == null || photonView == null) return;

        isPuzzleActive = false;
        waitingForInput = false;

        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }

        if (buttonTxt != null)
            buttonTxt.SetText("");
        if (feedbackTxt != null)
            feedbackTxt.gameObject.SetActive(false);
    }

}