using UnityEngine;
using UnityEngine.UI;

public class PotionFillController : MonoBehaviour
{
    [SerializeField] private Slider slider;

    Transform child1;
    Transform child2;
    Transform child3;
    Transform child4;
    void Start()
    {
        child1 = transform.transform.GetChild(0);
        child2 = transform.transform.GetChild(1);
        child3 = transform.transform.GetChild(2);
        child4 = transform.transform.GetChild(3);
    }

    void Update()
    {
        if (slider.value % 1 < .3f)
        {
            child1.gameObject.SetActive(true);
            child2.gameObject.SetActive(false);
            child3.gameObject.SetActive(false);
            child4.gameObject.SetActive(false);
        }

        else if (slider.value % 1 >= .3f && slider.value % 1 < .5f)
        {
            child1.gameObject.SetActive(false);
            child2.gameObject.SetActive(true);
            child3.gameObject.SetActive(false);
            child4.gameObject.SetActive(false);
        }

        else if (slider.value % 1 >= .5f && slider.value % 1 < .7f)
        {
            child1.gameObject.SetActive(false);
            child2.gameObject.SetActive(false);
            child3.gameObject.SetActive(true);
            child4.gameObject.SetActive(false);
        }

        else if (slider.value % 1 >= .7f && slider.value % 1 < .9f)
        {
            child1.gameObject.SetActive(false);
            child2.gameObject.SetActive(false);
            child3.gameObject.SetActive(true);
            child4.gameObject.SetActive(false);
        }

        else if (slider.value % 1 >= .9f)
        {
            child1.gameObject.SetActive(false);
            child2.gameObject.SetActive(false);
            child3.gameObject.SetActive(false);
            child4.gameObject.SetActive(true);
        }
    }
}
