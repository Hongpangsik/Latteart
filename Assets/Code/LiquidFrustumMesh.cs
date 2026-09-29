using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LiquidFrustumMesh : MonoBehaviour
{
    [Header("Frustum Shape")]
    [SerializeField] private float topRadius = 0.85f;
    [SerializeField] private float bottomRadius = 0.45f;
    [SerializeField] private float height = 1f;

    [Range(8, 128)]
    [SerializeField] private int segments = 64;

    private Mesh mesh;

    private void Awake()
    {
        CreateMesh();
    }

    private void OnValidate()
    {
        CreateMesh();
    }

    private void CreateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Liquid Frustum";
        }
        else
        {
            mesh.Clear();
        }

        int vertexCount = segments * 2 + 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];

        int topCenter = segments * 2;
        int bottomCenter = segments * 2 + 1;

        // ¿·¸é¿ë À§/¾Æ·¡ Á¤Á¡
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i] = new Vector3(
    x * bottomRadius,
    0f,
    z * bottomRadius
);

            vertices[i + segments] = new Vector3(
    x * topRadius,
    height,
    z * topRadius
);

            uv[i] = new Vector2(
                (float)i / segments,
                0f
            );

            uv[i + segments] = new Vector2(
                (float)i / segments,
                1f
            );
        }

        vertices[topCenter] = new Vector3(0f, height, 0f);
        vertices[bottomCenter] = new Vector3(0f, 0f, 0f);

        uv[topCenter] = new Vector2(0.5f, 0.5f);
        uv[bottomCenter] = new Vector2(0.5f, 0.5f);

        // ¿·¸é + À§ + ¾Æ·¡
        int[] triangles = new int[segments * 12];

        int triangleIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            int bottomA = i;
            int bottomB = next;

            int topA = i + segments;
            int topB = next + segments;

            // ¿·¸é
            triangles[triangleIndex++] = bottomA;
            triangles[triangleIndex++] = topA;
            triangles[triangleIndex++] = topB;

            triangles[triangleIndex++] = bottomA;
            triangles[triangleIndex++] = topB;
            triangles[triangleIndex++] = bottomB;

            // À­¸é
            triangles[triangleIndex++] = topCenter;
            triangles[triangleIndex++] = topB;
            triangles[triangleIndex++] = topA;

            // ¾Æ·§¸é
            triangles[triangleIndex++] = bottomCenter;
            triangles[triangleIndex++] = bottomA;
            triangles[triangleIndex++] = bottomB;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }
}