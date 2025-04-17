using UnityEngine;

/// <summary>
/// A quick fix utility script to correct leg orientation
/// Attach this to your hexapod and run its function to fix leg orientation
/// </summary>
public class QuickFixLegOrientation : MonoBehaviour
{
    [Header("Leg References")]
    public HexapodLeg[] legs;

    [Header("Fix Settings")]
    [Tooltip("Rotate femur joints to point downward")]
    public float femurDownRotation = 90f;
    [Tooltip("Rotate tibia joints to point downward")]
    public float tibiaDownRotation = 0f;

    [ContextMenu("Fix Leg Orientation")]
    public void FixLegOrientation()
    {
        if (legs == null || legs.Length == 0)
        {
            // Try to get legs from the HexapodController
            HexapodController controller = GetComponent<HexapodController>();
            if (controller != null && controller.legs != null)
            {
                legs = controller.legs;
            }
        }

        if (legs == null || legs.Length == 0)
        {
            Debug.LogError("No legs found to fix! Assign legs manually or ensure hexapod controller is present.");
            return;
        }

        int legsFixed = 0;

        foreach (HexapodLeg leg in legs)
        {
            if (leg == null) continue;

            // Access the leg transforms
            Transform hipJoint = leg.hip;
            Transform femurJoint = leg.femur;
            Transform tibiaJoint = leg.tibia;

            if (hipJoint == null || femurJoint == null || tibiaJoint == null)
            {
                Debug.LogWarning("Incomplete leg joints found on leg: " + leg.name);
                continue;
            }

            // Fix femur orientation to point downward
            femurJoint.localRotation = Quaternion.Euler(femurDownRotation, 0, 0);

            // Fix tibia orientation to point downward
            tibiaJoint.localRotation = Quaternion.Euler(tibiaDownRotation, 0, 0);

            // Also make sure the visual meshes under these joints are properly aligned
            AlignChildMeshes(femurJoint);
            AlignChildMeshes(tibiaJoint);

            legsFixed++;
        }

        Debug.Log("Fixed orientation for " + legsFixed + " legs");
    }

    /// <summary>
    /// Ensures that any mesh children are properly aligned
    /// </summary>
    private void AlignChildMeshes(Transform joint)
    {
        // Skip if no children
        if (joint.childCount == 0) return;

        // For each child with a renderer (visual mesh)
        for (int i = 0; i < joint.childCount; i++)
        {
            Transform child = joint.GetChild(i);

            if (child.GetComponent<Renderer>() != null)
            {
                // This is a visual mesh - make sure it's aligned properly
                // For femur and tibia, should be oriented along local Y axis
                child.localPosition = new Vector3(0, -child.localScale.y / 2, 0);
                child.localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// Create a fixed hexapod with proper leg orientations
    /// </summary>
    [ContextMenu("Create Fixed Hexapod")]
    public void CreateFixedHexapod()
    {
        GameObject body = CreateBody();
        CreateRightLegs(body);
        CreateLeftLegs(body);
        AttachController();
        Debug.Log("Created fixed hexapod with properly oriented legs");
    }

    private GameObject CreateBody()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.2f, 0.25f, 1.8f);
        return body;
    }

    private void CreateRightLegs(GameObject body)
    {
        float legSpreadX = 0.65f;

        // Front right leg
        CreateLeg(body, "Leg_FR", legSpreadX, 0, 0.7f, 30f, true);

        // Middle right leg
        CreateLeg(body, "Leg_MR", legSpreadX, 0, 0f, 0f, true);

        // Rear right leg
        CreateLeg(body, "Leg_RR", legSpreadX, 0, -0.7f, -30f, true);
    }

    private void CreateLeftLegs(GameObject body)
    {
        float legSpreadX = -0.65f;

        // Front left leg
        CreateLeg(body, "Leg_FL", legSpreadX, 0, 0.7f, -30f, false);

        // Middle left leg
        CreateLeg(body, "Leg_ML", legSpreadX, 0, 0f, 0f, false);

        // Rear left leg
        CreateLeg(body, "Leg_RL", legSpreadX, 0, -0.7f, 30f, false);
    }

    private GameObject CreateLeg(GameObject parent, string name, float xPos, float yPos, float zPos, float hipAngle, bool isRightSide)
    {
        // Constants for leg creation
        float hipLength = 0.25f;
        float femurLength = 0.45f;
        float tibiaLength = 0.65f;
        float hipWidth = 0.12f;
        float femurWidth = 0.1f;
        float tibiaWidth = 0.08f;

        // Create leg root
        GameObject leg = new GameObject(name);
        leg.transform.SetParent(parent.transform);
        leg.transform.localPosition = new Vector3(xPos, yPos, zPos);

        // Create hip joint - rotates on Y axis
        GameObject hipJoint = new GameObject("HipJoint");
        hipJoint.transform.SetParent(leg.transform);
        hipJoint.transform.localPosition = Vector3.zero;
        hipJoint.transform.localRotation = Quaternion.Euler(0, hipAngle, 0);

        // Direction for hip to extend
        Vector3 hipDir = isRightSide ? Vector3.right : Vector3.left;

        // Create hip visual
        GameObject hip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hip.name = "Hip";
        hip.transform.SetParent(hipJoint.transform);
        hip.transform.localPosition = hipDir * (hipLength / 2);

        // Rotates 90 degrees around Z to point outward
        float hipZRot = isRightSide ? 90 : -90;
        hip.transform.localRotation = Quaternion.Euler(0, 0, hipZRot);
        hip.transform.localScale = new Vector3(hipWidth, hipLength / 2, hipWidth);

        // Create femur joint - at end of hip, rotates on X axis
        GameObject femurJoint = new GameObject("FemurJoint");
        femurJoint.transform.SetParent(hipJoint.transform);
        femurJoint.transform.localPosition = hipDir * hipLength;

        // Important: rotate femur joint to point downward
        femurJoint.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Create femur visual - cylinder pointing along parent's Y axis
        GameObject femur = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        femur.name = "Femur";
        femur.transform.SetParent(femurJoint.transform);
        femur.transform.localPosition = new Vector3(0, -femurLength / 2, 0);
        femur.transform.localRotation = Quaternion.identity;
        femur.transform.localScale = new Vector3(femurWidth, femurLength / 2, femurWidth);

        // Create tibia joint - at end of femur, rotates on X axis
        GameObject tibiaJoint = new GameObject("TibiaJoint");
        tibiaJoint.transform.SetParent(femurJoint.transform);
        tibiaJoint.transform.localPosition = new Vector3(0, -femurLength, 0);
        tibiaJoint.transform.localRotation = Quaternion.identity;

        // Create tibia visual - cylinder pointing along parent's Y axis
        GameObject tibia = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tibia.name = "Tibia";
        tibia.transform.SetParent(tibiaJoint.transform);
        tibia.transform.localPosition = new Vector3(0, -tibiaLength / 2, 0);
        tibia.transform.localRotation = Quaternion.identity;
        tibia.transform.localScale = new Vector3(tibiaWidth, tibiaLength / 2, tibiaWidth);

        // Create foot
        GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foot.name = "Foot";
        foot.transform.SetParent(tibiaJoint.transform);
        foot.transform.localPosition = new Vector3(0, -tibiaLength, 0);
        foot.transform.localScale = new Vector3(tibiaWidth * 1.2f, tibiaWidth * 0.5f, tibiaWidth * 1.2f);

        // Create target for IK
        GameObject footTarget = new GameObject(name + "_FootTarget");
        footTarget.transform.SetParent(leg.transform);
        footTarget.transform.position = foot.transform.position;

        return leg;
    }

    private void AttachController()
    {
        // Add hexapod controller
        HexapodController controller = gameObject.GetComponent<HexapodController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<HexapodController>();
        }

        // Find all legs and attach components
        Transform body = transform.Find("Body");
        if (body == null) return;

        GameObject[] legObjects = new GameObject[6];
        legObjects[0] = GameObject.Find("Leg_FR");
        legObjects[1] = GameObject.Find("Leg_FL");
        legObjects[2] = GameObject.Find("Leg_MR");
        legObjects[3] = GameObject.Find("Leg_ML");
        legObjects[4] = GameObject.Find("Leg_RR");
        legObjects[5] = GameObject.Find("Leg_RL");

        HexapodLeg[] legComponents = new HexapodLeg[6];

        for (int i = 0; i < 6; i++)
        {
            if (legObjects[i] == null) continue;

            HexapodLeg legComponent = legObjects[i].GetComponent<HexapodLeg>();
            if (legComponent == null)
            {
                legComponent = legObjects[i].AddComponent<HexapodLeg>();
            }

            // Find and assign joints
            legComponent.hip = FindChildWithName(legObjects[i], "HipJoint").transform;
            legComponent.femur = FindChildWithName(legComponent.hip.gameObject, "FemurJoint").transform;
            legComponent.tibia = FindChildWithName(legComponent.femur.gameObject, "TibiaJoint").transform;
            legComponent.footTarget = FindChildWithName(legObjects[i], legObjects[i].name + "_FootTarget").transform;

            // Set parameters
            legComponent.femurLength = 0.45f;
            legComponent.tibiaLength = 0.65f;

            legComponents[i] = legComponent;
        }

        controller.legs = legComponents;
    }

    private GameObject FindChildWithName(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        return child != null ? child.gameObject : null;
    }
}