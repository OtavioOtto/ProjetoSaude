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
    [Header("First time check")]
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

    Color orange = new Color(249f / 255f, 129f / 255f, 40f / 255f);
    Color yellowGreen = new Color(154f / 255f, 205f / 255f, 50f / 255f);

    private float syncedProgress = 0f;
    private bool syncedComplete = false;
    private bool syncedActive = false;

    private FinalPuzzleCollider activationCollider;
    private FinalPuzzleCoordinator coordinator;

    private void Start()
    {
        firstTime = true;
        progress.value = 0f;
        isPuzzleActive = false;
        puzzleComplete = false;

        activationCollider = FindFirstObjectByType<FinalPuzzleCollider>();
        coordinator = FindFirstObjectByType<FinalPuzzleCoordinator>();

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

        if (puzzleComplete)
            StartCoroutine(HideUI());

        UpdateProgressColor();

        if (isPuzzleActive && waitingForInput)
        {
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
        return activationCollider != null &&
               activationCollider.playerInside &&
               FinalPuzzleCoordinator.Instance != null &&
               FinalPuzzleCoordinator.Instance.bothPuzzlesActive;
    }

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

            int buttonIndex;
            do
            {
                buttonIndex = Random.Range(0, _buttons.Length);
            } 
            while (buttonIndex == lastNumber && _buttons.Length > 1);

            currentButton = _buttons[buttonIndex];
            lastNumber = buttonIndex;

            photonView.RPC("UpdateButtonText", RpcTarget.All, "[" + currentButton + "]");
            timeLeft = 3f;
            waitingForInput = true;
            missed = false;

            CancelInvoke("SubtractOne");
            InvokeRepeating("SubtractOne", 1f, 1f);

            KeyCode targetKeyCode = GetKeyCodeFromString(currentButton);
            bool success = false;
            float inputStartTime = Time.time;


            while (waitingForInput && timeLeft > 0)
            {
                if (Input.GetKeyDown(targetKeyCode))
                {
                    success = true;
                    waitingForInput = false;
                    break;
                }
                else if (Input.anyKeyDown)
                {
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

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master client forcing game end via final puzzle");
            coordinator.EndGame();
        }
        else
        {
            Debug.Log("Non-master client requesting game end via final puzzle");
            photonView.RPC("RequestGameEnd", RpcTarget.MasterClient);
        }
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
        photonView.RPC("UpdateProgress", RpcTarget.All, 0f);

        if (puzzleCoroutine != null)
        {
            StopCoroutine(puzzleCoroutine);
            puzzleCoroutine = null;
        }

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