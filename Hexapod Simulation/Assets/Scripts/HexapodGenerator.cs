using UnityEngine;

/// <summary>
/// Generates a properly configured hexapod with correct leg orientations
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
    public float legHeightOffset = 0f; // Vertical offset for leg positions

    [Header("Leg Orientation")]
    public float frontLegAngle = 30f; // Front legs angle forward
    public float middleLegAngle = 0f; // Middle legs straight out
    public float rearLegAngle = -30f; // Rear legs angle backward
    public float hipDownAngle = 15f;  // Hip downward angle
    public float femurDownAngle = 30f; // Femur downward angle

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
    /// Create all six legs of the hexapod with proper orientation
    /// </summary>
    private void CreateLegs(GameObject body)
    {
        // Create leg pairs (front, middle, rear)
        // Front legs
        legs[0] = CreateLeg(body, "Leg_FR", legSpreadX, legHeightOffset, frontLegPosZ, frontLegAngle, true);  // Front Right
        legs[1] = CreateLeg(body, "Leg_FL", -legSpreadX, legHeightOffset, frontLegPosZ, -frontLegAngle, false); // Front Left

        // Middle legs
        legs[2] = CreateLeg(body, "Leg_MR", legSpreadX, legHeightOffset, middleLegPosZ, middleLegAngle, true);  // Middle Right
        legs[3] = CreateLeg(body, "Leg_ML", -legSpreadX, legHeightOffset, middleLegPosZ, -middleLegAngle, false); // Middle Left

        // Rear legs
        legs[4] = CreateLeg(body, "Leg_RR", legSpreadX, legHeightOffset, rearLegPosZ, rearLegAngle, true);  // Rear Right
        legs[5] = CreateLeg(body, "Leg_RL", -legSpreadX, legHeightOffset, rearLegPosZ, -rearLegAngle, false); // Rear Left
    }

    /// <summary>
    /// Create a single leg with correct orientation based on side (left or right)
    /// </summary>
    private GameObject CreateLeg(GameObject parent, string name, float xPos, float yPos, float zPos, float hipAngle, bool isRightSide)
    {
        // Create leg parent object (empty GameObject for the leg root)
        GameObject leg = new GameObject(name);
        leg.transform.SetParent(parent.transform);
        leg.transform.localPosition = new Vector3(xPos, yPos, zPos);
        leg.transform.localRotation = Quaternion.identity;

        // Calculate the appropriate hip rotation
        // For right side: positive angle means rotate toward body center on Y axis
        // For left side: positive angle means rotate away from body center on Y axis
        float adjustedHipAngle = isRightSide ? hipAngle : hipAngle;

        // Create hip joint - this is an empty GameObject to serve as the rotation point
        GameObject hipJoint = new GameObject("HipJoint");
        hipJoint.transform.SetParent(leg.transform);
        hipJoint.transform.localPosition = Vector3.zero;
        hipJoint.transform.localRotation = Quaternion.Euler(0, adjustedHipAngle, 0);

        // Direction for the hip to extend (outward from body)
        Vector3 hipDirection = isRightSide ? Vector3.right : Vector3.left;

        // Create hip visual (cylinder extending outward from body)
        GameObject hip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hip.name = "Hip";
        hip.transform.SetParent(hipJoint.transform);

        // Position halfway along length in the appropriate direction
        hip.transform.localPosition = hipDirection * (hipLength / 2);

        // Rotate to point outward with slight downward angle
        // For right side, need 90 degrees Z rotation, for left side, need -90 degrees Z rotation
        float hipZRotation = isRightSide ? 90 : -90;
        hip.transform.localRotation = Quaternion.Euler(hipDownAngle, 0, hipZRotation);

        hip.transform.localScale = new Vector3(hipWidth, hipLength / 2, hipWidth);

        // Apply material to hip
        if (legMaterial != null)
        {
            hip.GetComponent<Renderer>().material = legMaterial;
        }

        // Calculate hip endpoint position for femur joint attachment
        Vector3 hipEndpoint = hipDirection * hipLength;

        // Create femur joint - at the end of the hip
        GameObject femurJoint = new GameObject("FemurJoint");
        femurJoint.transform.SetParent(hipJoint.transform);
        femurJoint.transform.localPosition = hipEndpoint;
        femurJoint.transform.localRotation = Quaternion.identity;

        // Create femur visual - this should point downward from the hip
        GameObject femur = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        femur.name = "Femur";
        femur.transform.SetParent(femurJoint.transform);

        // Position halfway down
        femur.transform.localPosition = new Vector3(0, -femurLength / 2, 0);

        // Apply initial rotation for the femur pointing downward with correct angle
        femur.transform.localRotation = Quaternion.Euler(femurDownAngle, 0, 0);

        femur.transform.localScale = new Vector3(femurWidth, femurLength / 2, femurWidth);

        // Apply material to femur
        if (legMaterial != null)
        {
            femur.GetComponent<Renderer>().material = legMaterial;
        }

        // Create tibia joint - at the end of the femur
        GameObject tibiaJoint = new GameObject("TibiaJoint");
        tibiaJoint.transform.SetParent(femurJoint.transform);
        tibiaJoint.transform.localPosition = new Vector3(0, -femurLength, 0);
        tibiaJoint.transform.localRotation = Quaternion.identity;

        // Create tibia visual - this should point straight down
        GameObject tibia = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tibia.name = "Tibia";
        tibia.transform.SetParent(tibiaJoint.transform);
        tibia.transform.localPosition = new Vector3(0, -tibiaLength / 2, 0);
        tibia.transform.localRotation = Quaternion.identity; // Default points downward
        tibia.transform.localScale = new Vector3(tibiaWidth, tibiaLength / 2, tibiaWidth);

        // Apply material to tibia
        if (legMaterial != null)
        {
            tibia.GetComponent<Renderer>().material = legMaterial;
        }

        // Create a foot at the end of the tibia
        GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foot.name = "Foot";
        foot.transform.SetParent(tibiaJoint.transform);
        foot.transform.localPosition = new Vector3(0, -tibiaLength, 0);
        foot.transform.localScale = new Vector3(tibiaWidth * 1.2f, tibiaWidth * 0.5f, tibiaWidth * 1.2f);

        // Apply material to foot
        if (legMaterial != null)
        {
            foot.GetComponent<Renderer>().material = legMaterial;
        }

        // Create a foot target (for IK) - this is invisible during play
        GameObject footTarget = new GameObject(name + "_FootTarget");
        footTarget.transform.SetParent(leg.transform);

        // Position the foot target at the foot's position
        // We need to calculate its position in the leg's local space
        Vector3 footWorldPos = foot.transform.position;
        Vector3 footLocalPos = leg.transform.InverseTransformPoint(footWorldPos);
        footTarget.transform.localPosition = footLocalPos;

        return leg;
    }

    /// <summary>
    /// Attach necessary controller components to make the hexapod functional
    /// </summary>
    private void AttachComponents(GameObject hexapod)
    {
        // Add HexapodController to the main object if it doesn't exist
        HexapodController controller = hexapod.GetComponent<HexapodController>();
        if (controller == null)
        {
            controller = hexapod.AddComponent<HexapodController>();
        }

        // Setup leg components
        HexapodLeg[] legComponents = new HexapodLeg[6];

        for (int i = 0; i < 6; i++)
        {
            // Add HexapodLeg component if it doesn't exist
            HexapodLeg legComponent = legs[i].GetComponent<HexapodLeg>();
            if (legComponent == null)
            {
                legComponent = legs[i].AddComponent<HexapodLeg>();
            }

            // Find the joints
            Transform hipJoint = legs[i].transform.Find("HipJoint");
            Transform femurJoint = hipJoint.Find("FemurJoint");
            Transform tibiaJoint = femurJoint.Find("TibiaJoint");

            // Assign the segments - we point to the joint transforms, not the visual components
            legComponent.hip = hipJoint;
            legComponent.femur = femurJoint;
            legComponent.tibia = tibiaJoint;

            // Assign the foot target
            legComponent.footTarget = legs[i].transform.Find(legs[i].name + "_FootTarget");

            // Set the length parameters
            legComponent.femurLength = femurLength;
            legComponent.tibiaLength = tibiaLength;

            // Adjust the leg IK parameters based on side
            bool isRightSide = (i % 2 == 0); // Even indices are right side legs
            if (!isRightSide)
            {
                // Mirror the hip rotation limit for left legs
                legComponent.hipRotationLimitMin = -legComponent.hipRotationLimitMax;
                legComponent.hipRotationLimitMax = -legComponent.hipRotationLimitMin;
            }

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