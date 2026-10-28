using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LatteDrawingData
{
    public List<LatteStrokeData> strokes = new List<LatteStrokeData>();

    public void Clear()
    {
        strokes.Clear();
    }
}