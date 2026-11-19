using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Run from EditMode Test Runner. Each test enters actual Play Mode with LattePlay loaded.
// TC identifiers remain TC-01..TC-06; the _PlayMode suffix only distinguishes NUnit method names.
public class LatteCoreScenePlayModeTests
{
    private const string ScenePath = "Assets/Scenes/LattePlay.unity";
    private const int Repetitions = 50;
    private const float Tolerance = 0.0001f;
    private const float RequiredRate = 95f;

    private static T Field<T>(object target, string name)
    {
        var info = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(info, "Missing field: " + name);
        return (T)info.GetValue(target);
    }

    private static bool Near(float a, float b) { return Mathf.Abs(a - b) <= Tolerance; }

    private static void Verify(string tc, int count, Func<int, bool> check)
    {
        int passed = 0;
        int[] perTotal = new int[count];
        int[] perPass = new int[count];
        var errors = new StringBuilder();
        for (int n = 0; n < Repetitions; n++)
        {
            int i = n % count;
            perTotal[i]++;
            bool ok = false;
            try { ok = check(i); }
            catch (Exception e)
            {
                if (errors.Length < 1500) errors.Append(" #").Append(n + 1).Append(": ").Append(e.Message);
            }
            if (ok) { passed++; perPass[i]++; }
        }
        float rate = passed * 100f / Repetitions;
        var log = new StringBuilder("[" + tc + " PlayMode] " + passed + "/50 (" + rate.ToString("F1") + "%); ");
        for (int i = 0; i < count; i++)
            log.Append("D").Append(i + 1).Append(":").Append(perPass[i]).Append("/").Append(perTotal[i]).Append(" ");
        Debug.Log(log.ToString());
        Assert.That(rate, Is.GreaterThanOrEqualTo(RequiredRate), log + " " + errors);
    }

    private static LatteSurfaceMesh GetSurface()
    {
        var all = UnityEngine.Object.FindObjectsByType<LatteSurfaceMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in all)
            if (item.gameObject.scene == SceneManager.GetActiveScene() && Field<Transform>(item, "frustumTransform") != null)
                return item;
        Assert.Fail("LattePlay scene has no LatteSurfaceMesh with frustumTransform");
        return null;
    }

    private static LatteHeartController GetHeart()
    {
        var all = UnityEngine.Object.FindObjectsByType<LatteHeartController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in all)
            if (item.gameObject.scene == SceneManager.GetActiveScene()) return item;
        Assert.Fail("LattePlay scene has no LatteHeartController");
        return null;
    }

    private static void OpenSceneBeforePlay()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator TC01_SurfaceCenter_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var surface = GetSurface();
        var frustum = Field<Transform>(surface, "frustumTransform");
        float[] heights = { 0f, 0.2f, 0.4f };
        Verify("TC-01", heights.Length, i =>
        {
            var uv = surface.WorldToSurfaceUV(frustum.TransformPoint(new Vector3(0f, heights[i], 0f)));
            return Near(uv.x, 0.5f) && Near(uv.y, 0.5f);
        });
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TC02_SurfaceDirections_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var surface = GetSurface();
        var frustum = Field<Transform>(surface, "frustumTransform");
        float radius = Mathf.Max(Field<float>(surface, "bottomRadius"), Field<float>(surface, "topRadius"));
        Vector2[] offsets = {
            new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f),
            new Vector2(0.5f, 0f), new Vector2(-0.5f, 0f),
            new Vector2(0f, 0.5f), new Vector2(0f, -0.5f)
        };
        Verify("TC-02", offsets.Length, i =>
        {
            var p = frustum.TransformPoint(new Vector3(offsets[i].x * radius, 0.4f, offsets[i].y * radius));
            var uv = surface.WorldToSurfaceUV(p);
            return Near(uv.x, 0.5f + offsets[i].x * 0.5f) && Near(uv.y, 0.5f + offsets[i].y * 0.5f);
        });
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TC03_SurfaceBoundary_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var surface = GetSurface();
        var frustum = Field<Transform>(surface, "frustumTransform");
        float height = Field<float>(surface, "height");
        float radius = Mathf.Lerp(Field<float>(surface, "bottomRadius"), Field<float>(surface, "topRadius"), 0.5f);
        Vector3[] local = {
            new Vector3(0f, height * 0.5f, 0f),
            new Vector3(radius + 0.1f, height * 0.5f, 0f),
            new Vector3(0f, height + 0.2f, 0f),
            new Vector3(0.1f, height * 0.5f, 0f),
            new Vector3(-0.1f, height * 0.5f, 0f),
            new Vector3(radius - 0.001f, height * 0.5f, 0f),
            new Vector3(radius + 0.001f, height * 0.5f, 0f)
        };
        bool[] expected = { true, false, false, true, true, true, false };
        Verify("TC-03", local.Length, i => surface.IsWorldPointInsideSurface(frustum.TransformPoint(local[i])) == expected[i]);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TC04_FoamUpdate_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var heart = GetHeart();
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f), new Vector2(.2f,.2f) };
        float[] sizes = { .1f, .2f, .3f, .4f };
        Verify("TC-04", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], sizes[i], .4f);
            return heart.HasFoamCenter && heart.IsHeartFoamStarted && heart.IsFoamSessionActive
                && Near(heart.FoamRadiusUV, sizes[i] * .5f)
                && Vector2.Distance(heart.FoamCenterUV, centers[i]) <= Tolerance;
        });
        heart.ResetFoam();
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TC05_FoamReset_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var heart = GetHeart();
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f) };
        Verify("TC-05", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], .1f + .05f * i, .4f);
            if (i == 2) heart.UpdateFoam(centers[i], .25f, .4f);
            heart.ResetFoam();
            return !heart.HasFoamCenter && !heart.IsHeartFoamStarted && !heart.IsFoamSessionActive
                && !heart.HeartFinishDetected && Near(heart.FoamRadiusUV, 0f);
        });
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TC06_FoamSessionEnd_PlayMode()
    {
        OpenSceneBeforePlay();
        yield return new EnterPlayMode();
        yield return null;
        var heart = GetHeart();
        Vector2[] centers = { new Vector2(.5f,.5f), new Vector2(.3f,.7f), new Vector2(.7f,.3f) };
        Verify("TC-06", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], .1f + .05f * i, .4f);
            if (i == 2) heart.UpdateFoam(centers[i], .25f, .4f);
            heart.EndFoamSession();
            return !heart.IsFoamSessionActive && heart.HasFoamCenter
                && Vector2.Distance(heart.FoamCenterUV, centers[i]) <= Tolerance;
        });
        heart.ResetFoam();
        yield return new ExitPlayMode();
    }
}
