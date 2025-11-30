using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class DialogManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogCanvas;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private Button continueButton;

    [Header("Dialog Settings")]
    [SerializeField] private float textSpeed = 0.05f;

    private Dialog[] currentDialog;
    private int currentLine = 0;
    private bool isTyping = false;
    private bool dialogActive = false;
    private Coroutine typingCoroutine;

    public static DialogManager Instance { get; private set; }

    [System.Serializable]
    public class Dialog
    {
        [TextArea(3, 5)]
        public string dialogText;
        public Sprite portrait;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Hide dialog panel initially
        dialogCanvas.SetActive(false);
        continueButton.onClick.AddListener(ContinueDialog);
    }

    void Update()
    {
        // Allow skipping dialog with Space or Enter
        if (dialogActive && Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                ContinueDialog();
            }
        }

    }

    [PunRPC]
    public void StartDialogRPC(Dialog[] dialog)
    {
        if (dialogActive) return;

        currentDialog = dialog;
        currentLine = 0;
        dialogActive = true;
        dialogCanvas.SetActive(true);

        DisplayLine(currentDialog[0]);
    }

    public void StartDialog(Dialog[] dialog)
    {
        // Use RPC to sync dialog across network
        photonView.RPC("StartDialogFromSerializedRPC", RpcTarget.All, SerializeDialog(dialog));
    }


    private void DisplayLine(Dialog dialogLine)
    {
        Debug.Log($"Displaying line: {dialogLine.dialogText}, portrait: {dialogLine.portrait?.name}");
        if (speakerPortrait != null && dialogLine.portrait != null)
        {
            speakerPortrait.sprite = dialogLine.portrait;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(dialogLine.dialogText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogText.text = "";
        continueButton.gameObject.SetActive(false);

        foreach (char letter in text.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        continueButton.gameObject.SetActive(true);
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogText.text = currentDialog[currentLine].dialogText;
        isTyping = false;
        continueButton.gameObject.SetActive(true);
    }

    public void ContinueDialog()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        currentLine++;

        if (currentLine < currentDialog.Length)
        {
            DisplayLine(currentDialog[currentLine]);
        }
        else
        {
            EndDialog();
        }
    }

    private void EndDialog()
    {
        dialogActive = false;
        dialogCanvas.SetActive(false);
        currentDialog = null;
    }

    // Serialization methods for RPC
    private object[] SerializeDialog(Dialog[] dialog)
    {
        // Each dialog has 2 properties: dialogText and portrait
        object[] serialized = new object[dialog.Length * 2];
        int index = 0;

        foreach (Dialog d in dialog)
        {
            serialized[index++] = d.dialogText;
            serialized[index++] = d.portrait != null ? d.portrait.name : "";
        }

        return serialized;
    }


    [PunRPC]
    private void StartDialogFromSerializedRPC(object[] serializedDialog)
    {
        // Each dialog takes 2 elements in the array
        int dialogLength = serializedDialog.Length / 2;
        Dialog[] dialog = new Dialog[dialogLength];
        int index = 0;

        for (int i = 0; i < dialogLength; i++)
        {
            dialog[i] = new Dialog
            {
                dialogText = (string)serializedDialog[index++],
                portrait = LoadPortrait((string)serializedDialog[index++])
            };
        }

        // Call the main RPC method with the deserialized dialog
        StartDialogRPC(dialog);
    }

    private Sprite LoadPortrait(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        return Resources.Load<Sprite>($"Portraits/{spriteName}");
    }

}