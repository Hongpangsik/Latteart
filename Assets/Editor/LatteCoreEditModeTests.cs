using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

// EditMode tests: 6 test cases, 50 independent repetitions per case.
// Each repetition uses one dataset, cycling through all datasets.
// Console reports successes/failures and the success rate.
// A case passes if >= 48/50 repetitions pass (>= 95%).
public class LatteCoreEditModeTests
{
    private const int Repetitions = 50;
    private const float RequiredRate = 95f;
    private const float Tolerance = 0.0001f;

    private GameObject root;
    private LatteSurfaceMesh surface;
    private LatteHeartController heart;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("LatteTestRoot");
        var cup = new GameObject("CupFrustum");
        cup.transform.SetParent(root.transform);
        var surfaceObject = new GameObject("Surface");
        surfaceObject.transform.SetParent(root.transform);
        surfaceObject.AddComponent<MeshFilter>();
        surface = surfaceObject.AddComponent<LatteSurfaceMesh>();
        SetPrivate(surface, "frustumTransform", cup.transform);
        heart = root.AddComponent<LatteHeartController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
    }

    private static void SetPrivate(object instance, string field, object value)
    {
        var f = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(f, "Field not found: " + field);
        f.SetValue(instance, value);
    }

    private static bool Near(float actual, float expected)
    {
        return Mathf.Abs(actual - expected) <= Tolerance;
    }

    // Executes all 50 repetitions, including after individual failures.
    // Failures are aggregated instead of aborting on the first assertion.
    private static void RunRepeated(string id, int datasetCount, Func<int, bool> check)
    {
        int passed = 0;
        var datasetPassed = new int[datasetCount];
        var datasetTotal = new int[datasetCount];
        var failures = new StringBuilder();

        for (int iteration = 0; iteration < Repetitions; iteration++)
        {
            int dataIndex = iteration % datasetCount;
            datasetTotal[dataIndex]++;
            bool ok = false;
            try
            {
                ok = check(dataIndex);
            }
            catch (Exception ex)
            {
                if (failures.Length < 1800)
                    failures.Append(" repetition ").Append(iteration + 1)
                        .Append(" (D").Append(dataIndex + 1).Append("): ")
                        .Append(ex.GetType().Name).Append(": ").Append(ex.Message).Append("; ");
            }

            if (ok)
            {
                passed++;
                datasetPassed[dataIndex]++;
            }
            else if (failures.Length < 1800)
            {
                failures.Append(" repetition ").Append(iteration + 1)
                    .Append(" (D").Append(dataIndex + 1).Append(") failed; ");
            }
        }

        float rate = 100f * passed / Repetitions;
        var summary = new StringBuilder();
        summary.Append("[").Append(id).Append("] Success: ")
            .Append(passed).Append("/").Append(Repetitions)
            .Append(" (").Append(rate.ToString("F1")).Append("%), required >= ")
            .Append(RequiredRate.ToString("F1")).Append("%; per-data: ");
        for (int i = 0; i < datasetCount; i++)
            summary.Append("D").Append(i + 1).Append(":")
                .Append(datasetPassed[i]).Append("/").Append(datasetTotal[i]).Append(" ");
        Debug.Log(summary.ToString());

        Assert.That(rate, Is.GreaterThanOrEqualTo(RequiredRate),
            summary.ToString() + " failures: " + failures);
    }

    [Test]
    public void TC01_SurfaceCenter_MapsToHalfHalf()
    {
        float[] heights = { 0f, 0.2f, 0.4f };
        RunRepeated("TC-01", heights.Length, i =>
        {
            Vector2 uv = surface.WorldToSurfaceUV(new Vector3(0f, heights[i], 0f));
            return Near(uv.x, 0.5f) && Near(uv.y, 0.5f);
        });
    }

    [Test]
    public void TC02_SurfaceUV_MapsPositiveAndNegativeDirections()
    {
        Vector3[] inputs = {
            new Vector3( 0.325f, 0.4f,  0.325f),
            new Vector3(-0.325f, 0.4f, -0.325f),
            new Vector3( 0.325f, 0.4f,  0f),
            new Vector3(-0.325f, 0.4f,  0f),
            new Vector3( 0f,     0.4f,  0.325f),
            new Vector3( 0f,     0.4f, -0.325f)
        };
        Vector2[] expected = {
            new Vector2(0.75f, 0.75f),
            new Vector2(0.25f, 0.25f),
            new Vector2(0.75f, 0.50f),
            new Vector2(0.25f, 0.50f),
            new Vector2(0.50f, 0.75f),
            new Vector2(0.50f, 0.25f)
        };
        RunRepeated("TC-02", inputs.Length, i =>
        {
            Vector2 uv = surface.WorldToSurfaceUV(inputs[i]);
            return Near(uv.x, expected[i].x) && Near(uv.y, expected[i].y);
        });
    }

    [Test]
    public void TC03_SurfaceBoundary_InsideAndOutside()
    {
        // At y=0.4, radius = Lerp(0.4, 0.65, 0.5) = 0.525.
        Vector3[] inputs = {
            new Vector3(0f, 0.4f, 0f),
            new Vector3(1f, 0.4f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0.1f, 0.4f, 0f),
            new Vector3(-0.1f, 0.4f, 0f),
            new Vector3(0.524f, 0.4f, 0f),
            new Vector3(0.526f, 0.4f, 0f)
        };
        bool[] expected = { true, false, false, true, true, true, false };
        RunRepeated("TC-03", inputs.Length, i =>
            surface.IsWorldPointInsideSurface(inputs[i]) == expected[i]);
    }

    [Test]
    public void TC04_FoamUpdate_RecordsCenterAndRadius()
    {
        Vector2[] centers = {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.3f, 0.7f),
            new Vector2(0.7f, 0.3f),
            new Vector2(0.2f, 0.2f)
        };
        float[] brushSizes = { 0.1f, 0.2f, 0.3f, 0.4f };
        RunRepeated("TC-04", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], brushSizes[i], 0.4f);
            return heart.HasFoamCenter
                && heart.IsHeartFoamStarted
                && heart.IsFoamSessionActive
                && Near(heart.FoamRadiusUV, brushSizes[i] * 0.5f)
                && Vector2.Distance(heart.FoamCenterUV, centers[i]) <= Tolerance;
        });
    }

    [Test]
    public void TC05_ResetFoam_ClearsSessionAndFinishState()
    {
        Vector2[] centers = {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.3f, 0.7f),
            new Vector2(0.7f, 0.3f)
        };
        RunRepeated("TC-05", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], 0.1f + 0.05f * i, 0.4f);
            if (i == 2) heart.UpdateFoam(centers[i], 0.25f, 0.4f);
            heart.ResetFoam();
            return !heart.HasFoamCenter
                && !heart.IsHeartFoamStarted
                && !heart.IsFoamSessionActive
                && !heart.HeartFinishDetected
                && Near(heart.FoamRadiusUV, 0f);
        });
    }

    [Test]
    public void TC06_EndFoamSession_StopsSessionWithoutErasingCenter()
    {
        Vector2[] centers = {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.3f, 0.7f),
            new Vector2(0.7f, 0.3f)
        };
        RunRepeated("TC-06", centers.Length, i =>
        {
            heart.ResetFoam();
            heart.UpdateFoam(centers[i], 0.1f + 0.05f * i, 0.4f);
            if (i == 2) heart.UpdateFoam(centers[i], 0.25f, 0.4f);
            heart.EndFoamSession();
            return !heart.IsFoamSessionActive
                && heart.HasFoamCenter
                && Vector2.Distance(heart.FoamCenterUV, centers[i]) <= Tolerance;
        });
    }
}
