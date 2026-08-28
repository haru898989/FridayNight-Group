using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        Debug.Log("=== TimeManager Start ===");

        Debug.Log(
            $"SelectedStageResourcePath = {StageSelectionContext.SelectedStageResourcePath}"
        );

        List<StageCatalogEntry> stages = StageCatalog.Load();

        Debug.Log($"StageCatalogのステージ数 = {stages.Count}");

        foreach (StageCatalogEntry s in stages)
        {
            Debug.Log(
                $"Stage: {s.stageFolder}, Path: {s.resourcePath}, Time: {s.timeLimit}"
            );
        }
        StageCatalogEntry stage = StageCatalog.Load()
        .Find(x => x.resourcePath == StageSelectionContext.SelectedStageResourcePath);

        if (stage != null)
        {
            SetTimeLimit(stage.timeLimit);
            Debug.Log($"ステージ {stage.stageFolder} の制限時間: {stage.timeLimit}秒");
        }
        else
        {
            currentTime = timeLimit;
            Debug.LogWarning("選択されたステージが見つからないため、デフォルトの制限時間を使用します。");
        }
    }
    public void SetTimeLimit(float time)
    {
        timeLimit = time;
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

        if (GoalPresentationUI.Instance != null)
        {
            GoalPresentationUI.Instance.HideAll();
        }

        Invoke(nameof(ChangeScene), 2f);
    }
    
    void ChangeScene()
    {
        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            // タイムオーバーはクリアではないため、接続を維持したまま
            // ホストから全員をステージ選択へ戻します。
            if (flow.IsSharedModeMasterClient)
            {
                flow.ReturnToStageSelect();
            }

            return;
        }

        SceneManager.LoadScene("Title");
    }
}
