using System.Collections.Generic;
using UnityEngine;

public class LatteLineDrawer : MonoBehaviour
{
    [SerializeField]
    private LatteDrawingManager drawingManager;

    private LatteStrokeData currentStroke;
    [SerializeField] private MonoBehaviour inputSource;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float minPointDistance = 0.01f;
    [SerializeField] private float surfaceOffset = 0.005f;

    private ILatteInput latteInput;
    private LineRenderer currentLine;
    private readonly List<Vector3> points = new();
    private readonly List<GameObject> lineObjects = new();

    private void Awake()
    {
        latteInput = inputSource as ILatteInput;

        if (latteInput == null)
        {
            Debug.LogError("Input Source must implement ILatteInput.");
        }
    }

    private void Update()
    {
        if (latteInput == null) return;

        if (latteInput.IsPouring())
        {
            Vector3 point = latteInput.GetPourPosition() + Vector3.up * surfaceOffset;

            if (currentLine == null)
            {
                StartNewLine(point);
            }
            else
            {
                TryAddPoint(point);
            }
        }
        else
        {
            if (currentStroke != null)
            {
                drawingManager.AddStroke(currentStroke);
                currentStroke = null;
            }

            currentLine = null;
            points.Clear();
        }
    }

    private void StartNewLine(Vector3 startPoint)
    {
        GameObject lineObject = new GameObject("LatteLine");
        lineObjects.Add(lineObject);

        currentLine = lineObject.AddComponent<LineRenderer>();

        currentLine.material = lineMaterial;
        currentLine.widthMultiplier = lineWidth;

        currentLine.positionCount = 0;
        currentLine.useWorldSpace = true;

        currentLine.numCapVertices = 8;
        currentLine.numCornerVertices = 8;

        points.Clear();

        currentStroke = new LatteStrokeData();

        AddPoint(startPoint);
    }

    private void TryAddPoint(Vector3 point)
    {
        if (points.Count == 0)
        {
            AddPoint(point);
            return;
        }

        float distance = Vector3.Distance(points[^1], point);

        if (distance >= minPointDistance)
        {
            AddPoint(point);
        }
    }

    private void AddPoint(Vector3 point)
    {
        points.Add(point);

        currentStroke.points.Add(point);

        currentLine.positionCount = points.Count;
        currentLine.SetPosition(points.Count - 1, point);
    }
    public void ClearLines()
    {
        foreach (GameObject lineObject in lineObjects)
        {
            if (lineObject != null)
            {
                Destroy(lineObject);
            }
        }

        lineObjects.Clear();

        currentLine = null;
        currentStroke = null;
        points.Clear();
    }
}