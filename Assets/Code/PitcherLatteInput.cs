
using UnityEngine;

public class PitcherLatteInput : MonoBehaviour, ILatteInput
{
    [Header("Milk Pour Controller")]
    [SerializeField] private MilkPourController milkPourController;

    // 우유가 수면에 닿은 월드 좌표 반환
    public Vector3 GetPourPosition()
    {
        if (milkPourController == null)
            return Vector3.zero;

        return milkPourController.CurrentSurfacePoint;
    }

    // 실제로 컵 안에 우유를 붓고 있는지 확인
    public bool IsPouring()
    {
        return milkPourController != null &&
               milkPourController.IsHittingSurface;
    }
}
