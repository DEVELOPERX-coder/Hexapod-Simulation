using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodController : MonoBehaviour
{
    // Global variables for position and rotation as requested
    public static Vector3 GlobalPosition;
    public static Quaternion GlobalRotation;

    // Hexapod configuration
    public float bodyRadius = 1.0f;
    public float bodyHeight = 0.2f;
    public float legLength = 1.5f;
    public float legWidth = 0.1f;

    // Movement settings
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 60.0f;

    // Leg animation settings
    public float legAnimationSpeed = 3.0f;
    public float legLiftHeight = 0.3f;
    public float stepSize = 0.5f;

    // Components
    private GameObject baseBody;
    private GameObject[] legs = new GameObject[6];
    private GameObject[] upperLegs = new GameObject[6];
    private GameObject[] lowerLegs = new GameObject[6];

    // Leg animation state
    private float animationTime = 0f;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool isMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize global position and rotation
        GlobalPosition = transform.position;
        GlobalRotation = transform.rotation;
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        // Create hexapod
        CreateHexapod();
    }

    // Update is called once per frame
    void Update()
    {
        // Handle input
        HandleInput();

        // Update global variables
        GlobalPosition = transform.position;
        GlobalRotation = transform.rotation;

        // Check if the hexapod is moving
        isMoving = (Vector3.Distance(transform.position, lastPosition) > 0.001f ||
                   Quaternion.Angle(transform.rotation, lastRotation) > 0.1f);

        // Update leg animations
        if (isMoving)
        {
            animationTime += Time.deltaTime * legAnimationSpeed;
            AnimateLegs();
        }

        // Update all child transforms based on global position and rotation
        UpdateChildTransforms();

        // Store current position and rotation for next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    // Create the hexapod (body and legs)
    void CreateHexapod()
    {
        // Create body
        baseBody = new GameObject("HexapodBody");
        baseBody.transform.parent = transform;
        baseBody.transform.localPosition = Vector3.zero;

        // Create hexagonal base using cylinder
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.parent = baseBody.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(bodyRadius * 2, bodyHeight, bodyRadius * 2);
        body.GetComponent<Renderer>().material.color = Color.gray;

        // Add direction indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.parent = baseBody.transform;
        indicator.transform.localPosition = new Vector3(0, 0, bodyRadius * 0.8f);
        indicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        indicator.GetComponent<Renderer>().material.color = Color.red;

        // Create legs
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 legPosition = new Vector3(
                Mathf.Cos(angle) * bodyRadius,
                0,
                Mathf.Sin(angle) * bodyRadius
            );

            legs[i] = new GameObject("Leg" + i);
            legs[i].transform.parent = baseBody.transform;
            legs[i].transform.localPosition = legPosition;

            // Create leg segments (upper and lower)
            upperLegs[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperLegs[i].name = "UpperLeg" + i;
            upperLegs[i].transform.parent = legs[i].transform;
            upperLegs[i].transform.localPosition = new Vector3(0, -legLength * 0.25f, 0);
            upperLegs[i].transform.localScale = new Vector3(legWidth, legLength * 0.5f, legWidth);
            upperLegs[i].GetComponent<Renderer>().material.color = Color.blue;

            lowerLegs[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowerLegs[i].name = "LowerLeg" + i;
            lowerLegs[i].transform.parent = upperLegs[i].transform;
            lowerLegs[i].transform.localPosition = new Vector3(0, -legLength * 0.5f, 0);
            lowerLegs[i].transform.localScale = new Vector3(legWidth * 0.8f, legLength * 0.5f, legWidth * 0.8f);
            lowerLegs[i].GetComponent<Renderer>().material.color = Color.green;

            // Point legs outward and downward
            legs[i].transform.LookAt(transform.position + new Vector3(
                Mathf.Cos(angle) * 10,
                -5,  // Point slightly downward
                Mathf.Sin(angle) * 10
            ));
        }
    }

    // Handle user input for movement
    void HandleInput()
    {
        // Movement (WASD or arrow keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate movement in local space
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // Rotation (Q and E keys)
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        }
    }

    // Animate the legs in a tripod gait
    void AnimateLegs()
    {
        // Tripod gait - legs 0, 2, 4 move together, and legs 1, 3, 5 move together
        for (int i = 0; i < 6; i++)
        {
            // Determine if this leg is in group 1 (even indices) or group 2 (odd indices)
            bool isGroup1 = (i % 2 == 0);

            // Calculate phase offset (0 to 1)
            float phase = isGroup1 ? (animationTime % 1.0f) : ((animationTime + 0.5f) % 1.0f);

            // Create leg movement cycle
            AnimateLeg(i, phase);
        }
    }

    // Animate a single leg based on phase (0 to 1)
    void AnimateLeg(int legIndex, float phase)
    {
        // During the first half of the phase, the leg is lifting and moving forward
        // During the second half, the leg is on the ground and pushing backward

        if (phase < 0.5f)
        {
            // Lifting and moving forward (normalized 0 to 1 for this motion)
            float liftPhase = phase * 2f;

            // Sin curve for smooth up and down motion
            float liftHeight = Mathf.Sin(liftPhase * Mathf.PI) * legLiftHeight;

            // Forward-back motion
            float swingAngle = Mathf.Lerp(30f, -30f, liftPhase) * Mathf.Deg2Rad;

            // Apply rotations to upper and lower leg joints
            upperLegs[legIndex].transform.localRotation = Quaternion.Euler(
                swingAngle * Mathf.Rad2Deg,
                0,
                0
            );

            lowerLegs[legIndex].transform.localRotation = Quaternion.Euler(
                -swingAngle * Mathf.Rad2Deg * 2,
                0,
                0
            );

            // Apply lift
            legs[legIndex].transform.localPosition = new Vector3(
                legs[legIndex].transform.localPosition.x,
                liftHeight,
                legs[legIndex].transform.localPosition.z
            );
        }
        else
        {
            // Grounded and pushing (normalized 0 to 1 for this motion)
            float groundPhase = (phase - 0.5f) * 2f;

            // Reverse motion on the ground (pushing backward)
            float swingAngle = Mathf.Lerp(-30f, 30f, groundPhase) * Mathf.Deg2Rad;

            // Apply rotations to upper and lower leg joints
            upperLegs[legIndex].transform.localRotation = Quaternion.Euler(
                swingAngle * Mathf.Rad2Deg,
                0,
                0
            );

            lowerLegs[legIndex].transform.localRotation = Quaternion.Euler(
                -swingAngle * Mathf.Rad2Deg * 1.5f,
                0,
                0
            );

            // Ensure leg is on the ground
            legs[legIndex].transform.localPosition = new Vector3(
                legs[legIndex].transform.localPosition.x,
                0,
                legs[legIndex].transform.localPosition.z
            );
        }
    }

    // Update child transforms based on global variables
    void UpdateChildTransforms()
    {
        // This ensures all movement is based on the global position and rotation
        // to prevent potential glitches as requested
        transform.position = GlobalPosition;
        transform.rotation = GlobalRotation;
    }
}