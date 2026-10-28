/*
using UnityEngine;

public class LatteArtPainter : MonoBehaviour
{
    [Header("Drawing Texture")]
    [SerializeField] private int textureSize = 1024;

    [Header("Surface Renderer")]
    [SerializeField] private Renderer latteSurfaceRenderer;

    [Header("Brush Settings")]
    [SerializeField, Range(0.001f, 0.1f)]
    private float brushSize = 0.025f;

    [SerializeField]
    private Color brushColor =
        new Color(1f, 0.95f, 0.85f, 1f);

    [Header("Dynamic Brush Size - Tilt")]
    [SerializeField] private bool useDynamicBrushSize = true;

    [SerializeField, Range(0.005f, 0.1f)]
    private float minBrushSize = 0.015f;

    [SerializeField, Range(0.005f, 0.1f)]
    private float maxBrushSize = 0.055f;

    [SerializeField] private float minPourAngle = 45f;
    [SerializeField] private float maxPourAngle = 90f;

    [Header("Dynamic Brush Size - Height")]
    [SerializeField] private bool useHeightBrushSize = true;

    [SerializeField] private Transform pourPoint;

    [SerializeField] private float nearSurfaceDistance = 0.05f;
    [SerializeField] private float farSurfaceDistance = 0.35f;

    [SerializeField] private float nearBrushMultiplier = 1.5f;
    [SerializeField] private float farBrushMultiplier = 0.5f;

    [Header("Milk Surface Visibility")]
    [SerializeField] private bool useHeightVisibility = true;

    // 이 거리 이하면 흰색 패턴이 생성됨
    [SerializeField] private float fullVisibilityDistance = 0.05f;

    // 이 거리 이상이면 표면에 패턴이 남지 않음
    [SerializeField] private float noVisibilityDistance = 0.25f;

    // 드로잉을 허용하는 최소 가시도
    [SerializeField, Range(0f, 1f)]
    private float minPaintVisibility = 0.15f;

    [Header("Pouring Input")]
    [SerializeField] private MilkPourController milkPourController;
    [SerializeField] private LatteSurfaceMesh latteSurfaceMesh;

    private RenderTexture drawingTexture;
    private Texture2D brushTexture;

    private static readonly int LatteArtTextureID =
        Shader.PropertyToID("_LatteArtTexture");

    [Header("Brush Interpolation")]
    [SerializeField] private bool useBrushInterpolation = true;

    [SerializeField, Range(0.1f, 1f)]
    private float brushSpacing = 0.3f;

    private Vector2 previousUV;
    private bool hasPreviousUV = false;

    [Header("Milk Foam Expansion")]
    [SerializeField] private bool useFoamExpansion = true;

    // 같은 위치에 머무를수록 거품이 커지는 속도
    [SerializeField] private float expansionSpeed = 0.8f;

    // 기본 브러시 크기에 곱할 최대 확산 배율
    [SerializeField] private float maxExpansionMultiplier = 4f;

    // 같은 위치로 판단할 UV 거리
    [SerializeField] private float stationaryThreshold = 0.015f;

    // 움직이면 누적 확산량이 줄어드는 속도


    private float foamExpansion = 0f;

    [Header("Heart Controller")]
    [SerializeField] private LatteHeartController heartController;

    [Header("Heart Foam Mask")]
    [SerializeField] private bool useHeartFoamMask = true;

    private RenderTexture heartFoamTexture;

    public RenderTexture HeartFoamTexture => heartFoamTexture;

    public RenderTexture DrawingTexture => drawingTexture;


    private void Awake()
    {
        CreateDrawingTexture();
        CreateHeartFoamTexture();
        CreateBrush();

        if (latteSurfaceRenderer != null)
        {
            latteSurfaceRenderer.material.SetTexture(
                LatteArtTextureID,
                drawingTexture
            );

            Debug.Log("라떼아트 텍스처 연결 완료");
        }
        else
        {
            Debug.LogError(
                "LatteArtPainter: LatteSurface Renderer가 없습니다."
            );
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearDrawing();
        }

        if (milkPourController == null || latteSurfaceMesh == null)
            return;

        if (!milkPourController.IsHittingSurface)
        {
            hasPreviousUV = false;
            foamExpansion = 0f;
            return;
        }

        Vector3 hitPoint =
            milkPourController.CurrentSurfacePoint;

        Vector2 uv =
            latteSurfaceMesh.WorldToSurfaceUV(hitPoint);

        // 기존 기울기 및 높이 기반 브러시 크기
        UpdateBrushSize(hitPoint);

        // 높이에 따른 표면 드로잉 여부
        float visibility = CalculatePaintVisibility(hitPoint);

        if (visibility < minPaintVisibility)
        {
            hasPreviousUV = false;
            foamExpansion = 0f;
            return;
        }

        // 같은 위치에 머무른 시간에 따른 확산 배율
        float expansionMultiplier = CalculateFoamExpansion(uv);

        // 이번 프레임에 사용할 브러시 크기
        float originalBrushSize = brushSize;

        brushSize = Mathf.Min(
     originalBrushSize * expansionMultiplier,
     0.65f
 );

        // 하트가 완성되었다면 추가 드로잉 중단
        if (heartController != null &&
            heartController.HeartFinishDetected)
        {
            brushSize = originalBrushSize;
            hasPreviousUV = false;
            return;
        }

        // 먼저 기존 우유 드로잉 실행
        PaintInterpolated(uv);

        // 그다음 거품 상태 및 마무리 동작 판단
        if (heartController != null)
        {
            heartController.UpdateFoam(
                uv,
                brushSize,
                foamExpansion
            );
        }

        brushSize = originalBrushSize;
    }

    private void UpdateBrushSize(Vector3 surfacePoint)
    {
        float finalBrushSize = brushSize;

        // 1. 피처 기울기에 따른 브러시 굵기
        if (useDynamicBrushSize && milkPourController != null)
        {
            float angle = Vector3.Angle(
                milkPourController.transform.up,
                Vector3.up
            );

            float t = Mathf.InverseLerp(
                minPourAngle,
                maxPourAngle,
                angle
            );

            finalBrushSize = Mathf.Lerp(
                minBrushSize,
                maxBrushSize,
                t
            );
        }

        // 2. 피처 높이에 따른 브러시 굵기
        if (useHeightBrushSize && pourPoint != null)
        {
            // 수면과 PourPoint 사이의 수직 거리
            float distance = Mathf.Abs(
                pourPoint.position.y - surfacePoint.y
            );

            float heightT = Mathf.InverseLerp(
                nearSurfaceDistance,
                farSurfaceDistance,
                distance
            );

            float multiplier = Mathf.Lerp(
                nearBrushMultiplier,
                farBrushMultiplier,
                heightT
            );

            finalBrushSize *= multiplier;
        }

        brushSize = Mathf.Clamp(
            finalBrushSize,
            0.001f,
            0.1f
        );
    }

    private void CreateDrawingTexture()
    {
        drawingTexture = new RenderTexture(
            textureSize,
            textureSize,
            0,
            RenderTextureFormat.ARGB32
        );

        drawingTexture.name = "LatteArtTexture";
        drawingTexture.filterMode = FilterMode.Bilinear;
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        drawingTexture.Create();

        ClearDrawing();

        Debug.Log("LatteArt RenderTexture 생성 완료");
    }

    private void CreateBrush()
    {
        int size = 64;

        brushTexture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;

                float distance = Mathf.Sqrt(
                    dx * dx + dy * dy
                );

                Color pixel = brushColor;

                pixel.a *= 1f - Mathf.SmoothStep(
                    0.85f,
                    1.0f,
                    distance
                );

                brushTexture.SetPixel(x, y, pixel);
            }
        }

        brushTexture.Apply();
    }

    public void PaintAtUV(Vector2 uv)
    {
        // 최종 라떼아트에는 항상 기록
        PaintToTexture(drawingTexture, uv);

        // 하트 거품이 활성화된 동안에만 별도 마스크 기록
        if (useHeartFoamMask &&
            heartFoamTexture != null &&
            heartController != null &&
            heartController.IsHeartFoamActive)
        {
            PaintToTexture(heartFoamTexture, uv);
        }
    }
    private void PaintToTexture(RenderTexture target, Vector2 uv)
    {
        if (target == null || brushTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, textureSize, 0, textureSize);

        float diameter = brushSize * textureSize;

        Rect rect = new Rect(
            uv.x * textureSize - diameter * 0.5f,
            uv.y * textureSize - diameter * 0.5f,
            diameter,
            diameter
        );

        Graphics.DrawTexture(rect, brushTexture);

        GL.PopMatrix();

        RenderTexture.active = previous;
    }

    private void PaintInterpolated(Vector2 currentUV)
    {
        if (!useBrushInterpolation || !hasPreviousUV)
        {
            PaintAtUV(currentUV);

            previousUV = currentUV;
            hasPreviousUV = true;
            return;
        }

        float distance = Vector2.Distance(previousUV, currentUV);

        float stepSize = Mathf.Max(
            brushSize * brushSpacing,
            0.001f
        );

        int steps = Mathf.CeilToInt(distance / stepSize);

        steps = Mathf.Clamp(steps, 1, 200);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;

            Vector2 uv = Vector2.Lerp(
                previousUV,
                currentUV,
                t
            );

            PaintAtUV(uv);
        }

        previousUV = currentUV;
        hasPreviousUV = true;
    }

    public void ClearDrawing()
    {
        ClearHeartFoamTexture();
        if (heartController != null)
        {
            heartController.ResetFoam();
        }
        hasPreviousUV = false;
        foamExpansion = 0f;

        if (drawingTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = drawingTexture;

        GL.Clear(true, true, Color.clear);

        RenderTexture.active = previous;
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
        {
            drawingTexture.Release();
            Destroy(drawingTexture);
        }

        if (brushTexture != null)
        {
            Destroy(brushTexture);
        }
        if (heartFoamTexture != null)
        {
            heartFoamTexture.Release();
            Destroy(heartFoamTexture);
        }
    }

    private float CalculatePaintVisibility(Vector3 surfacePoint)
    {
        if (!useHeightVisibility || pourPoint == null)
            return 1f;

        float distance = Mathf.Max(
            0f,
            pourPoint.position.y - surfacePoint.y
        );

        float t = Mathf.InverseLerp(
            fullVisibilityDistance,
            noVisibilityDistance,
            distance
        );

        // 가까우면 1, 멀어지면 0
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    private float CalculateFoamExpansion(Vector2 currentUV)
    {
        if (!useFoamExpansion)
            return 1f;

        if (!hasPreviousUV)
        {
            foamExpansion = 0f;
            return 1f;
        }

        float movement = Vector2.Distance(
            previousUV,
            currentUV
        );

        if (movement < stationaryThreshold)
        {
            // 한 지점에 머무르면 거품 확산
            foamExpansion += expansionSpeed * Time.deltaTime;
        }
        else
        {
            // 이동이 시작되면 확산 브러시 즉시 해제
            foamExpansion = 0f;
        }

        foamExpansion = Mathf.Clamp01(foamExpansion);

        return Mathf.Lerp(
            1f,
            maxExpansionMultiplier,
            foamExpansion
        );
    }
    private void CreateHeartFoamTexture()
    {
        if (!useHeartFoamMask)
            return;

        heartFoamTexture = new RenderTexture(
            textureSize,
            textureSize,
            0,
            RenderTextureFormat.ARGB32
        );

        heartFoamTexture.name = "HeartFoamTexture";
        heartFoamTexture.filterMode = FilterMode.Bilinear;
        heartFoamTexture.wrapMode = TextureWrapMode.Clamp;

        heartFoamTexture.Create();

        ClearHeartFoamTexture();

        Debug.Log("Heart Foam Texture 생성 완료");
    }

    private void ClearHeartFoamTexture()
    {
        if (heartFoamTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = heartFoamTexture;
        GL.Clear(true, true, Color.clear);

        RenderTexture.active = previous;
    }

}
*/