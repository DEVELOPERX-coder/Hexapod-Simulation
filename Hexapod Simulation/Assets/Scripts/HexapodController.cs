using UnityEngine;

/// <summary>
/// Enhanced controller for hexapod with improved gait and movement
/// </summary>
public class HexapodController : MonoBehaviour
{
    [Header("Legs")]
    public HexapodLeg[] legs;

    [Header("Movement Parameters")]
    [Tooltip("Maximum forward/backward speed")]
    public float moveSpeed = 1.5f;
    [Tooltip("Maximum rotation speed in degrees per second")]
    public float turnSpeed = 60.0f;
    [Tooltip("How quickly the hexapod accelerates")]
    public float acceleration = 4.0f;
    [Tooltip("How quickly the hexapod decelerates")]
    public float deceleration = 6.0f;
    [Tooltip("How quickly turning speed changes")]
    public float turnAcceleration = 8.0f;

    [Header("Gait Parameters")]
    [Tooltip("Height of leg during swing phase")]
    public float stepHeight = 0.25f;
    [Tooltip("Duration of a complete gait cycle")]
    public float gaitCycleTime = 0.8f;
    [Tooltip("Multiplier for stride length")]
    public float strideFactor = 1.0f;
    [Tooltip("Vertical body oscillation during walking")]
    public float bodyOscillationHeight = 0.05f;
    [Tooltip("How much the body tilts during movement")]
    public float bodyTiltFactor = 2.0f;

    [Header("Gait Type")]
    public float test = 0;
    public enum GaitType { Tripod, Wave, Ripple }
    [Tooltip("Current walking pattern")]
    public GaitType currentGait = GaitType.Tripod;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Internal movement variables
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float currentTurnRate = 0f;
    private float targetTurnRate = 0f;
    private float cycleTimer = 0f;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 movementDirection = Vector3.forward;
    private bool isMoving = false;

    // Leg grouping for different gaits
    private HexapodLeg[][] legGroups;

    // Body animation
    private Vector3 defaultBodyPosition;
    private Quaternion defaultBodyRotation;

    // For stance phase adjustment
    private Transform bodyTransform;

    void Start()
    {
        // Validate legs array
        if (legs == null || legs.Length != 6)
        {
            Debug.LogError("Hexapod requires exactly 6 legs to be assigned in the inspector");
            enabled = false;
            return;
        }

        // Store body reference (either this transform or a child transform)
        bodyTransform = transform.Find("Body");
        if (bodyTransform == null)
            bodyTransform = transform;

        // Store initial body position and rotation
        defaultBodyPosition = bodyTransform.localPosition;
        defaultBodyRotation = bodyTransform.localRotation;

        // Set up the leg groups based on the current gait
        SetupLegGroups();

        // Initialize tracking variables
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // Get input for movement
        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Set target speed and turn rate based on input
        targetSpeed = forwardInput * moveSpeed;
        targetTurnRate = turnInput * turnSpeed;

        // Apply smooth acceleration/deceleration
        ApplyMovementSmoothing();

        // Move the hexapod body
        MoveHexapod();

        // Update the leg positions based on movement
        UpdateLegs();

        // Animate the body for more natural movement
        AnimateBody();

        // Allow changing gait types with keyboard
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeGait(GaitType.Tripod);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeGait(GaitType.Wave);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeGait(GaitType.Ripple);
    }

    /// <summary>
    /// Apply smoothing to movement and rotation
    /// </summary>
    private void ApplyMovementSmoothing()
    {
        // Smooth acceleration/deceleration
        if (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed))
        {
            // Accelerating
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            // Decelerating
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
        }

        // Smooth rotation
        currentTurnRate = Mathf.MoveTowards(currentTurnRate, targetTurnRate, turnAcceleration * Time.deltaTime);

        // Determine if the hexapod is moving
        isMoving = Mathf.Abs(currentSpeed) > 0.05f || Mathf.Abs(currentTurnRate) > 0.5f;
    }

    /// <summary>
    /// Sets up leg groups based on the selected gait pattern
    /// </summary>
    private void SetupLegGroups()
    {
        switch (currentGait)
        {
            case GaitType.Tripod:
                // Two groups of three legs each (tripod gait)
                // This is the most stable and common insect walking pattern
                legGroups = new HexapodLeg[2][];
                legGroups[0] = new HexapodLeg[] { legs[0], legs[3], legs[4] }; // FR, ML, RR
                legGroups[1] = new HexapodLeg[] { legs[1], legs[2], legs[5] }; // FL, MR, RL
                break;

            case GaitType.Wave:
                // Six groups of one leg each (wave gait)
                // Moves one leg at a time - very stable but slow
                legGroups = new HexapodLeg[6][];
                for (int i = 0; i < 6; i++)
                {
                    legGroups[i] = new HexapodLeg[] { legs[i] };
                }
                break;

            case GaitType.Ripple:
                // Three groups of two legs each (ripple gait)
                // Good balance between speed and stability
                legGroups = new HexapodLeg[3][];
                legGroups[0] = new HexapodLeg[] { legs[0], legs[3] }; // FR, ML
                legGroups[1] = new HexapodLeg[] { legs[4], legs[1] }; // RR, FL
                legGroups[2] = new HexapodLeg[] { legs[2], legs[5] }; // MR, RL
                break;
        }
    }

    /// <summary>
    /// Changes the current gait pattern
    /// </summary>
    public void ChangeGait(GaitType newGait)
    {
        if (currentGait != newGait)
        {
            currentGait = newGait;
            SetupLegGroups();
            cycleTimer = 0f; // Reset the cycle timer
        }
    }

    /// <summary>
    /// Moves and rotates the hexapod based on input
    /// </summary>
    void MoveHexapod()
    {
        // Only update movement direction if actually moving
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            movementDirection = transform.forward * Mathf.Sign(currentSpeed);
        }

        // Rotate the hexapod
        transform.Rotate(0, currentTurnRate * Time.deltaTime, 0);

        // Move the hexapod forward/backward
        Vector3 moveDirection = transform.forward * currentSpeed * Time.deltaTime;
        transform.position += moveDirection;
    }

    /// <summary>
    /// Updates all leg positions based on the current gait pattern
    /// </summary>
    void UpdateLegs()
    {
        // Update gait cycle timer
        cycleTimer += Time.deltaTime;
        if (cycleTimer > gaitCycleTime)
        {
            cycleTimer -= gaitCycleTime;
        }

        // Calculate movement since last frame
        Vector3 positionDelta = transform.position - lastPosition;
        Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(lastRotation);

        // Calculate actual speed for adaptive stride length
        float currentMoveMagnitude = positionDelta.magnitude / Time.deltaTime;

        // Get the number of groups based on the current gait
        int numGroups = legGroups.Length;

        // Update each leg group
        for (int i = 0; i < numGroups; i++)
        {
            // Calculate which phase this group should be in
            float phaseOffset = (float)i / numGroups;
            float groupPhase = (cycleTimer / gaitCycleTime + phaseOffset) % 1.0f;

            // Determine if this group is in stance or swing phase
            bool isStance = groupPhase >= 0.5f;

            // Update the leg group
            UpdateLegGroup(legGroups[i], isStance, positionDelta, rotationDelta, groupPhase, currentMoveMagnitude);
        }

        // Store current position and rotation for next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    /// <summary>
    /// Updates a group of legs based on their current phase in the gait cycle
    /// </summary>
    void UpdateLegGroup(HexapodLeg[] group, bool isStance, Vector3 positionDelta,
                      Quaternion rotationDelta, float groupPhase, float currentSpeed)
    {
        foreach (HexapodLeg leg in group)
        {
            // Get the default foot position in local space
            Vector3 defaultPos = leg.GetDefaultFootPosition();

            if (isMoving)
            {
                if (isStance)
                {
                    // *** STANCE PHASE - foot stays on ground (move opposite to body) ***

                    // Convert default position to world space
                    Vector3 worldDefaultPos = transform.TransformPoint(defaultPos);

                    // Apply inverse of body movement to foot to keep it fixed on ground
                    Vector3 stancePos = transform.InverseTransformPoint(worldDefaultPos - positionDelta);

                    // Check if we're primarily turning rather than moving straight
                    bool isTurning = Mathf.Abs(currentTurnRate) > 1.0f && currentSpeed < 0.1f;

                    if (isTurning)
                    {
                        // Special handling for pure rotation
                        // Get the leg's position relative to body center in horizontal plane
                        Vector3 legOffset = new Vector3(defaultPos.x, 0, defaultPos.z);
                        float legDistance = legOffset.magnitude;

                        // For left side legs vs right side legs, rotation effects are opposite
                        float sideMultiplier = (defaultPos.x < 0) ? -1f : 1f;

                        // Calculate the angular displacement based on turn rate
                        float angularDisplacement = currentTurnRate * Time.deltaTime;

                        // Calculate how much the leg would move due to body rotation
                        float rotationalDisplacement = Mathf.Deg2Rad * angularDisplacement * legDistance;

                        // Apply adjustment to compensate for rotation (perpendicular to leg offset)
                        Vector3 rotationAdjustment = Vector3.Cross(Vector3.up, legOffset.normalized) *
                                                   rotationalDisplacement * sideMultiplier;

                        stancePos += rotationAdjustment;
                    }

                    // Set the foot position
                    leg.SetFootPosition(stancePos);
                }
                else
                {
                    // *** SWING PHASE - foot moves through air in an arc ***

                    // Normalize to 0-1 range for just this phase
                    float swingPhase = (groupPhase * 2.0f) % 1.0f;

                    // Use easing function for smoother, more natural movement
                    float easedPhase = EaseInOutQuad(swingPhase);

                    // Calculate stride length based on current movement
                    float strideLength = Mathf.Abs(currentSpeed) * gaitCycleTime * strideFactor;

                    // Calculate stride direction and adjustment
                    Vector3 strideVector;

                    if (Mathf.Abs(currentSpeed) > 0.1f)
                    {
                        // Moving forward/backward - stride along movement direction
                        strideVector = transform.forward * Mathf.Sign(currentSpeed) * strideLength;
                    }
                    else if (Mathf.Abs(currentTurnRate) > 0.5f)
                    {
                        // Just turning - stride perpendicular to leg position vector
                        Vector3 legPos = new Vector3(defaultPos.x, 0, defaultPos.z);

                        // For left vs. right legs, turning direction is opposite
                        float sideMultiplier = (defaultPos.x < 0) ? -1f : 1f;

                        // Direction is perpendicular to the leg's position from center
                        Vector3 strideDir = Vector3.Cross(Vector3.up, legPos.normalized) *
                                           Mathf.Sign(currentTurnRate) * sideMultiplier;

                        // Calculate turn-based stride length based on distance from center
                        float turnStrideLength = legPos.magnitude * Mathf.Abs(currentTurnRate) *
                                                Mathf.Deg2Rad * gaitCycleTime * strideFactor;

                        strideVector = strideDir * turnStrideLength;
                    }
                    else
                    {
                        // Not much movement, use a small default stride
                        strideVector = transform.forward * 0.05f;
                    }

                    // Calculate start and end positions for the swing
                    // Start position is behind the default position by the stride vector
                    Vector3 startPos = defaultPos - transform.InverseTransformDirection(strideVector);
                    Vector3 endPos = defaultPos;

                    // Interpolate between start and end using eased phase
                    Vector3 swingPos = Vector3.Lerp(startPos, endPos, easedPhase);

                    // Add height using a sine curve for natural arc
                    swingPos.y += Mathf.Sin(swingPhase * Mathf.PI) * stepHeight;

                    // Set the foot position
                    leg.SetFootPosition(swingPos);
                }
            }
            else
            {
                // Not moving, gradually return legs to default positions
                Vector3 currentPos = transform.InverseTransformPoint(leg.footTarget.position);
                Vector3 targetPos = defaultPos;

                // Smoothly move towards default position
                Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * 2.0f);
                leg.SetFootPosition(newPos);
            }
        }
    }

    /// <summary>
    /// Animates the body for more natural movement
    /// </summary>
    private void AnimateBody()
    {
        if (bodyTransform == null || bodyTransform == transform)
            return; // Skip body animation if no separate body is found

        if (!isMoving)
        {
            // Return body to default position and rotation when not moving
            bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, defaultBodyPosition, Time.deltaTime * 3.0f);
            bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, defaultBodyRotation, Time.deltaTime * 3.0f);
            return;
        }

        // Calculate body oscillation - a slight up and down movement synchronized with the gait
        float bodyOscillation = 0;
        if (bodyOscillationHeight > 0)
        {
            bodyOscillation = Mathf.Sin(cycleTimer / gaitCycleTime * Mathf.PI * 2) * bodyOscillationHeight;
        }

        // Apply vertical oscillation
        Vector3 newBodyPos = defaultBodyPosition + new Vector3(0, bodyOscillation, 0);
        bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, newBodyPos, Time.deltaTime * 8.0f);

        // Calculate body tilt - lean into turns and movement
        float forwardTilt = -currentSpeed * 0.5f * bodyTiltFactor;
        float sideTilt = currentTurnRate * 0.05f * bodyTiltFactor;

        Quaternion targetTilt = Quaternion.Euler(forwardTilt, 0, sideTilt) * defaultBodyRotation;
        bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetTilt, Time.deltaTime * 2.0f);
    }

    /// <summary>
    /// Adjust all legs to match terrain height
    /// </summary>
    public void AdaptToTerrain()
    {
        foreach (HexapodLeg leg in legs)
        {
            leg.AdaptToGround();
        }
    }

    /// <summary>
    /// Easing function for smoother movement
    /// </summary>
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    /// <summary>
    /// Display debug info on screen
    /// </summary>
    void OnGUI()
    {
        if (!showDebugInfo)
            return;

        GUI.Box(new Rect(10, 10, 200, 120), "Hexapod Debug");
        GUI.Label(new Rect(20, 30, 180, 20), "Speed: " + currentSpeed.ToString("F2"));
        GUI.Label(new Rect(20, 50, 180, 20), "Turn Rate: " + currentTurnRate.ToString("F2"));
        GUI.Label(new Rect(20, 70, 180, 20), "Gait: " + currentGait.ToString());
        GUI.Label(new Rect(20, 90, 180, 20), "Cycle: " + (cycleTimer / gaitCycleTime).ToString("F2"));
    }

    /// <summary>
    /// Draw gizmos for visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebugInfo)
            return;

        // Draw movement direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + movementDirection * 0.5f);

        // Draw forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
    }
}