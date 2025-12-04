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
                statsPower.gameObject.GetComponent<AudioSource>().Play();
                statsPower.connected = true;
                statsPower.movable = true;

                // Only copy X and Y, maintain powered wire's Z position
                Vector3 connectedPos = transform.position;
                connectedPos.z = statsPower.transform.position.z; // Keep powered wire's Z
                statsPower.connectedPosition = connectedPos;

                stats.connected = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PoweredWireStats>()) 
        {
            PoweredWireStats statsPower = collision.GetComponent<PoweredWireStats>();
            if (statsPower.color == stats.color)
            {
                statsPower.connected = false;
                stats.connected = false;
            }
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
