using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Start()
    {
        SetScore(0f);
    }

    public void SetScore(float score)
    {
        scoreText.text = $"Score : {score:F1}";
    }
}