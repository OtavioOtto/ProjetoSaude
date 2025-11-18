using UnityEngine;

public class PoweredWireBehaviour : MonoBehaviour
{
    [SerializeField] private bool mouseDown = false;
    private PoweredWireStats stats;
    LineRenderer line;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        stats = gameObject.GetComponent<PoweredWireStats>();
        line = gameObject.GetComponent<LineRenderer>();

        // Ensure consistent Z position
        Vector3 pos = transform.position;
        transform.position = pos;
        stats.startPos = pos;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -1f);

        // Set initial line positions
        if (line != null)
        {
            line.SetPosition(1, new Vector3(transform.position.x - 1f, transform.position.y, 0f));
            line.SetPosition(0, new Vector3(transform.position.x - 2f, transform.position.y, 0f));
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        MoveWire();
        if (line != null)
        {
            line.SetPosition(3, new Vector3(transform.position.x - .1f, transform.position.y, -0.5f));
            line.SetPosition(2, new Vector3(transform.position.x - .4f, transform.position.y, -0.5f));
        }
    }

    private void OnEnable()
    {
        // If Start hasn't run yet, we can't initialize here
        if (isInitialized && line != null)
        {
            line.SetPosition(1, new Vector3(transform.position.x - 1f, transform.position.y, 0f));
            line.SetPosition(0, new Vector3(transform.position.x - 2f, transform.position.y, 0f));
        }
    }

    void OnMouseDown()
    {
        if (!isInitialized) return;

        mouseDown = true;
        stats.movable = true;
        stats.moving = true;
    }

    void OnMouseOver()
    {
        if (!isInitialized) return;
        stats.movable = true;
    }

    void OnMouseExit()
    {
        if (!isInitialized) return;

        if (!mouseDown)
            stats.movable = false;
    }

    void OnMouseUp()
    {
        if (!isInitialized) return;

        mouseDown = false;
        stats.moving = false;

        Vector3 targetPosition;
        if (!stats.connected)
            targetPosition = stats.startPos;
        else
            targetPosition = stats.connectedPosition;

        transform.position = targetPosition;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -1f);
    }

    void MoveWire()
    {
        if (!isInitialized) return;

        if (mouseDown && stats.movable)
        {
            Vector3 mousePos = Input.mousePosition;
            GameObject player = GameObject.Find("Morfeus(Clone)");
            if (player != null)
            {
                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    mousePos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
                    Vector3 newPosition = cam.ScreenToWorldPoint(mousePos);
                    transform.position = newPosition;
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -1f);
                }
            }
        }
    }
}