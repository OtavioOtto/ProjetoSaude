using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FinalPuzzleHandler : MonoBehaviour
{
    public TMP_Text buttonTxt;
    public TMP_Text feedbackTxt;
    public Slider progress;
    public bool firstTime;
    public bool puzzleComplete;
    public bool isPuzzleActive;
    public GameObject ui;
    private string[] _buttons = { "E", "F", "J", "G", "H", "R", "T" };
    private float timeLeft; 
    private Coroutine puzzleCoroutine;
    private string currentButton;
    private bool waitingForInput;

    private void Start()
    {
        firstTime = true;
        progress.value = 0f;
        isPuzzleActive = false;
        puzzleComplete = false;
    }

    void Update()
    {
        if (gameObject.activeSelf && !isPuzzleActive && !puzzleComplete)
        {
            isPuzzleActive = true;
            puzzleCoroutine = StartCoroutine(FinalPuzzle());
        }
        else if (!gameObject.activeSelf && isPuzzleActive)
        {
            isPuzzleActive = false;
            if (puzzleCoroutine != null)
            {
                StopCoroutine(puzzleCoroutine);
            }
        }

        if (puzzleComplete)
            StartCoroutine(HideUI());
    }    

    IEnumerator FinalPuzzle()
    {
        int lastNumber = -1;
        bool missed = false;
        while(progress.value != 1)
        {
            if (!firstTime)
                yield return new WaitForSeconds(.5f);
            else
                firstTime = false;

            int buttonIndex = Random.Range(0, 7);
            if(lastNumber != -1)
                while(buttonIndex == lastNumber)
                    buttonIndex = Random.Range(0, 7);

            currentButton = _buttons[buttonIndex];
            buttonTxt.SetText("[" + currentButton + "]");
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
                progress.value += 0.1f;
                buttonTxt.SetText("");
                int resposta = Random.Range(0,3);
                feedbackTxt.gameObject.SetActive(true);
                switch (resposta) 
                {
                    case 0: feedbackTxt.SetText("Boa!");
                        break;

                    case 1:
                        feedbackTxt.SetText("Certo!");
                        break;

                    case 2:
                        feedbackTxt.SetText("Isso!");
                        break;
                }
                StartCoroutine(HideFeedbackText());
            }
            else
            {
                progress.value = Mathf.Max(0f, progress.value - 0.1f);
                buttonTxt.SetText("");
                feedbackTxt.gameObject.SetActive(true);
                if(!missed)
                    feedbackTxt.SetText("Muito Lento...");
                else
                    feedbackTxt.SetText("Errou...");
                StartCoroutine(HideFeedbackText());
            }

            yield return new WaitForSeconds(.5f);
        }
            isPuzzleActive = false;
            puzzleComplete = true;
            feedbackTxt.SetText("Completou!");
            StopCoroutine(puzzleCoroutine);
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
        feedbackTxt.gameObject.SetActive(false);
    }

    IEnumerator HideUI() 
    {
        yield return new WaitForSeconds(1.5f);
        ui.SetActive(false);
    }

    // Helper method to convert string to KeyCode
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
}