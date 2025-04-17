using UnityEngine;

/// <summary>
/// Controls a single leg of a hexapod robot, handling IK and positioning
/// </summary>
public class HexapodLeg : MonoBehaviour
{
    [Header("Leg Segments")]
    public Transform hip;
    public Transform femur;
    public Transform tibia;

    [Header("Leg Movement Parameters")]
    public float hipRotationLimit = 45f;
    public float femurRotationLimit = 90f;
    public float tibiaRotationLimit = 135f;

    [Header("IK Settings")]
    public Transform footTarget;
    public float femurLength = 0.3f;
    public float tibiaLength = 0.4f;
    public float groundClearance = 0.05f;

    private Vector3 defaultFootPosition;
    private bool setupComplete = false;

    void Start()
    {
        SetupLeg();
    }

    /// <summary>
    /// Sets up the leg and its default positions
    /// </summary>
    public void SetupLeg()
    {
        // If footTarget isn't assigned, create one
        if (footTarget == null)
        {
            GameObject target = new GameObject(name + "_FootTarget");
            footTarget = target.transform;
            footTarget.SetParent(transform);

            // Position it at the end of the leg chain
            if (tibia != null)
            {
                footTarget.position = tibia.position + Vector3.down * tibiaLength;
            }
            else
            {
                footTarget.localPosition = new Vector3(0, -femurLength - tibiaLength, 0);
            }
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
        }

        // Solve the IK for the leg
        SolveIK();
    }

    /// <summary>
    /// Solves inverse kinematics for the leg to reach the foot target
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
            // Get the target position in local space of the hip joint
            Vector3 hipPos = hip.position;
            Vector3 targetPos = footTarget.position;

            // Calculate the direction from hip to target
            Vector3 targetDir = targetPos - hipPos;

            // 1. First, rotate the hip joint to point towards the target in the horizontal plane
            float hipAngle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
            hip.localRotation = Quaternion.Euler(0, hipAngle, 0);

            // Get the new forward direction after hip rotation
            Vector3 hipForward = hip.forward;

            // Project the target onto the plane defined by the hip forward direction
            float targetDistance = Vector3.Project(targetDir, hipForward).magnitude;

            // Get the height difference
            float heightDiff = targetPos.y - hipPos.y;

            // 2. Calculate the knee and ankle angles using two-bone IK
            float targetDistanceSqr = targetDistance * targetDistance + heightDiff * heightDiff;
            float targetDistanceFromHip = Mathf.Sqrt(targetDistanceSqr);

            // Clamp the target distance to the possible range
            targetDistanceFromHip = Mathf.Clamp(targetDistanceFromHip, 0.01f, femurLength + tibiaLength - 0.01f);

            // Use the law of cosines to find the angles
            float cosKneeAngle = (femurLength * femurLength + tibiaLength * tibiaLength - targetDistanceSqr) / (2f * femurLength * tibiaLength);
            cosKneeAngle = Mathf.Clamp(cosKneeAngle, -1f, 1f); // Ensure the value is valid for acos
            float kneeAngle = Mathf.Acos(cosKneeAngle) * Mathf.Rad2Deg;

            // Calculate the femur angle relative to horizontal
            float cosFemurAngle = (femurLength * femurLength + targetDistanceSqr - tibiaLength * tibiaLength) / (2f * femurLength * targetDistanceFromHip);
            cosFemurAngle = Mathf.Clamp(cosFemurAngle, -1f, 1f);
            float femurAngle = Mathf.Acos(cosFemurAngle) * Mathf.Rad2Deg;

            // Adjust for height difference
            float femurPitchOffset = Mathf.Atan2(heightDiff, targetDistance) * Mathf.Rad2Deg;
            femurAngle += femurPitchOffset;

            // 3. Apply the rotations to the joints
            femur.localRotation = Quaternion.Euler(femurAngle, 0, 0);
            tibia.localRotation = Quaternion.Euler(-kneeAngle, 0, 0);
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
            Vector3 worldPosition = transform.TransformPoint(localPosition);
            footTarget.position = worldPosition;
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
            }
        }
    }
}