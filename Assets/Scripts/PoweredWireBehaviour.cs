using UnityEngine;

public class PoweredWireBehaviour : MonoBehaviour
{
    [SerializeField] private bool mouseDown = false;
    private PoweredWireStats stats;
    LineRenderer line;
    void Start()
    {
        stats = gameObject.GetComponent<PoweredWireStats>();
        line = gameObject.GetComponent<LineRenderer>();
    }

    void Update()
    {
        MoveWire();
            line.SetPosition(3, new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -0.5f));
            line.SetPosition(2, new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -0.5f));
    }

    void OnMouseDown() { mouseDown = true; }

    void OnMouseOver() { stats.movable = true; }

    void OnMouseExit()
    {
        if (!stats.moving) 
            stats.movable = false;
    }

    void OnMouseUp() 
    {
        mouseDown = false;
        if(!stats.connected)
            gameObject.transform.position = stats.startPos;
        else
            gameObject.transform.position = stats.connectedPosition;
    }

    void MoveWire() 
    {
        if (mouseDown && stats.movable)
        {
            stats.moving = true;

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);

            gameObject.transform.position = Camera.main.ScreenToWorldPoint(mousePos);
        }

        else
            stats.moving = false;
    }

}
