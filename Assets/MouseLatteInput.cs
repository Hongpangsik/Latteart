using UnityEngine;

public class MouseLatteInput : MonoBehaviour, ILatteInput
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask cupSurfaceLayer;

    private Vector3 currentPourPosition;
    private bool hasValidPosition;

    public Vector3 GetPourPosition()
    {
        return currentPourPosition;
    }

    public bool IsPouring()
    {
        return Input.GetMouseButton(0) && hasValidPosition;
    }

    private void Update()
    {
        UpdateMousePosition();
    }

    private void UpdateMousePosition()
    {
        hasValidPosition = false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, cupSurfaceLayer))
        {
            currentPourPosition = hit.point;
            hasValidPosition = true;
        }
    }
}