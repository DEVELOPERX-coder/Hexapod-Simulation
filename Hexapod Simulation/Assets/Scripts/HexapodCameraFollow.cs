using UnityEngine;

/// <summary>
/// Camera controller to follow the hexapod with various view options
/// </summary>
public class HexapodCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;               // The hexapod to follow
    public float followSpeed = 5.0f;       // How quickly the camera follows the target
    public float rotationSpeed = 3.0f;     // How quickly the camera rotates

    [Header("Position Settings")]
    public float distance = 3.0f;          // Distance from the target
    public float height = 1.5f;            // Height above the target
    public float offsetForward = 0.0f;     // Offset forward/backward from the target

    [Header("View Options")]
    public ViewMode currentViewMode = ViewMode.ThirdPerson;
    public enum ViewMode
    {
        ThirdPerson,                       // Follow behind the hexapod
        TopDown,                           // Look down at the hexapod
        FirstPerson,                       // View from the hexapod's perspective
        Orbital                            // Orbit around the hexapod
    }

    [Header("Control Options")]
    public bool allowMouseRotation = true; // Allow the player to rotate the camera with mouse
    public float mouseRotationSpeed = 2.0f;
    public bool smoothFollow = true;       // Use smooth follow or snap immediately
    public LayerMask collisionLayers;      // Layers that the camera will collide with

    // Private variables
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentRotationY = 0f;
    private float currentRotationX = 0f;
    private bool isFirstPersonView = false;
    private float orbitalAngle = 0f;

    private void Start()
    {
        // Find the hexapod if not assigned
        if (target == null)
        {
            GameObject hexapod = GameObject.FindWithTag("Player");
            if (hexapod == null)
                hexapod = GameObject.Find("Hexapod");

            if (hexapod != null)
                target = hexapod.transform;
            else
                Debug.LogError("HexapodCameraFollow: No target assigned and no hexapod found in scene!");
        }

        // Initialize camera position
        if (target != null)
        {
            UpdateCameraPosition();

            // Set initial rotation based on target
            currentRotationY = target.eulerAngles.y;
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Handle camera controls first
        HandleCameraControls();

        // Update the target position and rotation based on current view mode
        UpdateCameraPosition();

        // Apply smooth movement or snap
        if (smoothFollow)
        {
            // Smoothly move the camera towards the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

            // Smoothly rotate the camera towards the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Immediately snap to the target position and rotation
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Update the camera's target position and rotation based on view mode
    /// </summary>
    private void UpdateCameraPosition()
    {
        switch (currentViewMode)
        {
            case ViewMode.ThirdPerson:
                UpdateThirdPersonView();
                break;

            case ViewMode.TopDown:
                UpdateTopDownView();
                break;

            case ViewMode.FirstPerson:
                UpdateFirstPersonView();
                break;

            case ViewMode.Orbital:
                UpdateOrbitalView();
                break;
        }

        // Handle camera collision
        HandleCameraCollision();
    }

    /// <summary>
    /// Update the camera for third-person view (following behind)
    /// </summary>
    private void UpdateThirdPersonView()
    {
        isFirstPersonView = false;

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);

        // Calculate position
        Vector3 targetPos = target.position;
        Vector3 backward = rotation * -Vector3.forward;
        Vector3 right = rotation * Vector3.right;

        targetPosition = targetPos + backward * distance + Vector3.up * height + rotation * Vector3.forward * offsetForward;
        targetRotation = Quaternion.LookRotation(targetPos - targetPosition + Vector3.up * height * 0.5f);
    }

    /// <summary>
    /// Update the camera for top-down view
    /// </summary>
    private void UpdateTopDownView()
    {
        isFirstPersonView = false;

        // Set position directly above target
        targetPosition = target.position + Vector3.up * height * 2.5f;

        // Look directly down at the target
        targetRotation = Quaternion.Euler(90, 0, 0);
    }

    /// <summary>
    /// Update the camera for first-person view (from the hexapod's perspective)
    /// </summary>
    private void UpdateFirstPersonView()
    {
        isFirstPersonView = true;

        // Position just above the target's head
        targetPosition = target.position + Vector3.up * height * 0.5f;

        // Use the current rotation for looking around
        targetRotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
    }

    /// <summary>
    /// Update the camera for orbital view (rotating around the target)
    /// </summary>
    private void UpdateOrbitalView()
    {
        isFirstPersonView = false;

        // Automatically increment the orbital angle
        orbitalAngle += Time.deltaTime * 15f; // 15 degrees per second

        // Calculate position in a circle around the target
        float x = Mathf.Sin(orbitalAngle * Mathf.Deg2Rad) * distance;
        float z = Mathf.Cos(orbitalAngle * Mathf.Deg2Rad) * distance;

        targetPosition = target.position + new Vector3(x, height, z);
        targetRotation = Quaternion.LookRotation(target.position - targetPosition + Vector3.up * height * 0.25f);
    }

    /// <summary>
    /// Handle player camera control inputs
    /// </summary>
    private void HandleCameraControls()
    {
        // Change camera mode with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentViewMode = ViewMode.ThirdPerson;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            currentViewMode = ViewMode.TopDown;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            currentViewMode = ViewMode.FirstPerson;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            currentViewMode = ViewMode.Orbital;

        // Handle mouse rotation if allowed
        if (allowMouseRotation && Input.GetMouseButton(1)) // Right mouse button held
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseRotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * mouseRotationSpeed;

            currentRotationY += mouseX;

            // Limit the up/down rotation to avoid flipping
            currentRotationX -= mouseY;
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);
        }

        // If in first-person mode, sync rotation with target's forward direction
        if (isFirstPersonView && currentViewMode == ViewMode.FirstPerson)
        {
            target.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }
    }

    /// <summary>
    /// Handle camera collision detection to prevent clipping through walls
    /// </summary>
    private void HandleCameraCollision()
    {
        if (currentViewMode == ViewMode.FirstPerson)
            return; // No collision handling needed in first-person

        RaycastHit hit;
        Vector3 directionToTarget = target.position - targetPosition;

        // Check if there's anything between the camera and the target
        if (Physics.Raycast(target.position, -directionToTarget.normalized, out hit, distance, collisionLayers))
        {
            // If we hit something, adjust the camera position
            float distanceToObstacle = hit.distance;
            targetPosition = target.position - directionToTarget.normalized * (distanceToObstacle - 0.1f);
        }
    }

    /// <summary>
    /// Switch to a specific view mode
    /// </summary>
    public void SwitchViewMode(ViewMode newMode)
    {
        currentViewMode = newMode;
    }

    /// <summary>
    /// Draw gizmos for visualization in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.2f);
    }
}