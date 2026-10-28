using System.Collections.Generic;
using UnityEngine;

public class PourPointRecorder : MonoBehaviour
{
    [Header("Cup Boundary Settings")]
    [SerializeField] private Vector2 surfaceCenter = new Vector2(0.5f, 0.5f); // 2D UV 중심 좌표
    [SerializeField] private float validRadius = 0.5f;                       // 컵 원형 유효 반지름

    private List<Vector2> currentStroke = new List<Vector2>();

    // 1. 유효 범위 검증 및 시계열 좌표 저장
    public void RecordPoint(Vector2 uvCoord)
    {
        // 컵 중심점으로부터의 거리를 계산하여 유효 범위 내부인지 검증
        if (Vector2.Distance(uvCoord, surfaceCenter) <= validRadius)
        {
            currentStroke.Add(uvCoord);
            Debug.Log($"[Stroke Record] 궤적 데이터 누적 중: {uvCoord} (총 {currentStroke.Count}개 포인트)");
        }
    }

    // 2. 핀치가 끝났을 때 궤적(Stroke) 단위 마감
    public void EndStroke()
    {
        if (currentStroke.Count > 0)
        {
            Debug.Log($"[Stroke Complete] 총 {currentStroke.Count}개 포인트로 획 생성 완료");
            currentStroke.Clear();
        }
    }
}