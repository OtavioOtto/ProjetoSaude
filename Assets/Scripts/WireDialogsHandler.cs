using UnityEngine;

public class WireDialogsHandler : MonoBehaviour
{
    [SerializeField] private GameObject generatorUnpowered;
    [SerializeField] private GameObject generatorPowered;
    private GameObject firstDialogType;
    private GameObject secondDialogType;
    void Start()
    {
        firstDialogType = transform.GetChild(0).gameObject;
        secondDialogType = transform.GetChild(1).gameObject;
    }

    void Update()
    {
        if (generatorUnpowered.activeSelf) 
        {
            firstDialogType.SetActive(true);
            secondDialogType.SetActive(false);
        }

        else if (generatorPowered.activeSelf)
        {
            firstDialogType.SetActive(false);
            secondDialogType.SetActive(true);
        }
    }
}
