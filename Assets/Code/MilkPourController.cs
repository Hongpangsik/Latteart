using UnityEngine;

public class MilkPourController : MonoBehaviour
{
    [Header("Pour Point")]
    [SerializeField] private Transform pourPoint;

    [Header("Pour Settings")]
    [SerializeField] private float pourStartAngle = 45f;

    [Header("Debug")]
    [SerializeField] private float currentTiltAngle;
    [SerializeField] private bool spoutIsDown;
    [SerializeField] private bool isPouring;
    
    [Header("Milk Stream")]
    [SerializeField] private LineRenderer milkStream;
    [SerializeField] private float streamLength = 2f;
    [SerializeField] private float minStreamWidth = 0.015f;
    [SerializeField] private float maxStreamWidth = 0.045f;

    [Header("Latte Surface")]
    [SerializeField] private Transform latteSurface;
    [SerializeField] private LatteSurfaceMesh latteSurfaceMesh;

    [Header("Liquid Fill")]
    [SerializeField] private LiquidFillController liquidFillController;
    [SerializeField] private float minPourRate = 10f;
    [SerializeField] private float maxPourRate = 50f;
    [SerializeField] private float maxPourAngle = 90f;

    private void Update()
    {
        if (pourPoint == null)
            return;

        // 피처가 얼마나 기울어졌는지
        currentTiltAngle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        // 피처 중심보다 PourPoint가 아래에 있는지 확인
        spoutIsDown = pourPoint.position.y < transform.position.y;

        // 충분히 기울었고 + 주둥이가 아래쪽일 때만 붓기
        bool shouldPour =
            currentTiltAngle >= pourStartAngle &&
            spoutIsDown;

        if (shouldPour != isPouring)
        {
            isPouring = shouldPour;

            if (isPouring)
                Debug.Log("우유 붓기 시작");
            else
                Debug.Log("우유 붓기 종료");
        }
        UpdateMilkStream();
    }
    private void UpdateMilkStream()
    {
        if (milkStream == null || pourPoint == null)
            return;

        if (!isPouring)
        {
            milkStream.enabled = false;
            return;
        }

        milkStream.enabled = true;
        milkStream.positionCount = 2;

        Vector3 startPoint = pourPoint.position;

        // 기본값: 그냥 아래로 떨어짐
        Vector3 endPoint =
            startPoint + Vector3.down * streamLength;

        // LatteSurface가 PourPoint보다 아래에 있을 때
        if (latteSurface != null && latteSurfaceMesh != null &&
            startPoint.y > latteSurface.position.y)
        {
            // 우유가 수직으로 떨어졌을 때 수면과 만나는 위치
            Vector3 surfacePoint = new Vector3(
                startPoint.x,
                latteSurface.position.y,
                startPoint.z
            );

            // 그 위치가 실제 수면 영역 안이라면
            bool isInside =
    latteSurfaceMesh.IsWorldPointInsideSurface(surfacePoint);

            if (isInside)
            {
                endPoint = surfacePoint;

                if (liquidFillController != null)
                {
                    float pourStrength = Mathf.InverseLerp(
                pourStartAngle,
                 maxPourAngle,
                  currentTiltAngle
                );

                    float currentPourRate = Mathf.Lerp(
                        minPourRate,
                        maxPourRate,
                        pourStrength
                    );

                    liquidFillController.AddMilk(
                        currentPourRate * Time.deltaTime
                    );
                }
            }
            else
            {
                Debug.Log(
                    $"우유 적중 실패 | " +
                    $"Pour XZ: {surfacePoint.x:F3}, {surfacePoint.z:F3} | " +
                    $"Surface Y: {surfacePoint.y:F3}"
                );
            }
        }

        milkStream.SetPosition(0, startPoint);
        milkStream.SetPosition(1, endPoint);

        float streamStrength = Mathf.InverseLerp(
    pourStartAngle,
    maxPourAngle,
    currentTiltAngle
);

        float currentStreamWidth = Mathf.Lerp(
            minStreamWidth,
            maxStreamWidth,
            streamStrength
        );

        milkStream.startWidth = currentStreamWidth;
        milkStream.endWidth = currentStreamWidth * 0.7f;
    }
}