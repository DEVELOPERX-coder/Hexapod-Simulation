using UnityEngine;
using System.Collections.Generic;

public class HexapodGenerator : MonoBehaviour
{
    [Header("Body Configuration")]
    public float bodyRadius = 1.0f;
    public float bodyHeight = 0.3f;
    public Material bodyMaterial;

    [Header("Leg Configuration")]
    public float hipLength = 0.4f;
    public float femurLength = 0.8f;
    public float tibiaLength = 0.1f;
    public Material legMaterial;

    [Header("Joint Configuration")]
    public float jointRadius = 0.15f;
    public Material jointMaterial;

    private List<Transform[]> legs = new List<Transform[]>();

    private void Awake()
    {
        GenerateHexapod();
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    private GameObject CreateHexagonalBody()
    {
        GameObject body = new GameObject("Body");
        MeshFilter meshFilter = body.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = body.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[12];
        for(int i = 0; i < 6; ++i)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            vertices[i] = new Vector3(
                Mathf.Cos(angle) * bodyRadius,
                bodyHeight / 2,
                Mathf.Sin(angle) * bodyRadius
                );

            vertices[i + 6] = new Vector3(
                Mathf.Cos(angle) * bodyRadius,
                -bodyHeight / 2,
                Mathf.Sin(angle) * bodyRadius
                );
        }

        int[] triangles = new int[72];

        int idx = 0;
        for(int i = 0; i < 4; ++i)
        {
            triangles[idx++] = 0;
            triangles[idx++] = i + 1;
            triangles[idx++] = i + 2;
        }

        for (int i = 0; i < 4; ++i)
        {
            triangles[idx++] = 6;
            triangles[idx++] = 8 + 1;
            triangles[idx++] = 7 + 2;
        }

        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;

            // First triangle
            triangles[idx++] = i;
            triangles[idx++] = i + 6;
            triangles[idx++] = next;

            // Second triangle
            triangles[idx++] = next;
            triangles[idx++] = i + 6;
            triangles[idx++] = next + 6;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = bodyMaterial != null ? bodyMaterial : CreateDefaultMaterial(Color.gray);

        // Add a collider
        MeshCollider collider = body.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;

        // Add a rigidbody
        Rigidbody rb = body.AddComponent<Rigidbody>();
        rb.mass = 2.0f;

        return body;
    }

    public void GenerateHexapod()
    {
        GameObject body = CreateHexagonalBody();
    }
}