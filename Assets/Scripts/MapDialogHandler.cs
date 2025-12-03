using UnityEngine;

public class MapDialogHandler : MonoBehaviour
{
    [SerializeField] private FinalRoomCollider dialogHanlderEnergy;
    [SerializeField] private FinalRoomCollider dialogHanlderMap;
    [SerializeField] private GameObject optionOne;
    [SerializeField] private GameObject optionTwo;
    private void Update()
    {
        if (dialogHanlderEnergy.triggerOnce && dialogHanlderEnergy.hasTriggeredGlobally)
        {
            optionOne.SetActive(false);
            optionTwo.SetActive(true);
        }

        if (dialogHanlderMap.triggerOnce && dialogHanlderMap.hasTriggeredGlobally) 
        {
            optionOne.SetActive(false);
            optionTwo.SetActive(false);
        }
    }
}
