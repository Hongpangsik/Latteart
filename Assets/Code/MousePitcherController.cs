
using UnityEngine;

public class MousePitcherController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform pitcherModel;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float heightSpeed = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float tiltSpeed = 100f;
    [SerializeField] private Vector3 localTiltAxis = Vector3.forward;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool isDragging;
    private Vector3 lastMousePosition;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPitcher();
        }

        if (mainCamera == null)
            return;

        HandleMouseSelection();
        HandleMovement();
        HandleRotation();
    }

    private void HandleMouseSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform ||
                    hit.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void HandleMovement()
    {
        if (!isDragging)
            return;

        Vector3 mouseDelta =
            Input.mousePosition - lastMousePosition;

        Vector3 right = mainCamera.transform.right;
        Vector3 forward = mainCamera.transform.up;

        right.y = 0f;
        forward.y = 0f;

        right.Normalize();
        forward.Normalize();

        // 왼쪽 드래그: 수평 이동
        if (Input.GetMouseButton(0))
        {
            transform.position +=
                (right * mouseDelta.x +
                 forward * mouseDelta.y)
                * moveSpeed * 0.01f;
        }

        // 오른쪽 드래그: 높이 이동
        if (Input.GetMouseButton(1))
        {
            transform.position +=
                Vector3.up *
                mouseDelta.y *
                heightSpeed * 0.01f;
        }

        lastMousePosition = Input.mousePosition;
    }

    private void HandleRotation()
    {
        if (!isDragging)
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            transform.Rotate(
                localTiltAxis,
                scroll * tiltSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }

    private void ResetPitcher()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        isDragging = false;
    }
}
