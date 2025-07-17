using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CustomKeyCameraController : MonoBehaviour
{
    [Header("Movement Keys")]
    public KeyCode forwardKey = KeyCode.I;
    public KeyCode backwardKey = KeyCode.K;
    public KeyCode rightKey = KeyCode.L;
    public KeyCode leftKey = KeyCode.J;
    public KeyCode upKey = KeyCode.U;
    public KeyCode downKey = KeyCode.O;
    public float moveSpeed = 10f;
    public float fastMoveMultiplier = 3f;

    [Header("Mouse Look")]
    public KeyCode rotateButton = KeyCode.Mouse1;
    public float lookSensitivity = 3f;
    private float rotX = 0f, rotY = 0f;

    [Header("Zoom Keys")]
    public KeyCode zoomInKey = KeyCode.Equals;
    public KeyCode zoomOutKey = KeyCode.Minus;
    public float zoomStep = 5f;
    public float minFOV = 15f, maxFOV = 90f;


    private bool isInputEnabled = true;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        Vector3 e = transform.eulerAngles;
        rotX = e.y; rotY = e.x;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F4))
        {
            isInputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            isInputEnabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


        if (isInputEnabled)
        {
            HandleMovement();
            HandleMouseLook();
            HandleZoomKeys();
        }
    }

    void HandleMovement()
    {
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(forwardKey)) dir += transform.forward;
        if (Input.GetKey(backwardKey)) dir -= transform.forward;
        if (Input.GetKey(rightKey)) dir += transform.right;
        if (Input.GetKey(leftKey)) dir -= transform.right;
        if (Input.GetKey(upKey)) dir += transform.up;
        if (Input.GetKey(downKey)) dir -= transform.up;

        float speedMul = Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f;
        transform.position += dir.normalized * moveSpeed * speedMul * Time.deltaTime;
    }

    void HandleMouseLook()
    {
        if (!Input.GetKey(rotateButton)) return;
        rotX += Input.GetAxis("Mouse X") * lookSensitivity;
        rotY -= Input.GetAxis("Mouse Y") * lookSensitivity;
        rotY = Mathf.Clamp(rotY, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotY, rotX, 0f);
    }

    void HandleZoomKeys()
    {
        if (Input.GetKeyDown(zoomInKey))
        {
            cam.fieldOfView = Mathf.Max(minFOV, cam.fieldOfView - zoomStep);
        }
        if (Input.GetKeyDown(zoomOutKey))
        {
            cam.fieldOfView = Mathf.Min(maxFOV, cam.fieldOfView + zoomStep);
        }
    }
}
