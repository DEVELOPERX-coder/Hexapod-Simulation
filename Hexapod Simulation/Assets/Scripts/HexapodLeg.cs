using UnityEngine;

/// <summary>
/// Controls a single leg of a hexapod with proper orientation handling
/// </summary>
public class HexapodLeg : MonoBehaviour
{
    [Header("Leg Segments")]
    public Transform hip;
    public Transform femur;
    public Transform tibia;

    [Header("Leg Movement Parameters")]
    [Tooltip("Minimum angle for hip rotation (usually negative)")]
    public float hipRotationLimitMin = -60f;
    [Tooltip("Maximum angle for hip rotation (usually positive)")]
    public float hipRotationLimitMax = 60f;
    [Tooltip("Minimum angle for femur rotation (usually negative)")]
    public float femurRotationLimitMin = -90f;
    [Tooltip("Maximum angle for femur rotation (usually positive)")]
    public float femurRotationLimitMax = 90f;
    [Tooltip("Minimum angle for tibia rotation (usually negative)")]
    public float tibiaRotationLimitMin = -120f;
    [Tooltip("Maximum angle for tibia rotation (usually 0 or slightly positive)")]
    public float tibiaRotationLimitMax = 0f;

    [Header("IK Settings")]
    public Transform footTarget;
    public float femurLength = 0.45f;
    public float tibiaLength = 0.65f;
    public float groundClearance = 0.05f;

    [Header("Debug")]
    public bool debugDraw = false;

    private Vector3 defaultFootPosition;
    private bool setupComplete = false;
    private bool isRightSide; // Determines if this is a right or left leg

    void Awake()
    {
        // Determine if this is a right or left leg based on name or position
        string legName = gameObject.name.ToLower();
        isRightSide = legName.Contains("_r") || legName.Contains("right");

        // If we couldn't determine from name, use position
        if (transform.localPosition.x > 0)
            isRightSide = true;
        else if (transform.localPosition.x < 0)
            isRightSide = false;
    }

    void Start()
    {
        SetupLeg();
    }

    /// <summary>
    /// Sets up the leg and its default positions
    /// </summary>
    public void SetupLeg()
    {
        // Make sure we have all the joints
        if (hip == null || femur == null || tibia == null)
        {
            Debug.LogError("Leg joints not properly assigned on " + gameObject.name);
            return;
        }

        // If footTarget isn't assigned, create one
        if (footTarget == null)
        {
            GameObject target = new GameObject(name + "_FootTarget");
            footTarget = target.transform;
            footTarget.SetParent(transform);

            // Position the foot target at the end of the tibia
            Vector3 footPos = tibia.position + (Vector3.down * tibiaLength);
            footTarget.position = footPos;
        }

        // Store the default foot position in local space
        defaultFootPosition = transform.InverseTransformPoint(footTarget.position);

        // Mark setup as complete
        setupComplete = true;
    }

    void Update()
    {
        // Make sure the leg is set up
        if (!setupComplete)
        {
            SetupLeg();
            return;
        }

        // Solve the IK for the leg
        SolveIK();
    }

    /// <summary>
    /// Solves inverse kinematics for the leg to reach the foot target
    /// Improved version with correct orientation handling
    /// </summary>
    public void SolveIK()
    {
        if (!setupComplete || hip == null || femur == null || tibia == null || footTarget == null)
        {
            Debug.LogWarning("Leg not fully set up: " + name);
            return;
        }

        try
        {
            // IMPORTANT: We need to handle the leg orientation correctly
            // For a right leg: Positive hip angle rotates toward body center
            // For a left leg: Positive hip angle rotates away from body center

            // 1. Get the target position in local space relative to the leg root
            Vector3 rootPos = transform.position;
            Vector3 targetPos = footTarget.position;
            Vector3 targetVec = targetPos - rootPos;

            // 2. Calculate hip rotation (horizontal plane rotation)
            // Project target vector onto the horizontal plane
            Vector3 targetHorizontal = new Vector3(targetVec.x, 0, targetVec.z);

            // Calculate the desired hip angle
            float hipAngle = Mathf.Atan2(targetHorizontal.x, targetHorizontal.z) * Mathf.Rad2Deg;

            // Apply side-specific logic
            if (!isRightSide)
            {
                // For left legs, we need to adjust the angle calculation
                // Left legs have mirrored coordinate systems
                hipAngle = Mathf.Atan2(-targetHorizontal.x, targetHorizontal.z) * Mathf.Rad2Deg;
            }

            // Clamp the hip rotation to valid limits
            hipAngle = Mathf.Clamp(hipAngle, hipRotationLimitMin, hipRotationLimitMax);

            // Apply the hip rotation - this only rotates around the Y axis
            hip.localRotation = Quaternion.Euler(0, hipAngle, 0);

            // 3. Calculate femur and tibia rotations after hip rotation is applied
            // We need the target in the femur's parent space (hip joint)
            Vector3 targetFromHip = hip.InverseTransformPoint(targetPos);

            // Adjust for left/right side coordinate systems if needed
            if (!isRightSide)
            {
                // Mirror the X coordinate to handle left-side legs properly
                targetFromHip.x = -targetFromHip.x;
            }

            // Get the 2D distance in the Y-Z plane (after accounting for hip rotation)
            float targetDistanceYZ = new Vector2(targetFromHip.y, targetFromHip.z).magnitude;

            // Clamp the distance to what's physically reachable
            float maxReach = femurLength + tibiaLength;
            targetDistanceYZ = Mathf.Clamp(targetDistanceYZ, 0.01f, maxReach - 0.01f);

            // Use the law of cosines to calculate knee (tibia) angle
            float cosKneeAngle = (femurLength * femurLength + tibiaLength * tibiaLength - targetDistanceYZ * targetDistanceYZ) /
                               (2 * femurLength * tibiaLength);

            // Ensure valid range for arc cosine
            cosKneeAngle = Mathf.Clamp(cosKneeAngle, -1f, 1f);

            // Calculate knee angle (negative because knee bends backward)
            float kneeAngle = -Mathf.Acos(cosKneeAngle) * Mathf.Rad2Deg;

            // Calculate angle of femur relative to local Y axis
            float cosFemurAngle = (femurLength * femurLength + targetDistanceYZ * targetDistanceYZ - tibiaLength * tibiaLength) /
                                (2 * femurLength * targetDistanceYZ);

            // Ensure valid range for arc cosine  
            cosFemurAngle = Mathf.Clamp(cosFemurAngle, -1f, 1f);

            // Calculate femur angle
            float femurAngle = Mathf.Acos(cosFemurAngle) * Mathf.Rad2Deg;

            // Adjust for the target's height relative to hip
            float targetHeightAngle = Mathf.Atan2(targetFromHip.y, targetFromHip.z) * Mathf.Rad2Deg;
            femurAngle = targetHeightAngle + femurAngle;

            // Ensure knee angle is within limits
            kneeAngle = Mathf.Clamp(kneeAngle, tibiaRotationLimitMin, tibiaRotationLimitMax);

            // Ensure femur angle is within limits
            femurAngle = Mathf.Clamp(femurAngle, femurRotationLimitMin, femurRotationLimitMax);

            // Apply the calculated angles
            femur.localRotation = Quaternion.Euler(femurAngle, 0, 0);
            tibia.localRotation = Quaternion.Euler(kneeAngle, 0, 0);

            if (debugDraw)
            {
                Debug.DrawLine(hip.position, femur.position, Color.red);
                Debug.DrawLine(femur.position, tibia.position, Color.green);
                Debug.DrawLine(tibia.position, footTarget.position, Color.blue);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error solving IK for leg " + name + ": " + e.Message);
        }
    }

    /// <summary>
    /// Sets the foot target position (used by the controller for walking)
    /// </summary>
    public void SetFootPosition(Vector3 localPosition)
    {
        if (footTarget != null)
        {
            footTarget.position = transform.TransformPoint(localPosition);
        }
    }

    /// <summary>
    /// Gets the default foot position in local space
    /// </summary>
    public Vector3 GetDefaultFootPosition()
    {
        return defaultFootPosition;
    }

    /// <summary>
    /// Adjusts the foot position to match the ground height
    /// </summary>
    public void AdaptToGround()
    {
        if (footTarget == null)
            return;

        RaycastHit hit;
        // Start the ray from above the foot
        Vector3 rayStart = footTarget.position + Vector3.up * 0.5f;

        // Cast a ray downward to find the ground
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1f))
        {
            // Get the current position and adjust only the Y value to match ground + clearance
            Vector3 newPos = footTarget.position;
            newPos.y = hit.point.y + groundClearance;
            footTarget.position = newPos;
        }
    }

    /// <summary>
    /// Draw debug visualization for this leg
    /// </summary>
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !setupComplete)
            return;

        // Draw the leg chain
        if (hip != null && femur != null && tibia != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(hip.position, femur.position);
            Gizmos.DrawLine(femur.position, tibia.position);

            // Draw line to foot target
            if (footTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(tibia.position, footTarget.position);

                // Draw the foot target
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(footTarget.position, 0.02f);

                // Draw the default foot position
                if (debugDraw)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 defaultPos = transform.TransformPoint(defaultFootPosition);
                    Gizmos.DrawSphere(defaultPos, 0.015f);
                }
            }
        }
    }
}