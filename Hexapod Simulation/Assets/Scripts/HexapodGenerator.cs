using UnityEngine;

/// <summary>
/// Automatically generates a complete hexapod structure with body and legs
/// </summary>
public class HexapodGenerator : MonoBehaviour
{
    [Header("Body Settings")]
    public Vector3 bodySize = new Vector3(1.2f, 0.25f, 1.8f);
    public Material bodyMaterial;

    [Header("Leg Settings")]
    public float hipLength = 0.25f;
    public float femurLength = 0.45f;
    public float tibiaLength = 0.65f;
    public float hipWidth = 0.12f;
    public float femurWidth = 0.1f;
    public float tibiaWidth = 0.08f;
    public Material legMaterial;

    [Header("Leg Positions")]
    public float legSpreadX = 0.65f;  // Distance from center along X axis
    public float frontLegPosZ = 0.7f; // Front leg position on Z axis
    public float middleLegPosZ = 0f;  // Middle leg position on Z axis
    public float rearLegPosZ = -0.7f; // Rear leg position on Z axis
    public float legHeightOffset = -0.05f; // Vertical offset for leg positions

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool attachComponents = true;

    private GameObject hexapodBody;
    private GameObject[] legs = new GameObject[6];

    void Start()
    {
        if (generateOnStart)
        {
            GenerateHexapod();
        }
    }

    /// <summary>
    /// Generate the complete hexapod structure
    /// </summary>
    public void GenerateHexapod()
    {
        // Create the main hexapod object if it doesn't already exist
        GameObject hexapod = gameObject;

        // Create the body
        hexapodBody = CreateBody(hexapod);

        // Create the legs
        CreateLegs(hexapodBody);

        // Attach controller components if requested
        if (attachComponents)
        {
            AttachComponents(hexapod);
        }

        Debug.Log("Hexapod generated successfully!");
    }

    /// <summary>
    /// Create the hexapod body cube
    /// </summary>
    private GameObject CreateBody(GameObject parent)
    {
        // Create the body GameObject
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(parent.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = bodySize;

        // Assign material if provided
        if (bodyMaterial != null)
        {
            Renderer renderer = body.GetComponent<Renderer>();
            renderer.material = bodyMaterial;
        }

        return body;
    }

    /// <summary>
    /// Create all six legs of the hexapod
    /// </summary>
    private void CreateLegs(GameObject body)
    {
        // Create leg pairs (front, middle, rear)
        // Front legs
        legs[0] = CreateLeg(body, "Leg_FR", legSpreadX, legHeightOffset, frontLegPosZ);  // Front Right
        legs[1] = CreateLeg(body, "Leg_FL", -legSpreadX, legHeightOffset, frontLegPosZ); // Front Left

        // Middle legs
        legs[2] = CreateLeg(body, "Leg_MR", legSpreadX, legHeightOffset, middleLegPosZ);  // Middle Right
        legs[3] = CreateLeg(body, "Leg_ML", -legSpreadX, legHeightOffset, middleLegPosZ); // Middle Left

        // Rear legs
        legs[4] = CreateLeg(body, "Leg_RR", legSpreadX, legHeightOffset, rearLegPosZ);  // Rear Right
        legs[5] = CreateLeg(body, "Leg_RL", -legSpreadX, legHeightOffset, rearLegPosZ); // Rear Left
    }

    /// <summary>
    /// Create a single leg with hip, femur and tibia segments
    /// </summary>
    private GameObject CreateLeg(GameObject parent, string name, float xPos, float yPos, float zPos)
    {
        // Create leg parent object
        GameObject leg = new GameObject(name);
        leg.transform.SetParent(parent.transform);
        leg.transform.localPosition = new Vector3(xPos, yPos, zPos);
        leg.transform.localRotation = Quaternion.identity;

        // Create hip joint (connecting body to femur)
        GameObject hip = CreateLegSegment(
            leg,
            "Hip",
            new Vector3(0, 0, 0),
            new Vector3(0, 90, 0),
            new Vector3(hipWidth, hipWidth, hipLength)
        );

        // Create femur (thigh)
        GameObject femur = CreateLegSegment(
            hip,
            "Femur",
            new Vector3(0, 0, femurLength / 2),
            new Vector3(0, 0, 0),
            new Vector3(femurWidth, femurLength, femurWidth)
        );

        // Create tibia (lower leg)
        GameObject tibia = CreateLegSegment(
            femur,
            "Tibia",
            new Vector3(0, -tibiaLength / 2, 0),
            new Vector3(0, 0, 0),
            new Vector3(tibiaWidth, tibiaLength, tibiaWidth)
        );

        // Create a foot target point (for IK)
        GameObject footTarget = new GameObject(name + "_FootTarget");
        footTarget.transform.SetParent(leg.transform);
        footTarget.transform.localPosition = new Vector3(0, -femurLength - tibiaLength, hipLength);

        return leg;
    }

    /// <summary>
    /// Create a leg segment (cylinder)
    /// </summary>
    private GameObject CreateLegSegment(GameObject parent, string name, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        segment.name = name;
        segment.transform.SetParent(parent.transform);
        segment.transform.localPosition = position;
        segment.transform.localRotation = Quaternion.Euler(rotation);
        segment.transform.localScale = scale;

        // Assign material if provided
        if (legMaterial != null)
        {
            Renderer renderer = segment.GetComponent<Renderer>();
            renderer.material = legMaterial;
        }

        return segment;
    }

    /// <summary>
    /// Attach necessary controller components to make the hexapod functional
    /// </summary>
    private void AttachComponents(GameObject hexapod)
    {
        // Add HexapodController to the main object
        HexapodController controller = hexapod.AddComponent<HexapodController>();

        // Add HexapodLeg script to each leg and configure it
        HexapodLeg[] legComponents = new HexapodLeg[6];

        for (int i = 0; i < 6; i++)
        {
            // Add HexapodLeg component
            HexapodLeg legComponent = legs[i].AddComponent<HexapodLeg>();

            // Assign the segments
            legComponent.hip = legs[i].transform.Find("Hip");
            legComponent.femur = legs[i].transform.Find("Hip/Femur");
            legComponent.tibia = legs[i].transform.Find("Hip/Femur/Tibia");

            // Assign the foot target
            legComponent.footTarget = legs[i].transform.Find(legs[i].name + "_FootTarget");

            // Set the length parameters
            legComponent.femurLength = femurLength;
            legComponent.tibiaLength = tibiaLength;

            // Add to the array
            legComponents[i] = legComponent;
        }

        // Assign legs to the controller
        controller.legs = legComponents;
    }

    /// <summary>
    /// Create hexapod in the Unity Editor
    /// </summary>
    [ContextMenu("Generate Hexapod")]
    public void GenerateInEditor()
    {
        GenerateHexapod();
    }
}