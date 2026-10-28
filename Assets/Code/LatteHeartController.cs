
using UnityEngine;

public class LatteHeartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LatteArtPainter latteArtPainter;
    [SerializeField] private MilkPourController milkPourController;
    [SerializeField] private LatteSurfaceMesh latteSurfaceMesh;

    [Header("Foam Tracking")]
    [SerializeField] private bool enableFoamTracking = true;

    [SerializeField, Range(0f, 1f)]
    private float foamCenterLockThreshold = 0.3f;

    [SerializeField] private float centerFollowSpeed = 3f;

    private Vector2 foamCenterUV;
    private float foamRadiusUV;

    private bool hasFoamCenter;
    private bool foamCenterLocked;

    public Vector2 FoamCenterUV => foamCenterUV;
    public float FoamRadiusUV => foamRadiusUV;
    public bool HasFoamCenter => hasFoamCenter;

    [Header("Heart Finish Detection")]
    [SerializeField] private Transform pourPoint;

    [SerializeField] private float finishHeight = 0.12f;
    [SerializeField] private float minFinishDistance = 0.08f;
    [SerializeField] private float minFoamRadius = 0.08f;
    [SerializeField] private float minStartDistanceFromCenter = 0.06f;
    [SerializeField] private float minEndDistanceFromCenter = 0.06f;

    // 거품 중심에서 허용할 최대 거리 (UV)
    [SerializeField] private float centerPassTolerance = 0.04f;

    private bool isFinishing;
    private bool heartFinishDetected;
    private bool passedFoamCenter;

    private Vector2 finishStartUV;
    private Vector2 previousFinishUV;

    public bool HeartFinishDetected => heartFinishDetected;

    public bool IsHeartFoamActive =>
    enableFoamTracking &&
    isHeartFoamStarted &&
    !isFinishing &&
    !heartFinishDetected;

    [Header("Heart Foam State")]
    [SerializeField] private float minHeartExpansion = 0.3f;

    private bool isHeartFoamStarted;

    public bool IsHeartFoamStarted => isHeartFoamStarted;
    
    [Header("Foam Session")]
    private bool isFoamSessionActive;

    public bool IsFoamSessionActive => isFoamSessionActive;


    // LatteArtPainter가 현재 거품 정보를 전달하는 함수
    public void UpdateFoam(
        Vector2 currentUV,
        float currentBrushSize,
        float expansionAmount)
    {
        if (!enableFoamTracking)
            return;
        if (!isFoamSessionActive)
        {
            isFoamSessionActive = true;
            Debug.Log("거품 구간 시작");
        }

        if (!hasFoamCenter)
        {
            foamCenterUV = currentUV;
            foamRadiusUV = currentBrushSize * 0.5f;

            hasFoamCenter = true;
            foamCenterLocked = false;

            Debug.Log($"거품 중심 기록 시작: {foamCenterUV}");
        }

        if (!foamCenterLocked)
        {
            foamCenterUV = Vector2.Lerp(
                foamCenterUV,
                currentUV,
                Time.deltaTime * centerFollowSpeed
            );

            if (expansionAmount >= foamCenterLockThreshold)
            {
                foamCenterLocked = true;
                Debug.Log($"거품 중심 고정: {foamCenterUV}");
            }
            if (!isHeartFoamStarted &&
    expansionAmount >= minHeartExpansion)
            {
                isHeartFoamStarted = true;

                Debug.Log("하트 거품 형성 시작");
            }
        }

        foamRadiusUV = Mathf.Max(
            foamRadiusUV,
            currentBrushSize * 0.5f
        );
        
    }

    public void ResetFoam()
    {
        hasFoamCenter = false;
        foamCenterLocked = false;
        foamRadiusUV = 0f;

        isFinishing = false;
        heartFinishDetected = false;
        passedFoamCenter = false;
        isHeartFoamStarted = false;
        isFoamSessionActive = false;
    }
    private void DetectHeartFinish(
    Vector2 currentUV,
    Vector3 surfacePoint)
    {
        if (heartFinishDetected || !hasFoamCenter)
            return;

        if (pourPoint == null)
            return;

        if (foamRadiusUV < minFoamRadius)
            return;

        float height = Mathf.Max(
            0f,
            pourPoint.position.y - surfacePoint.y
        );

        // 피처가 낮아지면 마무리 동작 후보 초기화
        if (height < finishHeight)
        {
            isFinishing = false;
            passedFoamCenter = false;
            return;
        }

        if (!isFinishing)
        {
            isFinishing = true;
            passedFoamCenter = false;

            finishStartUV = currentUV;
            previousFinishUV = currentUV;
            return;
        }

        // 이전 위치와 현재 위치를 잇는 선분에서
        // 거품 중심까지의 최단 거리 계산
        Vector2 segment = currentUV - previousFinishUV;

        float segmentLengthSq = segment.sqrMagnitude;

        if (segmentLengthSq > 0.000001f)
        {
            float t = Mathf.Clamp01(
                Vector2.Dot(
                    foamCenterUV - previousFinishUV,
                    segment
                ) / segmentLengthSq
            );

            Vector2 closestPoint =
                previousFinishUV + segment * t;

            float centerDistance = Vector2.Distance(
                closestPoint,
                foamCenterUV
            );

            if (centerDistance <= centerPassTolerance)
            {
                passedFoamCenter = true;
            }
        }

        float movedDistance = Vector2.Distance(
            finishStartUV,
            currentUV
        );

        float startDistance = Vector2.Distance(
    finishStartUV,
    foamCenterUV
);

        float endDistance = Vector2.Distance(
            currentUV,
            foamCenterUV
        );

        Vector2 startDirection =
            finishStartUV - foamCenterUV;

        Vector2 endDirection =
            currentUV - foamCenterUV;

        // 중심을 기준으로 서로 반대편인지 확인
        bool crossedToOppositeSide =
            Vector2.Dot(startDirection, endDirection) < 0f;
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log(
                $"[하트 마무리 진단] " +
                $"중심통과: {passedFoamCenter} | " +
                $"이동거리: {movedDistance:F3}/{minFinishDistance:F3} | " +
                $"시작거리: {startDistance:F3}/{minStartDistanceFromCenter:F3} | " +
                $"끝거리: {endDistance:F3}/{minEndDistanceFromCenter:F3} | " +
                $"반대편: {crossedToOppositeSide}"
            );
        }

        // 거품 중심 근처에서 시작했는지
        bool startedNearCenter =
            startDistance <= centerPassTolerance;

        // 기존 방식: 거품 한쪽에서 반대쪽으로 통과
        bool crossedThroughCenter =
            passedFoamCenter &&
            startDistance >= minStartDistanceFromCenter &&
            endDistance >= minEndDistanceFromCenter &&
            crossedToOppositeSide;

        // 추가 방식: 중심에서 시작해 바깥쪽으로 이동
        bool pulledOutFromCenter =
            startedNearCenter &&
            movedDistance >= minFinishDistance &&
            endDistance >= minEndDistanceFromCenter;

        // 두 방식 중 하나를 만족하면 마무리 감지
        if (crossedThroughCenter || pulledOutFromCenter)
        {
            heartFinishDetected = true;

            Debug.Log(
                pulledOutFromCenter
                    ? "하트 마무리 감지! (중심에서 바깥쪽으로 이동)"
                    : "하트 마무리 감지! (중심 통과)"
            );
        }

        previousFinishUV = currentUV;
    }
    public void EndFoamSession()
    {
        if (!isFoamSessionActive)
            return;

        isFoamSessionActive = false;

        Debug.Log("거품 구간 종료");
    }
    public void TrackFinishMotion(Vector2 currentUV, Vector3 surfacePoint)
    {
        if (!enableFoamTracking || !hasFoamCenter)
            return;



        DetectHeartFinish(currentUV, surfacePoint);
    }
}
