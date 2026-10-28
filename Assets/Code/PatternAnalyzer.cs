
using UnityEngine;

public class PatternAnalyzer : MonoBehaviour
{
    [Header("Reference Pattern")]
    [SerializeField] private Texture2D referenceTexture;

    [Header("Drawing System")]
    [SerializeField] private LatteArtPainter latteArtPainter;

    [Header("Score UI")]
    [SerializeField] private ScoreUI scoreUI;

    [Header("Evaluation Settings")]
    [SerializeField, Range(0f, 1f)]
    private float threshold = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float alphaThreshold = 0.1f;

    [SerializeField]
    private int evaluationSize = 256;

    private float lastScore;

    public float LastScore => lastScore;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Analyze();
        }
    }

    public void Analyze()
    {
        if (latteArtPainter == null)
        {
            Debug.LogError("LatteArtPainter is missing.");
            return;
        }

        if (referenceTexture == null)
        {
            Debug.LogError("Reference Texture is missing.");
            return;
        }

        RenderTexture drawingTexture =
            latteArtPainter.DrawingTexture;

        if (drawingTexture == null)
        {
            Debug.LogError("Drawing Texture is missing.");
            return;
        }

        Texture2D userTexture =
            ReadRenderTexture(drawingTexture);

        Texture2D resizedReference =
            ResizeTexture(referenceTexture, evaluationSize);

        Texture2D resizedUser =
            ResizeTexture(userTexture, evaluationSize);

        lastScore = CompareTextures(
            resizedReference,
            resizedUser
        );

        Debug.Log(
            $"Latte Art Evaluation Score: {lastScore:F1}"
        );

        if (scoreUI != null)
        {
            scoreUI.SetScore(lastScore);
        }

        Destroy(userTexture);
        Destroy(resizedReference);
        Destroy(resizedUser);
    }

    private Texture2D ReadRenderTexture(RenderTexture rt)
    {
        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = rt;

        Texture2D texture = new Texture2D(
            rt.width,
            rt.height,
            TextureFormat.RGBA32,
            false
        );

        texture.ReadPixels(
            new Rect(0, 0, rt.width, rt.height),
            0,
            0
        );

        texture.Apply();

        RenderTexture.active = previous;

        return texture;
    }

    private Texture2D ResizeTexture(
        Texture source,
        int size)
    {
        RenderTexture temporary =
            RenderTexture.GetTemporary(
                size,
                size,
                0,
                RenderTextureFormat.ARGB32
            );

        Graphics.Blit(source, temporary);

        Texture2D result =
            ReadRenderTexture(temporary);

        RenderTexture.ReleaseTemporary(temporary);

        return result;
    }

    private float CompareTextures(
        Texture2D reference,
        Texture2D user)
    {
        Color[] referencePixels = reference.GetPixels();
        Color[] userPixels = user.GetPixels();

        int referenceMilkPixels = 0;
        int matchedPixels = 0;
        int wrongPixels = 0;
        int userMilkPixels = 0;

        for (int i = 0; i < referencePixels.Length; i++)
        {
            bool refMilk = IsReferenceMilkPixel(
                referencePixels[i]
            );

            bool userMilk = IsUserMilkPixel(
                userPixels[i]
            );

            if (refMilk)
            {
                referenceMilkPixels++;

                if (userMilk)
                {
                    matchedPixels++;
                }
            }

            if (userMilk)
            {
                userMilkPixels++;

                if (!refMilk)
                {
                    wrongPixels++;
                }
            }
        }

        if (referenceMilkPixels == 0)
        {
            Debug.LogWarning(
                "No milk pixels in reference pattern."
            );

            return 0f;
        }

        float matchScore =
            (float)matchedPixels /
            referenceMilkPixels * 100f;

        float penaltyScore =
            (float)wrongPixels /
            referenceMilkPixels * 50f;

        float finalScore = Mathf.Clamp(
            matchScore - penaltyScore,
            0f,
            100f
        );

        Debug.Log(
            $"Reference Pixels: {referenceMilkPixels}"
        );

        Debug.Log(
            $"User Pixels: {userMilkPixels}"
        );

        Debug.Log(
            $"Matched Pixels: {matchedPixels}"
        );

        Debug.Log(
            $"Wrong Pixels: {wrongPixels}"
        );

        Debug.Log(
            $"Match Score: {matchScore:F1}"
        );

        Debug.Log(
            $"Penalty: {penaltyScore:F1}"
        );

        return finalScore;
    }

    private bool IsReferenceMilkPixel(Color color)
    {
        // Transparent reference image
        if (color.a < alphaThreshold)
        {
            return false;
        }

        float brightness =
            (color.r + color.g + color.b) / 3f;

        return brightness >= threshold;
    }

    private bool IsUserMilkPixel(Color color)
    {
        // The drawing RenderTexture has a transparent background.
        return color.a >= alphaThreshold;
    }
}
