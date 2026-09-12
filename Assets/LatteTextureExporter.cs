using UnityEngine;
using System.IO;

public class LatteTextureExporter : MonoBehaviour
{
    [SerializeField] private LatteDrawingManager drawingManager;
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float brushSize = 8f;
    [SerializeField] private Transform cupSurface;
    [SerializeField] private float surfaceWidth = 3f;
    [SerializeField] private float surfaceHeight = 3f;

    public void ExportToPNG()
    {
        Texture2D texture = CreateTextureFromDrawing();

        byte[] pngData = texture.EncodeToPNG();

        string path = System.IO.Path.Combine(Application.dataPath, "LatteArt_Result.png");
        System.IO.File.WriteAllBytes(path, pngData);

        Debug.Log("PNG 저장 완료: " + path);
    }

    private void ClearTexture(Texture2D texture)
    {
        Color[] pixels = new Color[textureSize * textureSize];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void DrawStrokes(Texture2D texture)
    {
        foreach (LatteStrokeData stroke in drawingManager.drawingData.strokes)
        {
            foreach (Vector3 point in stroke.points)
            {
                Vector2 uv = WorldToUV(point);
                DrawCircle(texture, uv, brushSize, Color.white);
            }
        }

        texture.Apply();
    }

    private Vector2 WorldToUV(Vector3 worldPoint)
    {
        Vector3 localPoint = cupSurface.InverseTransformPoint(worldPoint);

        float u = Mathf.InverseLerp(-surfaceWidth / 2f, surfaceWidth / 2f, localPoint.x);
        float v = Mathf.InverseLerp(-surfaceHeight / 2f, surfaceHeight / 2f, localPoint.z);

        return new Vector2(u, v);
    }

    private void DrawCircle(Texture2D texture, Vector2 uv, float radius, Color color)
    {
        int centerX = Mathf.RoundToInt(uv.x * textureSize);
        int centerY = Mathf.RoundToInt(uv.y * textureSize);

        int r = Mathf.RoundToInt(radius);

        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y <= r * r)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            ExportToPNG();
        }
    }
    public Texture2D CreateTextureFromDrawing()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        ClearTexture(texture);
        DrawStrokes(texture);

        return texture;
    }
}