
using UnityEngine;

public class LatteArtPainter : MonoBehaviour
{
    [Header("Drawing Texture")]
    [SerializeField] private int textureSize = 1024;

    private RenderTexture drawingTexture;

    public RenderTexture DrawingTexture => drawingTexture;

    private void Awake()
    {
        CreateDrawingTexture();
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

    public void ClearDrawing()
    {
        if (drawingTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = drawingTexture;

        GL.Clear(
            true,
            true,
            Color.clear
        );

        RenderTexture.active = previous;
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
        {
            drawingTexture.Release();
            Destroy(drawingTexture);
        }
    }
}
