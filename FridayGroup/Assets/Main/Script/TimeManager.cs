using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("制限時間（秒）")]
    public float timeLimit = 180f;

    [Header("タイマー表示")]
    public TMP_Text timerText;

    private float currentTime;

    void Start()
    {
        currentTime = timeLimit;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                Debug.Log("時間切れ！");
            }
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}