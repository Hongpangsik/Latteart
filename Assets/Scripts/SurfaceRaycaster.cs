using UnityEngine;

public class SurfaceRaycaster : MonoBehaviour
{
    [Header("Input & Surface References")]
    [SerializeField] private MonoBehaviour latteInputBehaviour; // HandTrackingLogger 할당
    [SerializeField] private LayerMask surfaceLayer;            // LatteSurface 레이어
    [SerializeField] private PourPointRecorder strokeRecorder;   // 궤적 기록기

    [Header("Smoothing Settings")]
    [SerializeField] private float smoothFactor = 0.25f; // 2D 좌표 보정 강도 (0.1 ~ 0.3)

    private ILatteInput latteInput;
    private bool wasPouringLastFrame = false;
    private Vector2 smoothedSurfaceCoord;
    private bool isFirstPoint = true;

    private void Awake()
    {
        latteInput = latteInputBehaviour as ILatteInput;
        if (latteInput == null)
        {
            Debug.LogError("latteInputBehaviour가 ILatteInput 인터페이스를 구현하지 않았습니다!");
        }
    }

    private void Update()
    {
        if (latteInput == null) return;

        LatteHandFrameData frameData = latteInput.GetHandFrameData();

        // 손 추적 중 + isPouring == true 일 때 컵 수면으로 레이캐스트 실행
        if (frameData.isTracked && frameData.isPouring)
        {
            CastRayToSurface(frameData.indexTipPosition);
            wasPouringLastFrame = true;
        }
        else if (wasPouringLastFrame)
        {
            // 붓기가 중단되면 궤적을 마감하고 보정 위치 초기화
            if (strokeRecorder != null) strokeRecorder.EndStroke();
            isFirstPoint = true;
            wasPouringLastFrame = false;
        }
    }

    private void CastRayToSurface(Vector3 originPos)
    {
        // 1. 손위치에서 수직 아래로 Ray 투사
        Ray ray = new Ray(originPos, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2.0f, surfaceLayer))
        {
            // 2. 월드 3D 좌표 -> 가상 컵 수면 기준 2D UV 좌표(SurfaceCoordinate)로 변환
            Vector2 rawSurfaceCoord = hit.textureCoord;

            // 3. 2D 좌표 단계에서 손 떨림/프레임 튀김 방지 보정 적용
            Vector2 smoothedCoord = ApplySmoothing(rawSurfaceCoord);

            // 4. 시계열 궤적 기록기로 전달
            if (strokeRecorder != null)
            {
                strokeRecorder.RecordPoint(smoothedCoord);
            }
        }
    }

    // 선형 보간(Lerp)을 통한 좌표 보정 로직
    private Vector2 ApplySmoothing(Vector2 rawCoord)
    {
        if (isFirstPoint)
        {
            smoothedSurfaceCoord = rawCoord;
            isFirstPoint = false;
        }
        else
        {
            smoothedSurfaceCoord = Vector2.Lerp(smoothedSurfaceCoord, rawCoord, smoothFactor);
        }
        return smoothedSurfaceCoord;
    }
}