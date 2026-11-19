using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Native PlayMode integration test: 10 complete pitcher-driven cycles in LattePlay.
// No direct UpdateFoam/PaintAtUV/TrackFinishMotion calls: normal Update() pipeline only.
public class LatteFullPourCyclePlayModeTests
{
    const int Cycles = 10;
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static Component Find(string type)
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c && c.GetType().Name == type && c.gameObject.scene == SceneManager.GetActiveScene()) return c;
        Assert.Fail("Scene component not found: " + type);
        return null;
    }

    static T Get<T>(object target, string name)
    {
        var t = target.GetType();
        while (t != null)
        {
            var f = t.GetField(name, Flags);
            if (f != null) return (T)f.GetValue(target);
            var p = t.GetProperty(name, Flags);
            if (p != null) return (T)p.GetValue(target, null);
            t = t.BaseType;
        }
        throw new MissingMemberException(target.GetType().Name, name);
    }

    static void Call(object target, string method)
    {
        var m = target.GetType().GetMethod(method, Flags);
        Assert.NotNull(m, "Method missing: " + method);
        m.Invoke(target, null);
    }

    static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    static Quaternion PourRotation(Transform pitcher, Transform spout, float angle)
    {
        Vector3 spoutLocal = pitcher.InverseTransformPoint(spout.position);
        Quaternion best = pitcher.rotation;
        float lowest = float.PositiveInfinity;
        foreach (Vector3 axis in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            float drop = (q * spoutLocal).y;
            if (drop < lowest) { lowest = drop; best = q; }
        }
        if (lowest >= -0.001f)
            throw new InvalidOperationException("No pour orientation puts PourPoint below pitcher pivot. Check prefab axes.");
        return best;
    }

    static void SetSpout(Transform pitcher, Transform spout, Quaternion rotation, Vector3 desiredSpoutWorld)
    {
        Vector3 localSpout = pitcher.InverseTransformPoint(spout.position);
        pitcher.rotation = rotation;
        pitcher.position = desiredSpoutWorld - rotation * localSpout;
    }

    static byte[] Capture(RenderTexture rt)
    {
        Assert.NotNull(rt, "DrawingTexture is null");
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        try
        {
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            return image.EncodeToPNG();
        }
        finally
        {
            UnityEngine.Object.Destroy(image);
            RenderTexture.active = old;
        }
    }

    [UnitySetUp]
    public IEnumerator OpenScene()
    {
        Assert.IsTrue(Application.CanStreamedLevelBeLoaded("LattePlay"), "Add LattePlay to Build Profiles Scene List");
        yield return SceneManager.LoadSceneAsync("LattePlay", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FullPourToHeart_ResetAndSave_TenCycles()
    {
        var pour = Find("MilkPourController");
        var painter = Find("LatteArtPainter");
        var heart = Find("LatteHeartController");
        var surface = Find("LatteSurfaceMesh");
        Assert.IsTrue(((Behaviour)pour).enabled && ((Behaviour)painter).enabled && ((Behaviour)heart).enabled);
        Transform pitcher = pour.transform;
        Transform spout = Get<Transform>(pour, "pourPoint");
        Transform water = Get<Transform>(pour, "latteSurface");
        Assert.NotNull(spout);
        Assert.NotNull(water);
        Assert.NotNull(Get<object>(pour, "milkStream"), "Milk stream must be assigned for hit detection");
        Assert.IsFalse(Get<bool>(pour, "requirePinchToPour"),
            "Disable requirePinchToPour in PC LattePlay scene for transform-driven test");
        var drawing = Get<RenderTexture>(painter, "DrawingTexture");
        Assert.NotNull(drawing);

        Vector3 initialPosition = pitcher.position;
        Quaternion initialRotation = pitcher.rotation;
        Quaternion pourRotation = PourRotation(pitcher, spout, 70f);
        float startAngle = Get<float>(pour, "pourStartAngle");
        Assert.Less(startAngle, 70f);
        float finishHeight = Get<float>(heart, "finishHeight");
        float minTravel = Get<float>(heart, "minFinishDistance");
        float referenceRadius = Mathf.Max(Get<float>(surface, "bottomRadius"), Get<float>(surface, "topRadius"));
        Vector3 center = water.position;
        float low = Mathf.Max(.035f, Mathf.Min(.045f, finishHeight * .45f));
        float high = Mathf.Max(finishHeight + .035f, .16f);
        float travel = Mathf.Max(minTravel * referenceRadius * 2f * 1.6f, .14f);
        travel = Mathf.Min(travel, referenceRadius * .45f);
        string output = Path.Combine(Application.persistentDataPath, "LatteCycleTests");
        Directory.CreateDirectory(output);

        int passed = 0;
        try
        {
            for (int cycle = 1; cycle <= Cycles; cycle++)
            {
                {
                    Call(painter, "ClearDrawing");
                    center = water.position;
                    // Tilt back: verify pouring is stopped before each cycle.
                    SetSpout(pitcher, spout, Quaternion.identity, center + Vector3.up * .35f);
                    yield return Frames(3);
                    if (Get<bool>(pour, "IsHittingSurface")) throw new Exception("Idle unexpectedly hits surface");

                    // Tilt into pour and hold at center to build foam using real Update().
                    SetSpout(pitcher, spout, pourRotation, center + Vector3.up * low);
                    yield return Frames(4);
                    if (!Get<bool>(pour, "IsHittingSurface")) throw new Exception("Tilted stream does not hit surface");

                    for (int frame = 0; frame < 85; frame++) yield return null;
                    if (!Get<bool>(heart, "HasFoamCenter")) throw new Exception("Foam center was not recorded");
                    if (!Get<bool>(heart, "IsHeartFoamStarted")) throw new Exception("Foam did not reach heart state");

                    // Raise pitcher and pull outward, retaining tilt and surface contact.
                    Vector3 start = center + Vector3.up * high;
                    SetSpout(pitcher, spout, pourRotation, start);
                    yield return Frames(3);
                    for (int frame = 1; frame <= 25; frame++)
                    {
                        float t = frame / 25f;
                        SetSpout(pitcher, spout, pourRotation, start + Vector3.forward * (travel * t));
                        yield return null;
                    }
                    if (!Get<bool>(heart, "HeartFinishDetected"))
                        throw new Exception("Heart finish was not detected after pull-through");

                    SetSpout(pitcher, spout, Quaternion.identity, center + Vector3.up * .35f);
                    yield return Frames(3);
                    if (Get<bool>(pour, "IsHittingSurface")) throw new Exception("Stream still hits after upright stop");

                    byte[] png = Capture(drawing);
                    if (png.Length < 256) throw new Exception("PNG output unexpectedly small");
                    string path = Path.Combine(output, "Cycle_" + cycle.ToString("D2") + ".png");
                    File.WriteAllBytes(path, png);
                    if (!File.Exists(path) || new FileInfo(path).Length != png.Length)
                        throw new Exception("PNG file not saved");

                    Call(painter, "ClearDrawing");
                    yield return null;
                    if (Get<bool>(heart, "HasFoamCenter") || Get<bool>(heart, "HeartFinishDetected"))
                        throw new Exception("Reset did not clear heart state");
                }
                passed++;
                Debug.Log("[Full Cycle " + cycle + "/10] PASS: idle > pour > foam > finish > stop > PNG > reset");
            }
        }
        finally
        {
            pitcher.position = initialPosition;
            pitcher.rotation = initialRotation;
            Call(painter, "ClearDrawing");
        }
        Debug.Log("[Full Cycle] " + passed + "/10; PNG output: " + output);
        Assert.AreEqual(Cycles, passed, "Ten-cycle end-to-end verification failed");
    }
}
