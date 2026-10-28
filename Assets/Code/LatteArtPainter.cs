
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
    [Header("Dynamic Brush Size")]
    [SerializeField] private bool useDynamicBrushSize = true;

    [SerializeField, Range(0.005f, 0.1f)]
    private float minBrushSize = 0.015f;

    [SerializeField, Range(0.005f, 0.1f)]
    private float maxBrushSize = 0.055f;

    [SerializeField] private float minPourAngle = 60f;
    [SerializeField] private float maxPourAngle = 90f;

    [Header("Pouring Input")]
    [SerializeField] private MilkPourController milkPourController;
    [SerializeField] private LatteSurfaceMesh latteSurfaceMesh;

    [SerializeField]
    private Color brushColor =
        new Color(1f, 0.95f, 0.85f, 1f);

    private RenderTexture drawingTexture;
    private Texture2D brushTexture;
    private Material brushMaterial;

    private static readonly int LatteArtTextureID =
        Shader.PropertyToID("_LatteArtTexture");

    public RenderTexture DrawingTexture => drawingTexture;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearDrawing();
        }
        if (milkPourController == null || latteSurfaceMesh == null)
            return;

        // 우유가 수면에 닿지 않으면 그리지 않음
        if (!milkPourController.IsHittingSurface)
            return;

        // 우유가 닿은 위치 가져오기
        Vector3 hitPoint = milkPourController.CurrentSurfacePoint;

        // 월드 좌표를 UV 좌표로 변환
        Vector2 uv = latteSurfaceMesh.WorldToSurfaceUV(hitPoint);

        UpdateBrushSize();
        // 해당 위치에 우유색 브러시 그리기
        PaintAtUV(uv);
    }
    private void Awake()
    {
        // 1. 라떼아트용 RenderTexture 생성
        CreateDrawingTexture();

        // 2. 원형 브러시 생성
        CreateBrush();

        // 3. LatteSurface의 Material에 RenderTexture 연결
        if (latteSurfaceRenderer != null)
        {
            Material surfaceMaterial = latteSurfaceRenderer.material;

            surfaceMaterial.SetTexture(
                LatteArtTextureID,
                drawingTexture
            );

            Debug.Log("라떼아트 텍스처 연결 완료");

            // 4. 연결 상태 확인
            Debug.Log(
                "Painter Material: " +
                surfaceMaterial.name
            );

            Debug.Log(
                "Painter Texture: " +
                surfaceMaterial.GetTexture(LatteArtTextureID)
            );
        }
        else
        {
            Debug.LogError(
                "LatteArtPainter: Latte Surface Renderer가 연결되지 않았습니다."
            );
        }
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

                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 1f - Mathf.SmoothStep(
                    0.7f, 1f, distance
                );

                Color pixel = Color.white;

                pixel.a = 1f - Mathf.SmoothStep(
                    0.85f,
                    1.0f,
                    distance
                );

                brushTexture.SetPixel(x, y, pixel);
            }
        }

        brushTexture.Apply();

        Shader shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
        {
            Debug.LogError("브러시 Shader를 찾지 못했습니다.");
            return;
        }

        brushMaterial = new Material(shader);
        brushMaterial.mainTexture = brushTexture;
    }

    public void PaintAtUV(Vector2 uv)
    {
        if (drawingTexture == null || brushTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = drawingTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, textureSize, 0, textureSize);

        float diameter = brushSize * textureSize;

        Rect rect = new Rect(
            uv.x * textureSize - diameter * 0.5f,
            uv.y * textureSize - diameter * 0.5f,
            diameter,
            diameter
        );

        // 우선 별도 Material 없이 브러시 텍스처를 직접 그리기
        Graphics.DrawTexture(rect, brushTexture);

        GL.PopMatrix();

        RenderTexture.active = previous;

        Debug.Log($"브러시 테스트: UV={uv}, 크기={diameter}");
    }

    public void ClearDrawing()
    {
        if (drawingTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = drawingTexture;

        // 투명한 빨간색으로 초기화
        GL.Clear(true, true, Color.clear);

        RenderTexture.active = previous;

        Debug.Log("RenderTexture 투명 초기화 완료");
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
        {
            drawingTexture.Release();
            Destroy(drawingTexture);
        }

        if (brushTexture != null)
            Destroy(brushTexture);

        if (brushMaterial != null)
            Destroy(brushMaterial);
    }
    private void UpdateBrushSize()
    {
        if (!useDynamicBrushSize || milkPourController == null)
            return;

        // 피처의 기울기 계산
        float angle = Vector3.Angle(
            milkPourController.transform.up,
            Vector3.up
        );

        // 45도에서 0, 90도에서 1
        float t = Mathf.InverseLerp(
            minPourAngle,
            maxPourAngle,
            angle
        );

        // 기울기에 따라 브러시 크기 변경
        brushSize = Mathf.Lerp(
            minBrushSize,
            maxBrushSize,
            t
        );
    }
}
