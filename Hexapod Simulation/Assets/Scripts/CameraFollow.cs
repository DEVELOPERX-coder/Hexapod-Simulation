using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -5);

    [Header("Follow Settings")]
    public float smoothSpeed = 5.0f;
    public float rotationSmoothing = 5.0f;
    public bool followRotation = true;

    [Header("Look Settings")]
    public bool lookAtTarget = true;
    public Vector3 lookOffset = new Vector3(0, 1, 0);

    [Header("Control Settings")]
    public bool allowManualControl = true;
    public float zoomSpeed = 5.0f;
    public float rotateSpeed = 3.0f;
    public float minZoomDistance = 2.0f;
    public float maxZoomDistance = 10.0f;

    // Internal variables
    private float currentZoomDistance;
    private Vector3 currentOffset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentYRotation = 0f;

    void Start()
    {
        // If no target specified, try to find the quadruped
        if (target == null)
        {
            QuadrupedGenerator quadruped = FindObjectOfType<QuadrupedGenerator>();
            if (quadruped != null)
            {
                target = quadruped.transform;
            }
            else
            {
                Debug.LogError("No target assigned to CameraFollow and no QuadrupedGenerator found in scene!");
            }
        }

        // Initialize zoom distance
        currentZoomDistance = offset.magnitude;
        currentOffset = offset.normalized * currentZoomDistance;

        // Initialize the rotation based on the offset
        currentYRotation = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Handle manual control if enabled
        if (allowManualControl)
        {
            HandleManualControl();
        }

        // Calculate target position and rotation
        CalculateTargetPositionAndRotation();

        // Smoothly move towards target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Look at target if enabled
        if (lookAtTarget)
        {
            Vector3 lookPosition = target.position + lookOffset;
            transform.LookAt(lookPosition);
        }
        else if (followRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
        }
    }

    void HandleManualControl()
    {
        // Zoom control with scroll wheel
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoomDistance -= scrollInput * zoomSpeed;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
        }

        // Rotation control with right mouse button
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX != 0)
            {
                currentYRotation += mouseX * rotateSpeed;
            }

            // Disable look at target when manually rotating
            lookAtTarget = false;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            // Re-enable look at target when releasing right mouse button
            lookAtTarget = true;
        }

        // Update offset based on current zoom and rotation
        float offsetX = Mathf.Sin(currentYRotation * Mathf.Deg2Rad) * currentZoomDistance;
        float offsetZ = Mathf.Cos(currentYRotation * Mathf.Deg2Rad) * currentZoomDistance;
        currentOffset = new Vector3(offsetX, offset.y, offsetZ);
    }

    void CalculateTargetPositionAndRotation()
    {
        // Calculate target position
        if (followRotation)
        {
            targetPosition = target.position + target.rotation * currentOffset;
            targetRotation = target.rotation;
        }
        else
        {
            targetPosition = target.position + currentOffset;

            // If not looking at target, maintain camera's world rotation
            if (!lookAtTarget)
            {
                targetRotation = Quaternion.Euler(0, currentYRotation, 0);
            }
        }
    }
}