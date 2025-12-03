using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private GameObject menuUI;
    [SerializeField] private MainMenuController menu;
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject[] dialogues = new GameObject[5];
    private bool allFirstScene;
    private void Start()
    {
        allFirstScene = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            EndCutscene();

        if (anim.GetBool("part2") && !anim.GetBool("part3") && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) 
        {
            for (int i = 0; i < 2; i++) 
            {
                if (dialogues[i].activeSelf) 
                {
                    dialogues[i].SetActive(false);
                    dialogues[i+1].SetActive(true);
                    break;
                }
                if (i == 1)
                    allFirstScene = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (dialogues[2].activeSelf && allFirstScene)
                EndSecondScene();

            else if (dialogues[3].activeSelf)
                EndThirdScene();

            else if (dialogues[4].activeSelf)
                EndFifthScene();
        } 
    }
    public void StartCutscene() 
    {
        menuUI.SetActive(false);
    }

    public void EndFirstScene() 
    {
        anim.SetBool("part2", true);
        dialogues[0].SetActive(true);
    }

    private void EndSecondScene() 
    {
        anim.SetBool("part3", true);
        dialogues[2].SetActive(false);
        dialogues[3].SetActive(true);
    }

    private void EndThirdScene() 
    {
        anim.SetBool("part4", true);
        dialogues[3].SetActive(false);
        dialogues[4].SetActive(true);
    }

    public void EndFourthScene()
    {
        anim.SetBool("part5", true);
    }

    private void EndFifthScene()
    {
        anim.SetBool("part6", true);
        dialogues[4].SetActive(false);
    }

    public void EndCutscene() 
    {
        menu.StartGame();
    }
}
