using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float moveSpeed = 10f;
    public float edgePanSpeed = 5f;
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    
    [Header("Edge Pan Settings")]
    public float edgePanBorder = 10f;
    public bool enableEdgePan = true;
    
    [Header("Camera Limits")]
    public float minX = 0f;
    public float maxX = 200f;
    public float minY = 0f;
    public float maxY = 100f;
    
    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        // Set orthographic camera
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10f;
        }
        
        // Center camera on map
        Vector3 centerPos = new Vector3(maxX / 2f, maxY / 2f, -10f);
        transform.position = centerPos;
    }

    private void Update()
    {
        HandleKeyboardMovement();
        HandleEdgePan();
        HandleMouseDrag();
        HandleZoom();
        ClampCameraPosition();
    }

    private void HandleKeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    private void HandleEdgePan()
    {
        if (!enableEdgePan) return;
        
        Vector3 mousePos = Input.mousePosition;
        Vector3 movement = Vector3.zero;
        
        // Check screen edges
        if (mousePos.x < edgePanBorder)
        {
            movement.x -= edgePanSpeed * Time.deltaTime;
        }
        else if (mousePos.x > Screen.width - edgePanBorder)
        {
            movement.x += edgePanSpeed * Time.deltaTime;
        }
        
        if (mousePos.y < edgePanBorder)
        {
            movement.y -= edgePanSpeed * Time.deltaTime;
        }
        else if (mousePos.y > Screen.height - edgePanBorder)
        {
            movement.y += edgePanSpeed * Time.deltaTime;
        }
        
        transform.position += movement;
    }

    private void HandleMouseDrag()
    {
        // Middle mouse button drag
        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        
        if (Input.GetMouseButton(2) && isDragging)
        {
            Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = dragOrigin - currentMousePos;
            transform.position += diff;
        }
        
        if (Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0 && cam != null)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    private void ClampCameraPosition()
    {
        if (cam == null) return;
        
        // Calculate camera bounds based on orthographic size
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;
        
        // Clamp position
        float clampedX = Mathf.Clamp(transform.position.x, minX + horzExtent, maxX - horzExtent);
        float clampedY = Mathf.Clamp(transform.position.y, minY + vertExtent, maxY - vertExtent);
        
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    public void FocusOnPosition(Vector2 position)
    {
        Vector3 targetPos = new Vector3(position.x, position.y, transform.position.z);
        transform.position = targetPos;
    }

    public void SetCameraBounds(float mapWidth, float mapHeight)
    {
        minX = 0f;
        maxX = mapWidth;
        minY = 0f;
        maxY = mapHeight;
    }
}
