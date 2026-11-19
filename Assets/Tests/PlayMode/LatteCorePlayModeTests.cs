using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// PlayMode Test Runner: loads the real LattePlay scene and checks its runtime components.
// Runtime scripts are accessed by reflection so the test assembly does not depend on Assembly-CSharp.
public class LatteCorePlayModeTests
{
    const int Repetitions = 50;
    const float Tolerance = 0.0001f;
    const float RequiredRate = 95f;

    static Component FindSceneComponent(string typeName)
    {
        foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component != null && component.GetType().Name == typeName &&
                component.gameObject.scene == SceneManager.GetActiveScene())
                return component;
        Assert.Fail("LattePlay scene component missing: " + typeName);
        return null;
    }

    static object Read(object obj, string name)
    {
        Type t = obj.GetType();
        while (t != null)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(obj);
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return p.GetValue(obj, null);
            t = t.BaseType;
        }
        throw new MissingMemberException(obj.GetType().Name, name);
    }

    static object Call(object obj, string method, params object[] args)
    {
        var m = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) throw new MissingMethodException(obj.GetType().Name, method);
        return m.Invoke(obj, args);
    }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= Tolerance; }
    static bool Flag(object obj, string property) { return (bool)Read(obj, property); }
    static float Number(object obj, string property) { return Convert.ToSingle(Read(obj, property)); }
    static Vector2 Point(object obj, string property) { return (Vector2)Read(obj, property); }

    static void Verify(string tc, int count, Func<int, bool> check)
    {
        int passed = 0;
        int[] totals = new int[count], successes = new int[count];
        var errors = new StringBuilder();
        for (int n = 0; n < Repetitions; n++)
        {
            int i = n % count;
            totals[i]++;
            bool ok = false;
            try { ok = check(i); }
            catch (Exception ex)
            {
                if (errors.Length < 1200) errors.Append(" #").Append(n + 1).Append(": ").Append(ex.GetBaseException().Message);
            }
            if (ok) { passed++; successes[i]++; }
        }
        float rate = passed * 100f / Repetitions;
        var result = new StringBuilder("[" + tc + " PlayMode] " + passed + "/50 (" + rate.ToString("F1") + "%), required >= 95%; ");
        for (int i = 0; i < count; i++)
            result.Append("D").Append(i + 1).Append(":").Append(successes[i]).Append("/").Append(totals[i]).Append(" ");
        Debug.Log(result.ToString());
        Assert.That(rate, Is.GreaterThanOrEqualTo(RequiredRate), result + " " + errors);
    }

    [UnitySetUp]
    public IEnumerator LoadLattePlay()
    {
        Assert.That(Application.CanStreamedLevelBeLoaded("LattePlay"), Is.True,
            "Add Assets/Scenes/LattePlay.unity to File > Build Profiles > Scene List.");
        var loading = SceneManager.LoadSceneAsync("LattePlay", LoadSceneMode.Single);
        Assert.NotNull(loading);
        yield return loading;
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LattePlay"));
    }

    [UnityTest]
    public IEnumerator TC01_SurfaceCenter_PlayMode()
    {
        var surface = FindSceneComponent("LatteSurfaceMesh");
        var frustum = (Transform)Read(surface, "frustumTransform");
        Assert.NotNull(frustum);
        float[] heights = { 0f, .2f, .4f };
        Verify("TC-01", heights.Length, i =>
        {
            var uv = (Vector2)Call(surface, "WorldToSurfaceUV", frustum.TransformPoint(new Vector3(0f, heights[i], 0f)));
            return Near(uv.x, .5f) && Near(uv.y, .5f);
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC02_SurfaceDirections_PlayMode()
    {
        var surface = FindSceneComponent("LatteSurfaceMesh");
        var frustum = (Transform)Read(surface, "frustumTransform");
        Assert.NotNull(frustum);
        float radius = Mathf.Max(Number(surface, "bottomRadius"), Number(surface, "topRadius"));
        Vector2[] offsets = {
            new Vector2(.5f,.5f), new Vector2(-.5f,-.5f),
            new Vector2(.5f,0f), new Vector2(-.5f,0f),
            new Vector2(0f,.5f), new Vector2(0f,-.5f)
        };
        Verify("TC-02", offsets.Length, i =>
        {
            Vector3 world = frustum.TransformPoint(new Vector3(offsets[i].x * radius, .4f, offsets[i].y * radius));
            var uv = (Vector2)Call(surface, "WorldToSurfaceUV", world);
            return Near(uv.x, .5f + offsets[i].x * .5f) && Near(uv.y, .5f + offsets[i].y * .5f);
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC03_SurfaceBoundary_PlayMode()
    {
        var surface = FindSceneComponent("LatteSurfaceMesh");
        var frustum = (Transform)Read(surface, "frustumTransform");
        Assert.NotNull(frustum);
        float h = Number(surface, "height");
        float r = Mathf.Lerp(Number(surface, "bottomRadius"), Number(surface, "topRadius"), .5f);
        Vector3[] points = {
            new Vector3(0f,h*.5f,0f), new Vector3(r+.1f,h*.5f,0f),
            new Vector3(0f,h+.2f,0f), new Vector3(.1f,h*.5f,0f),
            new Vector3(-.1f,h*.5f,0f), new Vector3(r-.001f,h*.5f,0f),
            new Vector3(r+.001f,h*.5f,0f)
        };
        bool[] expected = { true, false, false, true, true, true, false };
        Verify("TC-03", points.Length, i =>
            (bool)Call(surface, "IsWorldPointInsideSurface", frustum.TransformPoint(points[i])) == expected[i]);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC04_FoamUpdate_PlayMode()
    {
        var heart = FindSceneComponent("LatteHeartController");
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f), new Vector2(.2f,.2f) };
        float[] sizes = { .1f,.2f,.3f,.4f };
        Verify("TC-04", centers.Length, i =>
        {
            Call(heart, "ResetFoam");
            Call(heart, "UpdateFoam", centers[i], sizes[i], .4f);
            return Flag(heart,"HasFoamCenter") && Flag(heart,"IsHeartFoamStarted") &&
                Flag(heart,"IsFoamSessionActive") && Near(Number(heart,"FoamRadiusUV"), sizes[i]*.5f) &&
                Vector2.Distance(Point(heart,"FoamCenterUV"),centers[i]) <= Tolerance;
        });
        Call(heart,"ResetFoam");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC05_FoamReset_PlayMode()
    {
        var heart = FindSceneComponent("LatteHeartController");
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f) };
        Verify("TC-05", centers.Length, i =>
        {
            Call(heart,"ResetFoam");
            Call(heart,"UpdateFoam",centers[i],.1f+.05f*i,.4f);
            if (i==2) Call(heart,"UpdateFoam",centers[i],.25f,.4f);
            Call(heart,"ResetFoam");
            return !Flag(heart,"HasFoamCenter") && !Flag(heart,"IsHeartFoamStarted") &&
                !Flag(heart,"IsFoamSessionActive") && !Flag(heart,"HeartFinishDetected") &&
                Near(Number(heart,"FoamRadiusUV"),0f);
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC06_FoamSessionEnd_PlayMode()
    {
        var heart = FindSceneComponent("LatteHeartController");
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f) };
        Verify("TC-06", centers.Length, i =>
        {
            Call(heart,"ResetFoam");
            Call(heart,"UpdateFoam",centers[i],.1f+.05f*i,.4f);
            if (i==2) Call(heart,"UpdateFoam",centers[i],.25f,.4f);
            Call(heart,"EndFoamSession");
            return !Flag(heart,"IsFoamSessionActive") && Flag(heart,"HasFoamCenter") &&
                Vector2.Distance(Point(heart,"FoamCenterUV"),centers[i]) <= Tolerance;
        });
        Call(heart,"ResetFoam");
        yield return null;
    }
}
