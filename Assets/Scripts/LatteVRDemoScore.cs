using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// VR demo scoring: left-hand pinch evaluates the current painted surface.
/// Shows a world-space result above the cup, without requiring a Canvas.
/// </summary>
public class LatteVRDemoScore : MonoBehaviour
{
    [SerializeField] private LatteArtPainter painter;
    [SerializeField] private Transform cup;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float pinchThreshold = 0.03f;
    [SerializeField] private float resultHeight = 0.3f;
    private XRHandSubsystem hands;
    private bool wasPinching;
    private TextMesh scoreText;
    private GameObject label;

    private void Start()
    {
        label = new GameObject("VR Score Label");
        label.transform.SetParent(transform, false);
        scoreText = label.AddComponent<TextMesh>();
        scoreText.text = "Left pinch: Score";
        scoreText.fontSize = 60;
        scoreText.characterSize = 0.004f;
        scoreText.anchor = TextAnchor.MiddleCenter;
        scoreText.alignment = TextAlignment.Center;
        scoreText.color = Color.white;
        var renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = 50;
    }

    private void Update()
    {
        if (hands == null || !hands.running)
        {
            var list = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(list);
            hands = list.Find(h => h.running);
        }

        if (cup != null && label != null)
        {
            label.transform.position = cup.position + Vector3.up * resultHeight;
            Camera camera = Camera.main;
            if (camera != null)
                label.transform.rotation = Quaternion.LookRotation(label.transform.position - camera.transform.position);
        }

        if (hands == null || !hands.running || !hands.leftHand.isTracked)
        {
            wasPinching = false;
            return;
        }

        var index = hands.leftHand.GetJoint(XRHandJointID.IndexTip);
        var thumb = hands.leftHand.GetJoint(XRHandJointID.ThumbTip);
        if (!index.TryGetPose(out Pose a) || !thumb.TryGetPose(out Pose b))
        {
            wasPinching = false;
            return;
        }
        bool pinching = Vector3.Distance(a.position, b.position) <= pinchThreshold;
        if (pinching && !wasPinching) Evaluate();
        wasPinching = pinching;
    }

    public void Evaluate()
    {
        if (painter == null || painter.DrawingTexture == null)
        {
            if (scoreText != null) scoreText.text = "Drawing unavailable";
            return;
        }
        RenderTexture rt = painter.DrawingTexture;
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        RenderTexture tmp = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(rt, tmp);
            RenderTexture.active = tmp;
            texture.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            texture.Apply();
            int painted = 0;
            foreach (Color p in texture.GetPixels())
                if (p.a > 0.15f) painted++;
            float coverage = painted / 4096f;
            // This is a demo coverage metric, NOT reference-pattern similarity.
            float score = Mathf.Clamp01(coverage / 0.18f) * 100f;
            if (scoreText != null) scoreText.text = $"Demo Score: {score:F0} / 100";
            Debug.Log($"VR Demo coverage score: {score:F1}, painted area: {coverage:P1}");
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
            Destroy(texture);
        }
    }
}
