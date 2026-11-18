using UnityEngine;

/// <summary>
/// Input-independent UV motion metrics for PC mouse and XR Hands pouring.
/// Add to any scene object and assign the existing MilkPourController and LatteSurfaceMesh.
/// Does not modify painting or heart detection.
/// </summary>
[DefaultExecutionOrder(100)]
public class PourMotionAnalyzer : MonoBehaviour
{
    [SerializeField] private MilkPourController milkPourController;
    [SerializeField] private LatteSurfaceMesh latteSurfaceMesh;
    [SerializeField, Min(0.001f)] private float speedSmoothingTime = 0.08f;
    [SerializeField, Min(0f)] private float stationarySpeedThreshold = 0.04f;
    [SerializeField, Min(0f)] private float maxSampleGapSeconds = 0.2f;
    [SerializeField] private bool logSessionSummary = true;

    [Header("Live data (UV units)")]
    [SerializeField] private bool isSampling;
    [SerializeField] private Vector2 currentUV;
    [SerializeField] private Vector2 previousUV;
    [SerializeField] private Vector2 movementDirection;
    [SerializeField] private float speedUVPerSecond;
    [SerializeField] private float cumulativeDistanceUV;
    [SerializeField] private float stationaryTimeSeconds;
    [SerializeField] private float sessionDurationSeconds;
    [SerializeField] private float lateralDeviationUV;

    private float lastSampleTime;
    private float velocityFilter;
    private Vector2 sessionStartUV;
    private Vector2 previousDirection;
    private float cumulativeTurnRadians;
    private float maxLateralDeviationUV;
    private int directionReversals;

    public bool IsSampling => isSampling;
    public Vector2 CurrentUV => currentUV;
    public Vector2 PreviousUV => previousUV;
    public Vector2 MovementDirection => movementDirection;
    public float SpeedUVPerSecond => speedUVPerSecond;
    public float CumulativeDistanceUV => cumulativeDistanceUV;
    public float StationaryTimeSeconds => stationaryTimeSeconds;
    public float SessionDurationSeconds => sessionDurationSeconds;
    public float LateralDeviationUV => lateralDeviationUV;
    public float MaxLateralDeviationUV => maxLateralDeviationUV;
    public float CumulativeTurnRadians => cumulativeTurnRadians;
    public int DirectionReversals => directionReversals;

    private void LateUpdate()
    {
        if (milkPourController == null || latteSurfaceMesh == null ||
            !milkPourController.IsHittingSurface)
        {
            EndSession();
            return;
        }

        Vector2 uv = latteSurfaceMesh.WorldToSurfaceUV(milkPourController.CurrentSurfacePoint);
        float now = Time.time;
        if (!isSampling)
        {
            BeginSession(uv, now);
            return;
        }

        float dt = now - lastSampleTime;
        if (dt <= Mathf.Epsilon) return;
        if (maxSampleGapSeconds > 0f && dt > maxSampleGapSeconds)
        {
            EndSession();
            BeginSession(uv, now);
            return;
        }

        previousUV = currentUV;
        currentUV = uv;
        Vector2 delta = currentUV - previousUV;
        float distance = delta.magnitude;
        float rawSpeed = distance / dt;
        float blend = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, speedSmoothingTime));
        speedUVPerSecond = Mathf.Lerp(speedUVPerSecond, rawSpeed, blend);
        movementDirection = distance > 0.00001f ? delta / distance : Vector2.zero;
        cumulativeDistanceUV += distance;
        sessionDurationSeconds += dt;
        stationaryTimeSeconds = speedUVPerSecond <= stationarySpeedThreshold
            ? stationaryTimeSeconds + dt : 0f;

        if (movementDirection.sqrMagnitude > 0.01f)
        {
            if (previousDirection.sqrMagnitude > 0.01f)
            {
                float signedAngle = Vector2.SignedAngle(previousDirection, movementDirection) * Mathf.Deg2Rad;
                cumulativeTurnRadians += Mathf.Abs(signedAngle);
                if (Vector2.Dot(previousDirection, movementDirection) < -0.25f)
                    directionReversals++;
            }
            previousDirection = movementDirection;
        }

        // Signed perpendicular deviation relative to the initial movement direction.
        Vector2 fromStart = currentUV - sessionStartUV;
        if (fromStart.sqrMagnitude > 0.000001f)
        {
            Vector2 axis = (currentUV - sessionStartUV).normalized;
            lateralDeviationUV = Vector2.Dot(fromStart, new Vector2(-axis.y, axis.x));
            maxLateralDeviationUV = Mathf.Max(maxLateralDeviationUV, Mathf.Abs(lateralDeviationUV));
        }
        lastSampleTime = now;
    }

    private void BeginSession(Vector2 uv, float now)
    {
        isSampling = true;
        currentUV = previousUV = sessionStartUV = uv;
        movementDirection = previousDirection = Vector2.zero;
        speedUVPerSecond = cumulativeDistanceUV = stationaryTimeSeconds = 0f;
        sessionDurationSeconds = cumulativeTurnRadians = 0f;
        lateralDeviationUV = maxLateralDeviationUV = 0f;
        directionReversals = 0;
        lastSampleTime = now;
    }

    private void EndSession()
    {
        if (!isSampling) return;
        if (logSessionSummary)
            Debug.Log($"PourMotionAnalyzer: duration={sessionDurationSeconds:F2}s, distance={cumulativeDistanceUV:F3} UV, reversals={directionReversals}, turn={cumulativeTurnRadians:F2} rad");
        isSampling = false;
        speedUVPerSecond = 0f;
        movementDirection = Vector2.zero;
    }

    public void ResetMetrics()
    {
        isSampling = false;
        speedUVPerSecond = cumulativeDistanceUV = stationaryTimeSeconds = 0f;
        sessionDurationSeconds = cumulativeTurnRadians = 0f;
        lateralDeviationUV = maxLateralDeviationUV = 0f;
        directionReversals = 0;
    }
}
