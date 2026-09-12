using UnityEngine;

public class LatteDrawingManager : MonoBehaviour
{
    [SerializeField] private LatteLineDrawer lineDrawer;

    public LatteDrawingData drawingData = new LatteDrawingData();

    public void AddStroke(LatteStrokeData stroke)
    {
        drawingData.strokes.Add(stroke);
        Debug.Log($"현재 Stroke 수 : {drawingData.strokes.Count}");
    }

    public void ClearDrawing()
    {
        drawingData.Clear();

        if (lineDrawer != null)
        {
            lineDrawer.ClearLines();
        }

        Debug.Log("그림 초기화 완료");
    }

    public void PrintDrawingData()
    {
        Debug.Log($"전체 Stroke 수: {drawingData.strokes.Count}");

        for (int i = 0; i < drawingData.strokes.Count; i++)
        {
            Debug.Log($"Stroke {i + 1}: 점 개수 {drawingData.strokes[i].points.Count}");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearDrawing();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintDrawingData();
        }
    }
}