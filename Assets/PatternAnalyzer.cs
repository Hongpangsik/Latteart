using UnityEngine;

public class PatternAnalyzer : MonoBehaviour
{
    [SerializeField] private Texture2D referenceTexture;
    [SerializeField] private LatteTextureExporter textureExporter;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private float threshold = 0.5f;

    public void Analyze()
    {
        Texture2D userTexture = textureExporter.CreateTextureFromDrawing();

        if (referenceTexture == null || userTexture == null)
        {
            Debug.LogWarning("ReferenceTexture 또는 UserTexture가 없습니다.");
            return;
        }

        float score = CompareTextures(referenceTexture, userTexture);

        Debug.Log($"라떼아트 유사도 점수: {score:F1}점");

        if (scoreUI != null)
        {
            scoreUI.SetScore(score);
        }
    }

    private float CompareTextures(Texture2D reference, Texture2D user)
    {
        if (reference.width != user.width || reference.height != user.height)
        {
            Debug.LogError("이미지 크기가 다릅니다.");
            return 0f;
        }

        int referenceMilkPixels = 0;
        int matchedPixels = 0;
        int wrongPixels = 0;

        for (int y = 0; y < reference.height; y++)
        {
            for (int x = 0; x < reference.width; x++)
            {
                bool refMilk = IsMilkPixel(reference.GetPixel(x, y));
                bool userMilk = IsMilkPixel(user.GetPixel(x, y));

                if (refMilk)
                {
                    referenceMilkPixels++;

                    if (userMilk)
                    {
                        matchedPixels++;
                    }
                }
                else
                {
                    if (userMilk)
                    {
                        wrongPixels++;
                    }
                }
            }
        }

        if (referenceMilkPixels == 0)
        {
            return 0f;
        }

        float matchScore = (float)matchedPixels / referenceMilkPixels * 100f;
        float penaltyScore = (float)wrongPixels / referenceMilkPixels * 50f;

        float finalScore = matchScore - penaltyScore;

        finalScore = Mathf.Clamp(finalScore, 0f, 100f);

        Debug.Log($"맞춘 픽셀: {matchedPixels}");
        Debug.Log($"틀린 픽셀: {wrongPixels}");
        Debug.Log($"가점: {matchScore:F1}");
        Debug.Log($"감점: {penaltyScore:F1}");

        return finalScore;
    }

    private bool IsMilkPixel(Color color)
    {
        float brightness = (color.r + color.g + color.b) / 3f;
        return brightness >= threshold;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Analyze();
        }
    }
}