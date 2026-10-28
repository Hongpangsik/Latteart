using UnityEngine;

public class LatteDotPainter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour inputSource;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private float dotInterval = 0.05f;

    private ILatteInput latteInput;
    private float timer;

    private void Awake()
    {
        latteInput = inputSource as ILatteInput;

        if (latteInput == null)
        {
            Debug.LogError("Input Source must implement ILatteInput.");
        }
    }

    private void Update()
    {
        if (latteInput == null) return;

        if (latteInput.IsPouring())
        {
            timer += Time.deltaTime;

            if (timer >= dotInterval)
            {
                SpawnDot(latteInput.GetPourPosition());
                timer = 0f;
            }
        }
        else
        {
            timer = dotInterval;
        }
    }

    private void SpawnDot(Vector3 position)
    {
        Vector3 spawnPosition = position + Vector3.up * 0.005f;
        Instantiate(dotPrefab, spawnPosition, Quaternion.identity);
    }
}