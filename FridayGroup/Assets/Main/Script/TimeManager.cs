using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    [Header("制限時間（秒）")]
    public float timeLimit = 180f;

    [Header("タイマー表示")]
    public TMP_Text timerText;
    public GameObject timeUpText;

    [Header("遷移先シーン")]
    public string nextSceneName = "Result";

    private float currentTime;
    private bool isTimeUp = false;

    void Start()
    {
        currentTime = timeLimit;
    }

    void Update()
    {
        if (isTimeUp) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                isTimeUp = true;
                TimeUp();
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

    void TimeUp()
    {
        Debug.Log("時間切れ");
        if (timeUpText != null)
        {
            timeUpText.SetActive(true);
        }

        PlayerBase[] players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);

        foreach (PlayerBase player in players)
        {
            player.canMove = false;
        }
        Invoke(nameof(ChangeScene), 2f);
    }
    
    void ChangeScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}