using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LatteSurfaceMesh : MonoBehaviour
{
    [Header("Liquid Frustum")]
    [SerializeField] private Transform frustumTransform;

    [Header("Frustum Shape")]
    [SerializeField] private float bottomRadius = 0.4f;
    [SerializeField] private float topRadius = 0.65f;
    [SerializeField] private float height = 0.8f;

    [Header("Surface")]
    [Range(8, 128)]
    [SerializeField] private int segments = 64;

    [Header("Intersection")]
    [SerializeField] private float maxSearchDistance = 1.5f;
    [SerializeField] private int searchIterations = 20;

    private Mesh mesh;

    private Vector3[] vertices;
    private Vector2[] uv;
    private int[] triangles;

    private void Awake()
    {
        CreateMesh();
    }

    private void OnValidate()
    {
        CreateMesh();
    }

    private void LateUpdate()
    {
        UpdateSurface();
    }

    private void CreateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Dynamic Latte Surface";
        }
        else
        {
            mesh.Clear();
        }

        vertices = new Vector3[segments + 1];
        uv = new Vector2[segments + 1];
        triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private void UpdateSurface()
    {
        if (frustumTransform == null || mesh == null)
            return;

        // LatteSurface의 중심은 LiquidFillController가
        // 현재 worldFillHeight 위치로 이동시켜 준다.
        Vector3 surfaceCenter = transform.position;

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            // LatteSurface는 항상 월드 수평이므로
            // 월드 XZ 평면에서 바깥쪽으로 탐색한다.
            Vector3 worldDirection = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            );

            float distance = FindIntersectionDistance(
                surfaceCenter,
                worldDirection
            );

            Vector3 worldPoint =
                surfaceCenter + worldDirection * distance;

            // Mesh 정점은 LatteSurface의 Local 좌표로 저장
            vertices[i + 1] =
                transform.InverseTransformPoint(worldPoint);

            // 라떼아트용 UV는 중심 0.5, 0.5 기준으로 유지
            float normalizedDistance =
                maxSearchDistance > 0f
                ? distance / maxSearchDistance
                : 0f;

            uv[i + 1] = new Vector2(
                0.5f + Mathf.Cos(angle) * normalizedDistance * 0.5f,
                0.5f + Mathf.Sin(angle) * normalizedDistance * 0.5f
            );
        }

        mesh.vertices = vertices;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private float FindIntersectionDistance(
        Vector3 surfaceCenter,
        Vector3 worldDirection)
    {
        float low = 0f;
        float high = maxSearchDistance;

        // 이분 탐색으로 컵 내부/외부 경계를 찾는다.
        for (int i = 0; i < searchIterations; i++)
        {
            float mid = (low + high) * 0.5f;

            Vector3 worldPoint =
                surfaceCenter + worldDirection * mid;

            if (IsInsideFrustum(worldPoint))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private bool IsInsideFrustum(Vector3 worldPoint)
    {
        // 월드 위치를 컵의 Local 좌표로 변환
        Vector3 localPoint =
            frustumTransform.InverseTransformPoint(worldPoint);

        // Frustum 높이 밖이면 컵 내부가 아님
        float heightTolerance = 0.001f;

        if (localPoint.y < -heightTolerance ||
            localPoint.y > height + heightTolerance)
        {
            return false;
        }

        float heightRatio =
            Mathf.Clamp01(localPoint.y / height);

        // 해당 높이에서의 컵 반지름
        float radiusAtHeight =
            Mathf.Lerp(
                bottomRadius,
                topRadius,
                heightRatio
            );

        float radialDistance =
            new Vector2(
                localPoint.x,
                localPoint.z
            ).magnitude;

        return radialDistance <= radiusAtHeight;
    }
    public bool IsWorldPointInsideSurface(Vector3 worldPoint)
    {
        if (frustumTransform == null)
            return false;

        // 검사할 위치를 컵의 로컬 좌표로 변환
        Vector3 localPoint =
            frustumTransform.InverseTransformPoint(worldPoint);

        // 해당 높이가 Frustum 범위 밖이면 실패
        float heightTolerance = 0.001f;

        if (localPoint.y < -heightTolerance ||
            localPoint.y > height + heightTolerance)
        {
            return false;
        }

        // 현재 높이에서 컵의 반지름 계산
        float heightRatio =
            Mathf.Clamp01(localPoint.y / height);

        float radiusAtHeight =
            Mathf.Lerp(
                bottomRadius,
                topRadius,
                heightRatio
            );

        // 컵 중심축에서 얼마나 떨어져 있는지
        float radialDistance =
            new Vector2(
                localPoint.x,
                localPoint.z
            ).magnitude;

        return radialDistance <= radiusAtHeight;
    }
}