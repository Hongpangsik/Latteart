using UnityEngine;

public class LiquidFillController : MonoBehaviour
{
    [Header("Liquid")]
    [SerializeField] private Renderer coffeeRenderer;
    [SerializeField] private Transform latteSurface;
    [SerializeField] private Renderer latteSurfaceRenderer;

    // 컵의 위치와 기울기 기준
    [SerializeField] private Transform cupTransform;

    [Header("Fill Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fillAmount = 0.2f;

    [SerializeField] private float bottomY = 0f;
    [SerializeField] private float topY = 0.8f;

    private Material liquidMaterial;

    [Header("Milk Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float milkRatio = 0f;

    [SerializeField] private Color espressoColor = new Color(0.20f, 0.07f, 0.025f, 1f);
    [SerializeField] private Color latteColor = new Color(0.65f, 0.40f, 0.22f, 1f);

    [Header("Liquid Amount")]
    [SerializeField] private float espressoAmount = 30f;
    [SerializeField] private float milkAmount = 0f;
    [SerializeField] private float cupCapacity = 250f;

    private void Start()
    {
        if (coffeeRenderer != null)
        {
            liquidMaterial = coffeeRenderer.material;
        }
    }

    private void Update()
    {
        UpdateLiquid();
    }
    public void AddMilk(float amount)
    {
        if (amount <= 0f)
            return;

        milkAmount += amount;

        // 컵 용량 이상으로 넘치지 않도록 제한
        float maxMilkAmount = Mathf.Max(0f, cupCapacity - espressoAmount);
        milkAmount = Mathf.Clamp(milkAmount, 0f, maxMilkAmount);
    }
    private void UpdateLiquid()
    {

        if (liquidMaterial == null)
            return;

        // 현재 전체 액체량
        float totalAmount = espressoAmount + milkAmount;

        // 컵 용량을 기준으로 현재 채워진 비율 계산
        fillAmount = Mathf.Clamp01(totalAmount / cupCapacity);

        // 전체 액체 중 우유가 차지하는 비율
        milkRatio = totalAmount > 0f
            ? Mathf.Clamp01(milkAmount / totalAmount)
            : 0f;

        float localHeight = Mathf.Lerp(bottomY, topY, fillAmount);

        // CoffeeBase의 로컬 높이를 월드 높이로 변환
        Vector3 localPoint = new Vector3(0f, localHeight, 0f);
        Vector3 worldPoint = coffeeRenderer.transform.TransformPoint(localPoint);

        float worldFillHeight = worldPoint.y;
        
        // Shader의 Fill Height 변경
        liquidMaterial.SetFloat("_FillHeight", worldFillHeight);

        // 우유 비율에 따라 에스프레소 → 라떼 색으로 변화
        Color currentColor = Color.Lerp(
            espressoColor,
            latteColor,
            milkRatio
        );

        liquidMaterial.SetColor("_LiquidColor", currentColor);
        if (latteSurfaceRenderer != null)
        {
            latteSurfaceRenderer.material.SetColor("_LiquidColor", currentColor);
        }
        if (latteSurfaceRenderer != null && cupTransform != null)
        {
            Material surfaceMaterial = latteSurfaceRenderer.material;

            surfaceMaterial.SetVector("_CupCenter", cupTransform.position);
            surfaceMaterial.SetVector("_CupUp", cupTransform.up);

            surfaceMaterial.SetFloat("_BottomRadius", 0.4f);
            surfaceMaterial.SetFloat("_TopRadius", 0.65f);
        }

        // LatteSurface 위치 이동
        if (latteSurface != null)
        {
            // 현재 액체 높이로 이동
            Vector3 surfacePosition = latteSurface.position;
            surfacePosition.y = worldFillHeight;
            latteSurface.position = surfacePosition;

            // 수면은 항상 월드 기준 수평
            latteSurface.rotation = Quaternion.identity;
           
        }
    }
}