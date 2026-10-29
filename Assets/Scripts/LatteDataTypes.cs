using UnityEngine;

[System.Serializable]
public struct LatteHandFrameData
{
    public Vector3 indexTipPosition;
    public float timestamp;
    public bool isTracked;
    public bool isPouring;

    public LatteHandFrameData(Vector3 pos, float time, bool tracked, bool pouring = false)
    {
        this.indexTipPosition = pos;
        this.timestamp = time;
        this.isTracked = tracked;
        this.isPouring = pouring;
    }
}

