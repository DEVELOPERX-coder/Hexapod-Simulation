using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodSimulation : MonoBehaviour
{
    // Global variables for position and rotation
    public static Vector3 GlobalPosition;
    public static Quaternion GlobalRotation;

    // Body configuration - more realistic proportions
    public float bodyLength = 0.6f;     // Length (front to back)
    public float bodyWidth = 0.4f;      // Width (side to side)
    public float bodyHeight = 0.1f;     // Height (top to bottom)

    // Leg configuration - biologically accurate proportions
    public float coxaLength = 0.15f;    // First leg segment (hip joint) - shortest
    public float femurLength = 0.3f;    // Second leg segment - middle length
    public float tibiaLength = 0.4f;    // Third leg segment (to foot) - longest
    public float legWidth = 0.04f;      // Thickness of leg segments

    // Movement settings
    public float moveSpeed = 0.8f;      // Slower for realistic movement
    public float rotateSpeed = 40.0f;   // Slower rotation
    public float stepHeight = 0.1f;     // How high legs lift during step - more conservative
    public float stepDistance = 0.2f;   // How far legs move in one step - smaller steps
    public float legSpeed = 1.2f;       // Speed of leg movement
    public float bodyHeight_standing = 0.3f;  // Lower standing height for stability

    // Leg positioning
    private Vector3[] legPositions = new Vector3[6]; // Position offsets for legs on body

    // Components
    private GameObject hexBody;
    private GameObject[] legRoots = new GameObject[6];
    private GameObject[] coxaSegments = new GameObject[6];
    private GameObject[] femurSegments = new GameObject[6];
    private GameObject[] tibiaSegments = new GameObject[6];

    // Leg state tracking
    private Vector3[] defaultLegPositions = new Vector3[6];
    private Vector3[] targetLegPositions = new Vector3[6];
    private Vector3[] currentLegPositions = new Vector3[6];
    private float[] legPhases = new float[6]; // 0 to 1 for leg movement cycle
    private bool[] legGrounded = new bool[6]; // Is the leg on the ground?

    // Movement state
    private Vector3 movementDirection = Vector3.zero;
    private float rotationDirection = 0f;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize global position and rotation
        GlobalPosition = transform.position;
        GlobalRotation = transform.rotation;
        lastPosition = GlobalPosition;
        lastRotation = GlobalRotation;

        // Initialize realistic leg positions (insect-like arrangement)
        // Front legs slightly forward and outward
        legPositions[0] = new Vector3(bodyWidth * 0.4f, 0, bodyLength * 0.4f);  // Front-right
        legPositions[5] = new Vector3(-bodyWidth * 0.4f, 0, bodyLength * 0.4f); // Front-left

        // Middle legs straight out to sides
        legPositions[1] = new Vector3(bodyWidth * 0.5f, 0, 0);                 // Middle-right
        legPositions[4] = new Vector3(-bodyWidth * 0.5f, 0, 0);                // Middle-left

        // Rear legs slightly backward and outward
        legPositions[2] = new Vector3(bodyWidth * 0.4f, 0, -bodyLength * 0.4f); // Rear-right
        legPositions[3] = new Vector3(-bodyWidth * 0.4f, 0, -bodyLength * 0.4f);// Rear-left

        // Create the hexapod
        CreateHexapod();

        // Initialize leg phases for tripod gait (alternating legs)
        // Legs 0, 2, 4 are one tripod, legs 1, 3, 5 are the other
        for (int i = 0; i < 6; i++)
        {
            legPhases[i] = (i % 2 == 0) ? 0f : 0.5f;
            legGrounded[i] = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Handle input for movement
        HandleInput();

        // Update global variables
        GlobalPosition = transform.position;
        GlobalRotation = transform.rotation;

        // Calculate movement direction for leg positioning
        Vector3 moveDirection = GlobalPosition - lastPosition;
        float rotationAmount = Quaternion.Angle(lastRotation, GlobalRotation);
        rotationDirection = Mathf.Sign(Vector3.Dot(transform.up,
                                  Vector3.Cross(lastRotation * Vector3.forward,
                                              GlobalRotation * Vector3.forward)));

        // Update leg movement
        UpdateLegMovement(moveDirection.magnitude, rotationAmount * rotationDirection);

        // Save current position/rotation for next frame
        lastPosition = GlobalPosition;
        lastRotation = GlobalRotation;
    }

    // Create the hexapod body and legs
    void CreateHexapod()
    {
        // Create body container
        hexBody = new GameObject("HexapodBody");
        hexBody.transform.parent = transform;
        hexBody.transform.localPosition = new Vector3(0, bodyHeight_standing, 0);
        hexBody.transform.localRotation = Quaternion.identity;

        // Create main body - more realistic elongated shape
        GameObject bodyMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyMesh.transform.parent = hexBody.transform;
        bodyMesh.transform.localPosition = Vector3.zero;
        bodyMesh.transform.localScale = new Vector3(bodyWidth, bodyHeight, bodyLength);
        bodyMesh.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.4f);

        // Create thorax segments for more realistic insect appearance
        // Front thorax segment (prothorax)
        GameObject frontSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontSegment.transform.parent = hexBody.transform;
        frontSegment.transform.localPosition = new Vector3(0, 0, bodyLength * 0.4f);
        frontSegment.transform.localScale = new Vector3(bodyWidth * 0.8f, bodyHeight * 1.1f, bodyLength * 0.35f);
        frontSegment.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.45f);

        // Rear thorax segment (metathorax)
        GameObject rearSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearSegment.transform.parent = hexBody.transform;
        rearSegment.transform.localPosition = new Vector3(0, 0, -bodyLength * 0.4f);
        rearSegment.transform.localScale = new Vector3(bodyWidth * 0.85f, bodyHeight * 1.05f, bodyLength * 0.35f);
        rearSegment.GetComponent<Renderer>().material.color = new Color(0.32f, 0.32f, 0.42f);

        // Add direction indicator at front
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.parent = hexBody.transform;
        indicator.transform.localPosition = new Vector3(0, 0, bodyLength * 0.55f);
        indicator.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
        indicator.GetComponent<Renderer>().material.color = Color.red;

        // Create legs in biologically accurate positions
        for (int i = 0; i < 6; i++)
        {
            // Create leg root (attachment point)
            legRoots[i] = new GameObject("LegRoot_" + i);
            legRoots[i].transform.parent = hexBody.transform;
            legRoots[i].transform.localPosition = legPositions[i];

            // Set leg orientation based on position
            float legAngle = 0;
            if (i == 0 || i == 5)
            {      // Front legs
                legAngle = (i == 0) ? 60 : 120;
            }
            else if (i == 1 || i == 4)
            { // Middle legs
                legAngle = (i == 1) ? 90 : 90;
            }
            else
            {                       // Rear legs
                legAngle = (i == 2) ? 120 : 60;
            }

            legRoots[i].transform.localRotation = Quaternion.Euler(0, legAngle, 0);

            // Calculate the default foot position in world space (rest pose)
            // Adjust leg stance to be wider and more stable
            Vector3 footPos = legRoots[i].transform.position +
                             (legRoots[i].transform.right * (coxaLength + femurLength * 0.7f)) +
                             (-Vector3.up * bodyHeight_standing);

            // Front legs reach more forward
            if (i == 0 || i == 5)
            {
                footPos += legRoots[i].transform.forward * 0.15f;
            }
            // Rear legs reach more backward
            else if (i == 2 || i == 3)
            {
                footPos -= legRoots[i].transform.forward * 0.15f;
            }

            defaultLegPositions[i] = footPos;
            currentLegPositions[i] = footPos;
            targetLegPositions[i] = footPos;

            // Create coxa segment (horizontal segment that rotates around the body)
            coxaSegments[i] = CreateLegSegment("Coxa_" + i, legRoots[i].transform,
                              new Vector3(coxaLength / 2, 0, 0),
                              new Vector3(coxaLength, legWidth, legWidth),
                              new Color(0.2f, 0.6f, 0.8f));

            // Create femur segment (middle segment)
            femurSegments[i] = CreateLegSegment("Femur_" + i, coxaSegments[i].transform,
                               new Vector3(coxaLength / 2, 0, 0),
                               new Vector3(femurLength, legWidth * 0.9f, legWidth * 0.9f),
                               new Color(0.2f, 0.5f, 0.7f));
            femurSegments[i].transform.localRotation = Quaternion.Euler(0, 0, -30f);

            // Create tibia segment (lowest segment)
            tibiaSegments[i] = CreateLegSegment("Tibia_" + i, femurSegments[i].transform,
                               new Vector3(femurLength / 2, 0, 0),
                               new Vector3(tibiaLength, legWidth * 0.8f, legWidth * 0.8f),
                               new Color(0.2f, 0.4f, 0.6f));
            tibiaSegments[i].transform.localRotation = Quaternion.Euler(0, 0, -50f);

            // Create foot
            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foot.name = "Foot_" + i;
            foot.transform.parent = tibiaSegments[i].transform;
            foot.transform.localPosition = new Vector3(tibiaLength / 2, 0, 0);
            foot.transform.localScale = new Vector3(legWidth * 0.9f, legWidth * 0.9f, legWidth * 0.9f);
            foot.GetComponent<Renderer>().material.color = new Color(0.1f, 0.2f, 0.4f);
        }
    }

    // Helper function to create a leg segment
    GameObject CreateLegSegment(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.parent = parent;
        segment.transform.localPosition = position;
        segment.transform.localScale = scale;
        segment.GetComponent<Renderer>().material.color = color;
        return segment;
    }

    // Handle user input for movement
    void HandleInput()
    {
        // Movement (WASD or arrow keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        movementDirection = new Vector3(horizontal, 0, vertical).normalized;

        // Calculate movement in local space
        Vector3 movement = transform.TransformDirection(movementDirection) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        // Rotation (Q and E keys)
        float rotation = 0;
        if (Input.GetKey(KeyCode.Q))
        {
            rotation = -1;
            transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotation = 1;
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        }

        // Body height adjustment (R and F keys)
        if (Input.GetKey(KeyCode.R))
        {
            hexBody.transform.localPosition = new Vector3(0,
                Mathf.Min(bodyHeight_standing + 0.2f, hexBody.transform.localPosition.y + Time.deltaTime), 0);
        }
        if (Input.GetKey(KeyCode.F))
        {
            hexBody.transform.localPosition = new Vector3(0,
                Mathf.Max(bodyHeight_standing - 0.15f, hexBody.transform.localPosition.y - Time.deltaTime), 0);
        }
    }

    // Update leg movement based on hexapod's movement
    void UpdateLegMovement(float moveAmount, float rotateAmount)
    {
        bool isMoving = (moveAmount > 0.001f || Mathf.Abs(rotateAmount) > 0.1f);

        // Update each leg
        for (int i = 0; i < 6; i++)
        {
            // Update leg phase
            if (isMoving)
            {
                // Advance leg phase when moving
                legPhases[i] = (legPhases[i] + Time.deltaTime * legSpeed) % 1.0f;
            }

            // Calculate target position for foot
            if (isMoving)
            {
                // When the leg starts its swing phase (lifting off)
                if (legPhases[i] < 0.5f && legPhases[i] > 0.01f && legGrounded[i])
                {
                    // Determine new target position based on movement direction
                    Vector3 moveDir = transform.TransformDirection(movementDirection);
                    Vector3 rotatePoint = transform.position;

                    // Calculate rotation effect on this leg
                    Vector3 relativePos = defaultLegPositions[i] - rotatePoint;
                    float distance = relativePos.magnitude;

                    Vector3 rotationEffect = Vector3.zero;
                    if (Mathf.Abs(rotateAmount) > 0.1f)
                    {
                        // Approximate the rotation effect as a tangential vector
                        Vector3 tangent = Vector3.Cross(Vector3.up, relativePos.normalized);
                        rotationEffect = tangent * rotateAmount * 0.01f * distance;
                    }

                    // Combine movement and rotation effects
                    Vector3 combinedEffect = (moveDir * moveSpeed * 0.5f) + rotationEffect;
                    targetLegPositions[i] = defaultLegPositions[i] + combinedEffect;

                    // Limit how far the leg can reach from its default position
                    Vector3 offset = targetLegPositions[i] - defaultLegPositions[i];
                    if (offset.magnitude > stepDistance)
                    {
                        offset = offset.normalized * stepDistance;
                        targetLegPositions[i] = defaultLegPositions[i] + offset;
                    }

                    legGrounded[i] = false;
                }

                // When leg completes swing and touches down
                if (legPhases[i] > 0.5f && !legGrounded[i])
                {
                    legGrounded[i] = true;
                }
            }
            else
            {
                // When not moving, gradually return to default position
                targetLegPositions[i] = Vector3.Lerp(targetLegPositions[i], defaultLegPositions[i], Time.deltaTime * 2f);
            }

            // Calculate current position based on phase
            if (!legGrounded[i])
            {
                // Swing phase (leg in air)
                float swingPhase = legPhases[i] / 0.5f; // 0 to 1 for the swing part of cycle

                // Parabolic trajectory for swing phase
                Vector3 liftVector = Vector3.up * stepHeight * Mathf.Sin(swingPhase * Mathf.PI);
                Vector3 horizontalMove = Vector3.Lerp(currentLegPositions[i], targetLegPositions[i], swingPhase);

                currentLegPositions[i] = horizontalMove + liftVector;
            }
            else
            {
                // Stance phase (leg on ground) - keeps foot in same world position
                // but we need to update anyway to account for body movement
                currentLegPositions[i] = targetLegPositions[i];
            }

            // Apply IK to position the leg segments to reach the foot position
            ApplyLegIK(i, currentLegPositions[i]);
        }
    }

    // Apply inverse kinematics to position leg segments
    void ApplyLegIK(int legIndex, Vector3 targetFootPosition)
    {
        // Get the positions in local space
        Vector3 rootPos = legRoots[legIndex].transform.position;
        Vector3 footPos = targetFootPosition;

        // Vector from root to target foot position
        Vector3 rootToFoot = footPos - rootPos;

        // Coxa rotation (around Y axis)
        float coxaAngle = Mathf.Atan2(rootToFoot.x, rootToFoot.z) * Mathf.Rad2Deg;
        coxaAngle -= legRoots[legIndex].transform.eulerAngles.y;
        legRoots[legIndex].transform.localRotation = Quaternion.Euler(0, coxaAngle, 0);

        // Update coxa end position
        Vector3 coxaEndPos = rootPos + coxaSegments[legIndex].transform.right * coxaLength;

        // Calculate the distance for femur and tibia to cover
        Vector3 coxaToFoot = footPos - coxaEndPos;
        float distToFoot = coxaToFoot.magnitude;

        // Constrain the distance to what is reachable by the leg
        float maxReach = femurLength + tibiaLength;
        distToFoot = Mathf.Min(distToFoot, maxReach * 0.99f);

        // Calculate femur and tibia angles using the law of cosines
        float femurAngle, tibiaAngle;

        // Use law of cosines to calculate angles: c^2 = a^2 + b^2 - 2ab*cos(C)
        float a = femurLength;
        float b = tibiaLength;
        float c = distToFoot;

        // Femur angle (between coxa and femur)
        float cosC = (a * a + c * c - b * b) / (2 * a * c);
        cosC = Mathf.Clamp(cosC, -1f, 1f); // Avoid domain errors with acos
        float angleC = Mathf.Acos(cosC) * Mathf.Rad2Deg;

        // The direction vector in local space
        Vector3 localDir = coxaSegments[legIndex].transform.InverseTransformDirection(coxaToFoot.normalized);
        float elevationAngle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;

        femurAngle = elevationAngle + angleC;

        // Tibia angle (between femur and tibia)
        float cosB = (a * a + b * b - c * c) / (2 * a * b);
        cosB = Mathf.Clamp(cosB, -1f, 1f);
        tibiaAngle = 180f - (Mathf.Acos(cosB) * Mathf.Rad2Deg);

        // Apply rotations
        coxaSegments[legIndex].transform.localRotation = Quaternion.Euler(0, 0, 0);
        femurSegments[legIndex].transform.localRotation = Quaternion.Euler(0, 0, -femurAngle);
        tibiaSegments[legIndex].transform.localRotation = Quaternion.Euler(0, 0, -tibiaAngle);
    }
}