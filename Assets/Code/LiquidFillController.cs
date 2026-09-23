using UnityEngine;

public class LiquidFillController : MonoBehaviour
{
    [Header("Liquid")]
    [SerializeField] private Transform coffeeBase;
    [SerializeField] private Transform latteSurface;

    [Header("Fill Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fillAmount = 0.2f;

    [SerializeField] private float bottomY = 0.05f;
    [SerializeField] private float maxHeight = 0.9f;

    private void Update()
    {
        UpdateLiquid();
        KeepSurfaceLevel();
    }

    private void KeepSurfaceLevel()
    {
        if (latteSurface == null)
            return;

        // 컵이 회전해도 액체 표면은 월드 기준 수평 유지
        latteSurface.rotation = Quaternion.identity;
    }

    private void UpdateLiquid()
    {
        if (coffeeBase == null)
            return;

        // 현재 액체 높이
        float height = Mathf.Lerp(0.01f, maxHeight, fillAmount);

        // CoffeeBase 높이 변경
        Vector3 scale = coffeeBase.localScale;
        scale.y = height / 2f;
        coffeeBase.localScale = scale;

        // CoffeeBase가 아래에서 위로 차오르도록 위치 조정
        Vector3 position = coffeeBase.localPosition;
        position.y = bottomY + height / 2f;
        coffeeBase.localPosition = position;

        // LatteSurface를 액체의 가장 위쪽으로 이동
        if (latteSurface != null)
        {
            Vector3 surfacePosition = latteSurface.localPosition;
            surfacePosition.y = bottomY + height;
            latteSurface.localPosition = surfacePosition;
        }
    }
}