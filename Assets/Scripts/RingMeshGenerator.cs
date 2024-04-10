using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RingMeshGenerator : MonoBehaviour
{
    [Header("Major Radius")]
    [SerializeField] public float R = 5f; // Major Radius
    [Header("Minor Radius")]
    [SerializeField] public float r = 1f; // Minor Radius
    public int majorSegments = 36; // Segments around major radius
    public int minorSegments = 24; // Segments around minor radius

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateTorus(R, r, majorSegments, minorSegments);

        // Set collider
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;

        // Set color
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.color = Color.green;
    }

    private void Update()
    {
        // Generate mesh every frame, performance heavy but good for testing sizes
        //meshFilter.mesh = GenerateTorus(R, r, majorSegments, minorSegments);
    }

    private void OnTriggerEnter(Collider collider)
    {
        //if (meshFilter == null) { return; }

        Debug.Log("Trigger hit");
        meshRenderer.material.color = Color.red;
    }

    Mesh GenerateTorus(float R, float r, int majorSegments, int minorSegments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(majorSegments + 1) * (minorSegments + 1)];
        int[] triangles = new int[majorSegments * minorSegments * 6];
        int vertIndex = 0, triIndex = 0;

        for (int i = 0; i <= majorSegments; i++)
        {
            float theta = (float)i / majorSegments * Mathf.PI * 2;
            for (int j = 0; j <= minorSegments; j++)
            {
                float phi = (float)j / minorSegments * Mathf.PI * 2;
                Vector3 p = new Vector3(
                    (R + r * Mathf.Cos(phi)) * Mathf.Cos(theta),
                    (R + r * Mathf.Cos(phi)) * Mathf.Sin(theta),
                    r * Mathf.Sin(phi)
                );
                vertices[vertIndex++] = p;
            }
        }

        for (int i = 0; i < majorSegments; i++)
        {
            for (int j = 0; j < minorSegments; j++)
            {
                int current = i * (minorSegments + 1) + j;
                int next = (i + 1) * (minorSegments + 1) + j;

                // Adjusted triangle definitions to fix normals
                triangles[triIndex++] = current;
                triangles[triIndex++] = next + 1;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }


        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
