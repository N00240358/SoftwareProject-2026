using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the orthographic game camera. Supports keyboard movement, edge panning,
/// middle-mouse drag, scroll-wheel zoom, and hard clamping to map bounds.
/// </summary>
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

    /// <summary>Translates the camera using the Unity Input Horizontal/Vertical axes.</summary>
    private void HandleKeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    /// <summary>
    /// Pans the camera when the mouse cursor is within <see cref="edgePanBorder"/> pixels of any screen edge.
    /// No-ops when the cursor is over a UI element or <see cref="enableEdgePan"/> is false.
    /// </summary>
    private void HandleEdgePan()
    {
        if (!enableEdgePan) return;

        // Don't pan when the mouse is over a UI panel
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

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

    /// <summary>Pans the camera by dragging with the middle mouse button (button 2).</summary>
    private void HandleMouseDrag()
    {
        if (cam == null) return;

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

    /// <summary>Adjusts orthographic size based on scroll wheel input, clamped between <see cref="minZoom"/> and <see cref="maxZoom"/>.</summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0 && cam != null)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// Prevents the camera from showing empty space beyond the map edges.
    /// Accounts for the orthographic viewport half-extents so the full viewport stays inside bounds.
    /// </summary>
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

    /// <summary>
    /// Instantly moves the camera to center on the given world position (preserves Z depth).
    /// </summary>
    public void FocusOnPosition(Vector2 position)
    {
        Vector3 targetPos = new Vector3(position.x, position.y, transform.position.z);
        transform.position = targetPos;
    }

    /// <summary>
    /// Updates the camera clamp bounds to match the generated map size.
    /// Call this after MapGenerator.GenerateMap() to prevent the camera from leaving the map.
    /// </summary>
    public void SetCameraBounds(float mapWidth, float mapHeight)
    {
        minX = 0f;
        maxX = mapWidth;
        minY = 0f;
        maxY = mapHeight;
    }
}
