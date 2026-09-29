using System.Collections;
using UnityEngine;

public class CameraViewTransition : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform targetCamera;

    [Header("View Points")]
    [SerializeField] private Transform startView;
    [SerializeField] private Transform pourView;

    [Header("Transition")]
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private float startDelay = 0.5f;

    private void Start()
    {
        if (targetCamera == null || startView == null || pourView == null)
            return;

        // 게임 시작 시 StartView에서 시작
        targetCamera.position = startView.position;
        targetCamera.rotation = startView.rotation;

        StartCoroutine(MoveToPourView());
    }

    private IEnumerator MoveToPourView()
    {
        // 시작 화면을 잠깐 보여줌
        yield return new WaitForSeconds(startDelay);

        Vector3 startPosition = targetCamera.position;
        Quaternion startRotation = targetCamera.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);

            // 처음과 끝에서 부드럽게 감속/가속
            t = Mathf.SmoothStep(0f, 1f, t);

            targetCamera.position =
                Vector3.Lerp(startPosition, pourView.position, t);

            targetCamera.rotation =
                Quaternion.Slerp(startRotation, pourView.rotation, t);

            yield return null;
        }

        // 마지막 위치 정확하게 보정
        targetCamera.position = pourView.position;
        targetCamera.rotation = pourView.rotation;
    }
}