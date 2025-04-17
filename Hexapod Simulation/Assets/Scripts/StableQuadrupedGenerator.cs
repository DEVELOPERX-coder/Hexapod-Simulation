using UnityEngine;
using System.Collections.Generic;

public class StableQuadrupedGenerator : MonoBehaviour
{
    [Header("Body Configuration")]
    public float bodyLength = 1.5f;
    public float bodyWidth = 0.8f;
    public float bodyHeight = 0.3f;
    public Material bodyMaterial;

    [Header("Leg Configuration")]
    public float hipLength = 0.4f;
    public float femurLength = 0.8f;
    public float tibiaLength = 1.0f;
    public float legWidth = 0.1f;
    public Material legMaterial;

    [Header("Joint Configuration")]
    public float jointRadius = 0.15f;
    public Material jointMaterial;

    // Reference to the generated legs
    private List<Transform[]> legs = new List<Transform[]>();

    void Awake()
    {
        GenerateQuadruped();
    }

    public void GenerateQuadruped()
    {
        // Create the body
        GameObject body = CreateRectangularBody();
        body.transform.parent = transform;
        body.transform.localPosition = Vector3.zero;

        // Create the 4 legs at the corners
        Vector3[] legPositions = new Vector3[4]
        {
            new Vector3(bodyLength/2, 0, bodyWidth/2),     // Front Right
            new Vector3(bodyLength/2, 0, -bodyWidth/2),    // Front Left
            new Vector3(-bodyLength/2, 0, bodyWidth/2),    // Back Right
            new Vector3(-bodyLength/2, 0, -bodyWidth/2)    // Back Left
        };

        float[] legAngles = new float[4]
        {
            45f,    // Front Right
            135f,   // Front Left
            315f,   // Back Right (changed from -45 to 315 for outward orientation)
            225f,   // Back Left (changed from -135 to 225 for outward orientation)
        };

        for (int i = 0; i < 4; i++)
        {
            Transform[] legSegments = CreateLeg(i, legPositions[i], legAngles[i] * Mathf.Deg2Rad);
            legs.Add(legSegments);
        }

        // Use kinematic approach for more stability
        gameObject.AddComponent<KinematicQuadrupedController>();
    }

    private GameObject CreateRectangularBody()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";

        // Scale the cube to match the desired body dimensions
        body.transform.localScale = new Vector3(bodyLength, bodyHeight, bodyWidth);

        // Set the material
        Renderer renderer = body.GetComponent<Renderer>();
        renderer.material = bodyMaterial != null ? bodyMaterial : CreateDefaultMaterial(Color.gray);

        // Add a kinematic rigidbody (for stability)
        Rigidbody rb = body.AddComponent<Rigidbody>();
        rb.mass = 5.0f;
        rb.isKinematic = true; // Make kinematic for better stability

        return body;
    }

    private Transform[] CreateLeg(int legIndex, Vector3 position, float angle)
    {
        // Create a more structured leg hierarchy

        // Main leg parent at body corner
        GameObject legParent = new GameObject("Leg_" + legIndex);
        legParent.transform.parent = transform;
        legParent.transform.localPosition = position;
        legParent.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        // ===== HIP JOINT AND SEGMENT =====
        GameObject hipJoint = CreateJoint("Hip_" + legIndex);
        hipJoint.transform.parent = legParent.transform;
        hipJoint.transform.localPosition = Vector3.zero;

        // Create hip segment - extending outward from hip joint
        GameObject hipSegment = new GameObject("HipSegment_" + legIndex);
        hipSegment.transform.parent = hipJoint.transform;
        hipSegment.transform.localPosition = Vector3.zero;

        // Create the visual cylinder for hip
        GameObject hipVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hipVisual.name = "HipVisual_" + legIndex;
        hipVisual.transform.parent = hipSegment.transform;

        // Position cylinder to extend from joint (not centered on joint)
        hipVisual.transform.localPosition = new Vector3(hipLength / 2, 0, 0);
        hipVisual.transform.localRotation = Quaternion.Euler(0, 0, 90); // Rotate to align with X axis
        hipVisual.transform.localScale = new Vector3(legWidth, hipLength / 2, legWidth);

        // Set material
        hipVisual.GetComponent<Renderer>().material = legMaterial != null ? legMaterial : CreateDefaultMaterial(Color.blue);

        // ===== FEMUR JOINT AND SEGMENT =====
        GameObject femurJoint = CreateJoint("Femur_" + legIndex);
        femurJoint.transform.parent = hipSegment.transform;
        femurJoint.transform.localPosition = new Vector3(hipLength, 0, 0);  // Position at end of hip segment
        femurJoint.transform.localRotation = Quaternion.Euler(0, 0, -30);   // Angle downward

        // Create femur segment - extending from femur joint
        GameObject femurSegment = new GameObject("FemurSegment_" + legIndex);
        femurSegment.transform.parent = femurJoint.transform;
        femurSegment.transform.localPosition = Vector3.zero;

        // Create the visual cylinder for femur
        GameObject femurVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        femurVisual.name = "FemurVisual_" + legIndex;
        femurVisual.transform.parent = femurSegment.transform;

        // Position cylinder to extend from joint (not centered on joint)
        femurVisual.transform.localPosition = new Vector3(femurLength / 2, 0, 0);
        femurVisual.transform.localRotation = Quaternion.Euler(0, 0, 90); // Rotate to align with X axis
        femurVisual.transform.localScale = new Vector3(legWidth, femurLength / 2, legWidth);

        // Set material
        femurVisual.GetComponent<Renderer>().material = legMaterial != null ? legMaterial : CreateDefaultMaterial(Color.blue);

        // ===== TIBIA JOINT AND SEGMENT =====
        GameObject tibiaJoint = CreateJoint("Tibia_" + legIndex);
        tibiaJoint.transform.parent = femurSegment.transform;
        tibiaJoint.transform.localPosition = new Vector3(femurLength, 0, 0);  // Position at end of femur segment
        tibiaJoint.transform.localRotation = Quaternion.Euler(0, 0, 60);     // Angle to form a natural leg shape

        // Create tibia segment - extending from tibia joint
        GameObject tibiaSegment = new GameObject("TibiaSegment_" + legIndex);
        tibiaSegment.transform.parent = tibiaJoint.transform;
        tibiaSegment.transform.localPosition = Vector3.zero;

        // Create the visual cylinder for tibia
        GameObject tibiaVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tibiaVisual.name = "TibiaVisual_" + legIndex;
        tibiaVisual.transform.parent = tibiaSegment.transform;

        // Position cylinder to extend from joint (not centered on joint)
        tibiaVisual.transform.localPosition = new Vector3(tibiaLength / 2, 0, 0);
        tibiaVisual.transform.localRotation = Quaternion.Euler(0, 0, 90); // Rotate to align with X axis
        tibiaVisual.transform.localScale = new Vector3(legWidth, tibiaLength / 2, legWidth);

        // Set material
        tibiaVisual.GetComponent<Renderer>().material = legMaterial != null ? legMaterial : CreateDefaultMaterial(Color.blue);

        // ===== FOOT =====
        GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foot.name = "Foot_" + legIndex;
        foot.transform.parent = tibiaSegment.transform;
        foot.transform.localPosition = new Vector3(tibiaLength, 0, 0); // Position at end of tibia
        foot.transform.localScale = new Vector3(legWidth * 1.2f, legWidth * 1.2f, legWidth * 1.2f);
        foot.GetComponent<Renderer>().material = legMaterial != null ? legMaterial : CreateDefaultMaterial(Color.black);

        // Return the joints in order: hip, femur, tibia
        return new Transform[] { hipJoint.transform, femurJoint.transform, tibiaJoint.transform };
    }

    private GameObject CreateJoint(string name)
    {
        GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        joint.name = name;

        // Scale down for better proportions
        joint.transform.localScale = new Vector3(jointRadius, jointRadius, jointRadius);

        // Set material
        joint.GetComponent<Renderer>().material = jointMaterial != null ? jointMaterial : CreateDefaultMaterial(Color.red);

        // Remove collider to prevent physics issues
        DestroyImmediate(joint.GetComponent<SphereCollider>());

        return joint;
    }

    private GameObject CreateLegSegment(string name, float length, float width)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        segment.name = name;

        // Adjust rotation to make the cylinder point along the x-axis
        segment.transform.localRotation = Quaternion.Euler(0, 0, 90);

        // Scale to match desired dimensions
        segment.transform.localScale = new Vector3(width, length / 2, width);

        // Set material
        segment.GetComponent<Renderer>().material = legMaterial != null ? legMaterial : CreateDefaultMaterial(Color.blue);

        return segment;
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    public List<Transform[]> GetLegs()
    {
        return legs;
    }
}