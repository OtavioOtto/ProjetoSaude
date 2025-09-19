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
        line.SetPosition(3, new Vector3(gameObject.transform.localPosition.x - .1f, gameObject.transform.localPosition.y - .1f, 0));
        line.SetPosition(2, new Vector3(gameObject.transform.localPosition.x - .4f, gameObject.transform.localPosition.y - .4f, 0));
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
            float mouseX = Input.mousePosition.x;
            float mouseY = Input.mousePosition.y;

            gameObject.transform.position = Camera.main.ScreenToViewportPoint(new Vector3(mouseX, mouseY, 1));
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
        }

        else
            stats.moving = false;
    }

}
