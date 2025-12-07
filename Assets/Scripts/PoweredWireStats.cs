using UnityEngine;

public enum Colors {blue, red, yellow, green};
public class PoweredWireStats : MonoBehaviour
{
    [Header("Global Values")]
    public bool movable = false;
    public bool moving = false;
    public Vector3 startPos;
    public Colors color;
    public bool connected = false;
    public Vector3 connectedPosition;

    void Start()
    {
        startPos = transform.position;
    }
}
