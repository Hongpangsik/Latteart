
using UnityEngine;
using System.IO;

public class LatteArtSaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LatteArtPainter latteArtPainter;

    private void Update()
    {
        // P 키: 라떼아트 PNG 저장
        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveDrawing();
        }
    }

    public void SaveDrawing()
    {
        if (latteArtPainter == null ||
            latteArtPainter.DrawingTexture == null)
        {
            Debug.LogWarning("저장할 라떼아트 텍스처가 없습니다.");
            return;
        }

        RenderTexture renderTexture =
            latteArtPainter.DrawingTexture;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGBA32,
            false
        );

        texture.ReadPixels(
            new Rect(
                0,
                0,
                renderTexture.width,
                renderTexture.height
            ),
            0,
            0
        );

        texture.Apply();

        RenderTexture.active = previous;

        byte[] pngData = texture.EncodeToPNG();

        string fileName =
            "LatteArt_" +
            System.DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".png";

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "LatteArt"
        );

        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(
            folderPath,
            fileName
        );

        File.WriteAllBytes(filePath, pngData);

        Destroy(texture);

        Debug.Log("라떼아트 저장 완료: " + filePath);
    }
}
