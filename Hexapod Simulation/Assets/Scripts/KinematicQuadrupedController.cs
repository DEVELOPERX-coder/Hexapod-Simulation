// Get foot transform based on the new hierarchy structure
using System.Collections.Generic;
using UnityEngine;

public enum KinematicGaitType
{
    Trot,   // Diagonal legs move together (FR+BL, FL+BR)
    Walk,   // One leg at a time in sequence
    Pace,   // Legs on same side move together (FR+BR, FL+BL)
    Bound   // Front legs together, back legs together
}

public class KinematicQuadrupedController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.0f;        // Reduced for smoother movement
    public float turnSpeed = 40.0f;       // Reduced for smoother turning
    public float stepHeight = 0.25f;      // Increased for better leg lifting
    public float groundClearance = 0.3f;  // Adjusted for better foot placement
    public KinematicGaitType gaitType = KinematicGaitType.Trot;

    [Header("IK Settings")]
    public float stepDuration = 0.7f;     // Longer duration for smoother movement
    public float strideLength = 0.35f;    // Shorter stride for more stable movement
    public float smoothingFactor = 12f;   // Higher value for smoother leg movement

    // Leg group definitions for different gaits
    private int[] trotLegGroups = { 0, 1, 1, 0 };     // FR, FL, BR, BL (diagonal pairs)
    private int[] paceLegGroups = { 0, 1, 0, 1 };     // FR, FL, BR, BL (side pairs)
    private int[] boundLegGroups = { 0, 0, 1, 1 };    // FR, FL, BR, BL (front/back pairs)

    // Internal variables
    private List<Transform[]> legs;
    private Vector3[] defaultFootPositions;
    private Vector3[] targetFootPositions;
    private bool[] legMoving;
    private float[] legTimers;

    // For gait control
    private int currentLegGroup = 0;
    private float lastStepTime = 0f;

    // Default leg positions in local space
    private Vector3[] defaultLegPositions = new Vector3[4];
    private float[] defaultLegAngles = new float[4]
    {
        45f,    // Front Right
        135f,   // Front Left
        315f,   // Back Right (changed from -45 to 315 for outward orientation)
        225f,   // Back Left (changed from -135 to 225 for outward orientation)
    };

private Transform GetFootTransform(int legIndex)
{
    Transform tibiaJoint = legs[legIndex][2];

    // Find TibiaSegment
    Transform tibiaSegment = null;
    for (int i = 0; i < tibiaJoint.childCount; i++)
    {
        if (tibiaJoint.GetChild(i).name.Contains("TibiaSegment"))
        {
            tibiaSegment = tibiaJoint.GetChild(i);
            break;
        }
    }

    if (tibiaSegment == null) return null;

    // Find Foot under TibiaSegment
    for (int i = 0; i < tibiaSegment.childCount; i++)
    {
        if (tibiaSegment.GetChild(i).name.Contains("Foot"))
        {
            return tibiaSegment.GetChild(i);
        }
    }

    return null;
}
    void Start()
    {
        // Add a delay to ensure all components are initialized
        Invoke("InitializeQuadruped", 0.1f);
    }

    void InitializeQuadruped()
    {
        // Get reference to the quadruped generator
        StableQuadrupedGenerator generator = GetComponent<StableQuadrupedGenerator>();
        if (generator != null)
        {
            legs = generator.GetLegs();
        }
        else
        {
            Debug.LogError("StableQuadrupedGenerator component not found!");
            return;
        }

        if (legs.Count != 4)
        {
            Debug.LogError("Expected 4 legs but found " + legs.Count);
            return;
        }

        // Initialize arrays
        int legCount = legs.Count;
        defaultFootPositions = new Vector3[legCount];
        targetFootPositions = new Vector3[legCount];
        legMoving = new bool[legCount];
        legTimers = new float[legCount];

        // Set default positions based on body dimensions
        float bodyLength = generator.bodyLength;
        float bodyWidth = generator.bodyWidth;
        defaultLegPositions[0] = new Vector3(bodyLength / 2, 0, bodyWidth / 2);     // Front Right
        defaultLegPositions[1] = new Vector3(bodyLength / 2, 0, -bodyWidth / 2);    // Front Left
        defaultLegPositions[2] = new Vector3(-bodyLength / 2, 0, bodyWidth / 2);    // Back Right
        defaultLegPositions[3] = new Vector3(-bodyLength / 2, 0, -bodyWidth / 2);   // Back Left

        // Calculate initial foot positions - update this to find the actual foot position
        for (int i = 0; i < legCount; i++)
        {
            // Get the foot transform based on the new hierarchy
            Transform hipJoint = legs[i][0];
            Transform femurJoint = legs[i][1];
            Transform tibiaJoint = legs[i][2];

            // Navigate through hierarchy to find foot:
            // tibiaJoint > tibiaSegment > Foot
            Transform tibiaSegment = null;
            for (int j = 0; j < tibiaJoint.childCount; j++)
            {
                if (tibiaJoint.GetChild(j).name.Contains("TibiaSegment"))
                {
                    tibiaSegment = tibiaJoint.GetChild(j);
                    break;
                }
            }

            if (tibiaSegment == null)
            {
                Debug.LogError("Tibia segment not found for leg " + i);
                continue;
            }

            Transform foot = null;
            for (int j = 0; j < tibiaSegment.childCount; j++)
            {
                if (tibiaSegment.GetChild(j).name.Contains("Foot"))
                {
                    foot = tibiaSegment.GetChild(j);
                    break;
                }
            }

            if (foot == null)
            {
                Debug.LogError("Foot not found for leg " + i);
                continue;
            }

            // Store foot position in world space
            defaultFootPositions[i] = foot.position;
            targetFootPositions[i] = foot.position;

            // Set initial leg positions for a good stance
            float hipAngle = defaultLegAngles[i];
            float femurAngle = -30f; // Angle down
            float tibiaAngle = 60f;  // Angle to make foot touch ground

            // Position the legs in a standing pose - these are now hierarchy local rotations
            hipJoint.localRotation = Quaternion.Euler(0, hipAngle, 0);
            femurJoint.localRotation = Quaternion.Euler(0, 0, femurAngle);
            tibiaJoint.localRotation = Quaternion.Euler(0, 0, tibiaAngle);
        }

        // Position the quadruped at the right height
        float legLength = CalculateLegLength(generator);
        transform.position = new Vector3(
            transform.position.x,
        legLength * 0.65f, // Position at appropriate height
            transform.position.z
        );
    }

    private float CalculateLegLength(StableQuadrupedGenerator generator)
    {
        // Return the total leg length
        return generator.hipLength + generator.femurLength + generator.tibiaLength;
    }

    void Update()
    {
        // Get input for movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D or left/right
        float vertical = Input.GetAxis("Vertical");     // W/S or up/down

        if (vertical != 0 || horizontal != 0)
        {
            // Move the quadruped
            MoveQuadruped(vertical, horizontal);
        }

        // Update leg positions
        UpdateLegs();
    }

    void MoveQuadruped(float forward, float turn)
    {
        // Calculate movement direction
        Vector3 movement = transform.forward * forward * moveSpeed * Time.deltaTime;
        float rotationAmount = turn * turnSpeed * Time.deltaTime;

        // Move and rotate the entire robot
        transform.position += movement;
        transform.Rotate(0, rotationAmount, 0);

        // Update leg targets based on movement
        UpdateLegTargets(forward, turn);
    }

    void UpdateLegTargets(float forward, float turn)
    {
        // Only take a step if enough time has passed since last step
        if (Time.time - lastStepTime < stepDuration * 0.8f)
        {
            return;
        }

        // Determine which legs to move based on the gait type
        List<int> legsToMove = new List<int>();
        int[] currentGaitGroups;

        // Select the appropriate gait pattern
        switch (gaitType)
        {
            case KinematicGaitType.Trot:
                currentGaitGroups = trotLegGroups;
                break;

            case KinematicGaitType.Pace:
                currentGaitGroups = paceLegGroups;
                break;

            case KinematicGaitType.Bound:
                currentGaitGroups = boundLegGroups;
                break;

            case KinematicGaitType.Walk:
                // For walking, we move one leg at a time
                legsToMove.Add(currentLegGroup);
                currentLegGroup = (currentLegGroup + 1) % 4;
                lastStepTime = Time.time;

                // Calculate stride vector
                CalculateStridesAndMoveLegs(legsToMove, forward, turn);
                return;

            default:
                currentGaitGroups = trotLegGroups;
                break;
        }

        // Find all legs in the current group that aren't already moving
        for (int i = 0; i < legs.Count; i++)
        {
            if (currentGaitGroups[i] == currentLegGroup && !legMoving[i])
            {
                legsToMove.Add(i);
            }
        }

        // Only proceed if we have legs to move
        if (legsToMove.Count > 0)
        {
            // Switch to the next leg group
            currentLegGroup = (currentLegGroup + 1) % 2;
            lastStepTime = Time.time;

            // Calculate strides and move the legs
            CalculateStridesAndMoveLegs(legsToMove, forward, turn);
        }
    }

    void CalculateStridesAndMoveLegs(List<int> legsToMove, float forward, float turn)
    {
        // Calculate stride vector based on movement direction
        Vector3 strideVector = transform.forward * forward * strideLength;
        if (turn != 0)
        {
            strideVector += transform.right * turn * strideLength * 0.5f;
        }

        // Start moving the selected legs
        foreach (int legIndex in legsToMove)
        {
            // Calculate new target position
            Vector3 legPos = transform.TransformPoint(defaultLegPositions[legIndex]);

            // Get current foot position
            Transform foot = GetFootTransform(legIndex);
            if (foot == null) continue;

            Vector3 currentFootPos = foot.position;

            // Find a good target position
            Vector3 footTarget = legPos + strideVector;

            // Raycasting to find ground height
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(footTarget.x, transform.position.y + 1f, footTarget.z), Vector3.down, out hit, 10f))
            {
                footTarget.y = hit.point.y;
            }
            else
            {
                footTarget.y = transform.position.y - groundClearance;
            }

            // Start moving the leg
            targetFootPositions[legIndex] = footTarget;
            legMoving[legIndex] = true;
            legTimers[legIndex] = 0f;
        }
    }

    void UpdateLegs()
    {
        for (int i = 0; i < legs.Count; i++)
        {
            if (legMoving[i])
            {
                // Update leg timer
                legTimers[i] += Time.deltaTime;
                float normalizedTime = legTimers[i] / stepDuration;

                if (normalizedTime >= 1.0f)
                {
                    // Leg movement complete
                    legMoving[i] = false;

                    // IMPROVED: store the actual achieved foot position as the new target
                    Transform foot = GetFootTransform(i);
                    if (foot != null)
                    {
                        targetFootPositions[i] = foot.position;
                    }
                }
                else
                {
                    // IMPROVED: Use bezier curve for smoother, more natural arc

                    // Get starting position (this should be captured when movement starts)
                    Transform foot = GetFootTransform(i);
                    if (foot == null) continue;

                    Vector3 startPos = foot.position;

                    // Calculate a middle control point for the arc
                    Vector3 midPoint = Vector3.Lerp(startPos, targetFootPositions[i], 0.5f);
                    midPoint.y += stepHeight; // Raise the midpoint for an arc

                    // Use quadratic bezier curve for a natural arc motion
                    float t = normalizedTime;
                    Vector3 a = Vector3.Lerp(startPos, midPoint, t);
                    Vector3 b = Vector3.Lerp(midPoint, targetFootPositions[i], t);
                    Vector3 newPos = Vector3.Lerp(a, b, t);

                    // Update foot position using IK
                    PositionLegAtTarget(i, newPos);
                }
            }
            else
            {
                // IMPROVED: Better stationary leg behavior
                UpdateStationaryLeg(i);
            }
        }
    }

    // IMPROVED: Better handling of stationary legs
    void UpdateStationaryLeg(int legIndex)
    {
        // For stationary legs, we need to ensure they adapt to ground height changes
        // and maintain a good stance position

        Transform hipJoint = legs[legIndex][0];
        Transform tibiaJoint = legs[legIndex][2];
        Transform tibiaSegment = tibiaJoint.GetChild(0);
        Transform foot = tibiaSegment.GetChild(0);

        // Find an appropriate target position based on the body
        Vector3 defaultPos = transform.TransformPoint(defaultLegPositions[legIndex]);

        // Adjust default position downward to find the ground
        Vector3 rayStart = new Vector3(defaultPos.x, transform.position.y + 0.5f, defaultPos.z);
        RaycastHit hit;

        // Ray cast to find the ground
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
        {
            // Adjust the target position to be at ground level plus clearance
            Vector3 groundPos = hit.point;
            groundPos.y += 0.05f; // Small offset to prevent penetration

            // Smoothly update the target position
            targetFootPositions[legIndex] = Vector3.Lerp(
                targetFootPositions[legIndex],
                groundPos,
                Time.deltaTime * 5f
            );
        }

        // Position the leg
        PositionLegAtTarget(legIndex, targetFootPositions[legIndex]);
    }

    void PositionLegAtTarget(int legIndex, Vector3 targetPosition)
    {
        // Get the joints
        Transform hipJoint = legs[legIndex][0];
        Transform femurJoint = legs[legIndex][1];
        Transform tibiaJoint = legs[legIndex][2];

        // Get the lengths of each segment
        StableQuadrupedGenerator generator = GetComponent<StableQuadrupedGenerator>();
        float hipLength = generator.hipLength;
        float femurLength = generator.femurLength;
        float tibiaLength = generator.tibiaLength;

        // IMPROVED: First, get the hip position
        Vector3 hipPos = hipJoint.position;

        // IMPROVED: Convert target position to hip's parent space
        Vector3 localTarget = hipJoint.parent.InverseTransformPoint(targetPosition);

        // Calculate hip rotation angle (yaw)
        float hipAngle = defaultLegAngles[legIndex]; // Start with default angle

        // IMPROVED: Adjust hip angle based on target direction, but keep close to default
        float targetHipAngle = Mathf.Atan2(localTarget.z, localTarget.x) * Mathf.Rad2Deg;

        // IMPROVED: Blend between default angle and target angle
        // This keeps the legs pointing generally outward while still reaching targets
        float angleDiff = Mathf.DeltaAngle(hipAngle, targetHipAngle);
        float maxAngleAdjust = 30f; // Maximum degrees to adjust from default
        angleDiff = Mathf.Clamp(angleDiff, -maxAngleAdjust, maxAngleAdjust);
        hipAngle += angleDiff;

        // Temporarily set hip rotation to calculate distances correctly
        Quaternion originalHipRotation = hipJoint.localRotation;
        hipJoint.localRotation = Quaternion.Euler(0, hipAngle, 0);

        // IMPROVED: Calculate distance from hip joint to target position
        float targetDistance = Vector3.Distance(hipJoint.position, targetPosition);

        // Clamp distance to a reachable range
        float minReach = Mathf.Max((femurLength + tibiaLength) * 0.3f, Mathf.Abs(femurLength - tibiaLength));
        float maxReach = (femurLength + tibiaLength) * 0.95f;

        if (targetDistance < minReach || targetDistance > maxReach)
        {
            // Adjust target position to be within reach
            Vector3 direction = (targetPosition - hipJoint.position).normalized;
            float clampedDistance = Mathf.Clamp(targetDistance, minReach, maxReach);
            targetPosition = hipJoint.position + direction * clampedDistance;
        }

        // IMPROVED: Get target in hip joint's local space after setting hip angle
        Vector3 hipLocalTarget = hipJoint.InverseTransformPoint(targetPosition);

        // Project target onto 2D plane for IK calculations
        Vector2 target2D = new Vector2(
            Mathf.Sqrt(hipLocalTarget.x * hipLocalTarget.x + hipLocalTarget.z * hipLocalTarget.z),
            hipLocalTarget.y
        );

        // Safety check for very small distance
        if (target2D.magnitude < 0.01f)
        {
            // Default pose for tiny targets
            hipJoint.localRotation = Quaternion.Euler(0, hipAngle, 0);
            femurJoint.localRotation = Quaternion.Euler(0, 0, -45);
            tibiaJoint.localRotation = Quaternion.Euler(0, 0, 90);
            return;
        }

        // Calculate knee angle using law of cosines
        float kneeAngleCos = (femurLength * femurLength + tibiaLength * tibiaLength -
                             target2D.sqrMagnitude) / (2 * femurLength * tibiaLength);

        // Clamp to valid range to prevent NaN
        kneeAngleCos = Mathf.Clamp(kneeAngleCos, -0.99f, 0.99f);
        float kneeAngle = Mathf.Acos(kneeAngleCos) * Mathf.Rad2Deg;

        // Calculate femur angle
        float targetFemurAngle = Mathf.Atan2(target2D.y, target2D.x) * Mathf.Rad2Deg;

        // Apply law of cosines again for femur elevation
        float femurCos = (femurLength * femurLength + target2D.sqrMagnitude -
                          tibiaLength * tibiaLength) / (2 * femurLength * target2D.magnitude);

        femurCos = Mathf.Clamp(femurCos, -0.99f, 0.99f);
        float elevationAngle = Mathf.Acos(femurCos) * Mathf.Rad2Deg;

        // Total femur angle combines target direction and elevation
        float femurAngle = targetFemurAngle + elevationAngle;

        // IMPROVED: Smoothly interpolate to the target rotations
        Quaternion targetHipRot = Quaternion.Euler(0, hipAngle, 0);
        Quaternion targetFemurRot = Quaternion.Euler(0, 0, -femurAngle);
        Quaternion targetTibiaRot = Quaternion.Euler(0, 0, -(180 - kneeAngle));

        // Interpolate current rotations toward target rotations for smoother movement
        hipJoint.localRotation = Quaternion.Slerp(hipJoint.localRotation, targetHipRot,
                                                 Time.deltaTime * smoothingFactor);
        femurJoint.localRotation = Quaternion.Slerp(femurJoint.localRotation, targetFemurRot,
                                                   Time.deltaTime * smoothingFactor);
        tibiaJoint.localRotation = Quaternion.Slerp(tibiaJoint.localRotation, targetTibiaRot,
                                                   Time.deltaTime * smoothingFactor);
    }
}