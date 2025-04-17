using UnityEngine;

/// <summary>
/// Controls a hexapod's movement and gait patterns
/// </summary>
public class HexapodController : MonoBehaviour
{
    [Header("Legs")]
    public HexapodLeg[] legs;

    [Header("Movement Parameters")]
    public float moveSpeed = 1.5f;
    public float turnSpeed = 60.0f;
    public float stepHeight = 0.25f;

    [Header("Gait Settings")]
    public float gaitCycleTime = 0.8f;
    public enum GaitType { Tripod, Wave, Ripple }
    public GaitType currentGait = GaitType.Tripod;

    // Leg groups for different gaits
    private HexapodLeg[][] legGroups;

    private float cycleTimer = 0f;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool isMoving = false;

    void Start()
    {
        if (legs == null || legs.Length != 6)
        {
            Debug.LogError("Hexapod requires exactly 6 legs");
            return;
        }

        // Set up the leg groups based on the current gait
        SetupLegGroups();

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // Get input for movement
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        // Move the hexapod body
        isMoving = Mathf.Abs(forward) > 0.1f || Mathf.Abs(turn) > 0.1f;
        Move(forward, turn);

        // Update the leg positions based on movement
        UpdateLegs();
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
                legGroups = new HexapodLeg[2][];
                legGroups[0] = new HexapodLeg[] { legs[0], legs[3], legs[4] }; // FR, ML, RR
                legGroups[1] = new HexapodLeg[] { legs[1], legs[2], legs[5] }; // FL, MR, RL
                break;

            case GaitType.Wave:
                // Six groups of one leg each (wave gait)
                legGroups = new HexapodLeg[6][];
                for (int i = 0; i < 6; i++)
                {
                    legGroups[i] = new HexapodLeg[] { legs[i] };
                }
                break;

            case GaitType.Ripple:
                // Three groups of two legs each (ripple gait)
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
    void Move(float forward, float turn)
    {
        // Rotate the hexapod
        transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);

        // Move the hexapod forward/backward
        Vector3 moveDirection = transform.forward * forward * moveSpeed * Time.deltaTime;
        transform.position += moveDirection;
    }

    /// <summary>
    /// Updates all leg positions based on the current gait pattern
    /// </summary>
    void UpdateLegs()
    {
        // Update gait cycle timer
        if (isMoving)
        {
            cycleTimer += Time.deltaTime;
            if (cycleTimer > gaitCycleTime)
            {
                cycleTimer -= gaitCycleTime;
            }
        }

        // Calculate movement since last frame
        Vector3 positionDelta = transform.position - lastPosition;
        Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(lastRotation);

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
            UpdateLegGroup(legGroups[i], isStance, positionDelta, rotationDelta, groupPhase);
        }

        // Store current position and rotation for next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    /// <summary>
    /// Updates a group of legs based on their current phase in the gait cycle
    /// </summary>
    void UpdateLegGroup(HexapodLeg[] group, bool isStance, Vector3 positionDelta, Quaternion rotationDelta, float groupPhase)
    {
        foreach (HexapodLeg leg in group)
        {
            Vector3 defaultPos = leg.GetDefaultFootPosition();

            if (isMoving)
            {
                if (isStance)
                {
                    // Stance phase - foot stays on ground (move opposite to body)
                    Vector3 worldDefaultPos = transform.TransformPoint(defaultPos);

                    // Apply inverse of body movement to foot
                    Vector3 stancePos = transform.InverseTransformPoint(worldDefaultPos - positionDelta);
                    leg.SetFootPosition(stancePos);
                }
                else
                {
                    // Swing phase - foot moves forward along an arc
                    float swingPhase = (groupPhase * 2.0f) % 1.0f; // Normalize to 0-1 range

                    // Calculate forward movement direction based on overall movement
                    Vector3 movementDir = positionDelta.normalized;
                    if (movementDir.magnitude < 0.001f)
                    {
                        movementDir = transform.forward; // Default to forward if not moving
                    }

                    // Scale the stride length based on movement speed
                    float strideLength = moveSpeed * gaitCycleTime * 0.5f;
                    Vector3 forwardOffset = movementDir * strideLength;

                    // Calculate swing trajectory
                    Vector3 startPos = transform.InverseTransformPoint(transform.TransformPoint(defaultPos) - forwardOffset);
                    Vector3 endPos = defaultPos;

                    // Interpolate between start and end positions
                    Vector3 swingPos = Vector3.Lerp(startPos, endPos, swingPhase);

                    // Add height using a sine curve for natural arc
                    swingPos.y += Mathf.Sin(swingPhase * Mathf.PI) * stepHeight;

                    leg.SetFootPosition(swingPos);
                }
            }
            else
            {
                // Not moving, just keep feet at default positions
                leg.SetFootPosition(defaultPos);
            }

            // Update IK for this leg
            leg.SolveIK();
        }
    }

    /// <summary>
    /// Adjust the positions of the legs to match terrain
    /// </summary>
    public void AdaptToTerrain()
    {
        foreach (HexapodLeg leg in legs)
        {
            leg.AdaptToGround();
        }
    }
}