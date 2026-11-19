using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// EditMode regression tests. Assets/Editor compiles into Assembly-CSharp-Editor,
// which can reference runtime Assembly-CSharp without an additional asmdef.
public class LatteCoreEditModeTests
{
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
        if (root != null)
            Object.DestroyImmediate(root);
    }

    private static void SetPrivate(object instance, string field, object value)
    {
        var f = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(f, "Field not found: " + field);
        f.SetValue(instance, value);
    }

    [Test]
    public void TC01_SurfaceCenter_MapsToHalfHalf()
    {
        Vector2 uv = surface.WorldToSurfaceUV(Vector3.zero);
        Assert.That(uv.x, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(uv.y, Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void TC02_SurfaceUV_MapsPositiveAndNegativeDirections()
    {
        Vector2 positive = surface.WorldToSurfaceUV(new Vector3(0.325f, 0.4f, 0.325f));
        Vector2 negative = surface.WorldToSurfaceUV(new Vector3(-0.325f, 0.4f, -0.325f));
        Assert.That(positive.x, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(positive.y, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(negative.x, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(negative.y, Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void TC03_SurfaceBoundary_InsideAndOutside()
    {
        Assert.IsTrue(surface.IsWorldPointInsideSurface(new Vector3(0f, 0.4f, 0f)));
        Assert.IsFalse(surface.IsWorldPointInsideSurface(new Vector3(1f, 0.4f, 0f)));
        Assert.IsFalse(surface.IsWorldPointInsideSurface(new Vector3(0f, 1f, 0f)));
    }

    [Test]
    public void TC04_FoamUpdate_RecordsCenterAndRadius()
    {
        heart.UpdateFoam(new Vector2(0.5f, 0.5f), 0.1f, 0.4f);
        Assert.IsTrue(heart.HasFoamCenter);
        Assert.IsTrue(heart.IsHeartFoamStarted);
        Assert.IsTrue(heart.IsFoamSessionActive);
        Assert.That(heart.FoamRadiusUV, Is.EqualTo(0.05f).Within(0.0001f));
    }

    [Test]
    public void TC05_ResetFoam_ClearsSessionAndFinishState()
    {
        heart.UpdateFoam(new Vector2(0.5f, 0.5f), 0.1f, 0.4f);
        heart.ResetFoam();
        Assert.IsFalse(heart.HasFoamCenter);
        Assert.IsFalse(heart.IsHeartFoamStarted);
        Assert.IsFalse(heart.IsFoamSessionActive);
        Assert.IsFalse(heart.HeartFinishDetected);
        Assert.That(heart.FoamRadiusUV, Is.EqualTo(0f));
    }

    [Test]
    public void TC06_EndFoamSession_StopsSessionWithoutErasingCenter()
    {
        heart.UpdateFoam(new Vector2(0.5f, 0.5f), 0.1f, 0.4f);
        heart.EndFoamSession();
        Assert.IsFalse(heart.IsFoamSessionActive);
        Assert.IsTrue(heart.HasFoamCenter);
    }
}
