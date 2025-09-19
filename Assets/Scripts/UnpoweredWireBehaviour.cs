using UnityEngine;

public class UnpoweredWireBehaviour : MonoBehaviour
{
    private UnpoweredWireStats stats;
    void Start()
    {
        stats = gameObject.GetComponent<UnpoweredWireStats>();
    }

    void Update()
    {
        ManageLight();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PoweredWireStats>()) 
        {
            PoweredWireStats statsPower = collision.GetComponent<PoweredWireStats>();
            if (statsPower.color == stats.color) 
            {
                statsPower.connected = true;
                statsPower.connectedPosition = gameObject.transform.position;
                stats.connected = true;
            }
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PoweredWireStats>()) 
        {
            PoweredWireStats statsPower = collision.GetComponent<PoweredWireStats>();
        }
    }

    void ManageLight() 
    {
        if (stats.connected)
        {
            stats.poweredLight.SetActive(true);
            stats.unpoweredLight.SetActive(false);
        }
        else
        {
            stats.poweredLight.SetActive(false);
            stats.unpoweredLight.SetActive(true);
        }
    }
}
