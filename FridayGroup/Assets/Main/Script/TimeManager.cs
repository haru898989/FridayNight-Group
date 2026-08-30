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
    [SerializeField] private TimerRingGraphic timerGauge;
    public GameObject timeUpText;

    [Header("遷移先シーン")]
    public string nextSceneName = "Result";

    private float currentTime;
    private bool isTimeUp = false;
    private bool isTimeUpRequested;
    private OnlineStageFlow stageFlow;

    void Start()
    {
        stageFlow = OnlineStageFlow.Instance;
        if (stageFlow != null)
        {
            stageFlow.StageTimeUp += OnStageTimeUp;
        }

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

        UpdateTimerUI();
    }

    private void OnDestroy()
    {
        if (stageFlow != null)
        {
            stageFlow.StageTimeUp -= OnStageTimeUp;
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
                RequestTimeUp();
            }
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int totalSeconds = Mathf.CeilToInt(currentTime);

        if (timerText != null)
        {
            timerText.text = totalSeconds.ToString();
        }

        if (timerGauge != null)
        {
            timerGauge.FillAmount = timeLimit > 0f
                ? currentTime / timeLimit
                : 0f;
        }
    }

    private void RequestTimeUp()
    {
        if (isTimeUpRequested)
        {
            return;
        }

        isTimeUpRequested = true;

        if (stageFlow != null && stageFlow.IsConnected)
        {
            stageFlow.RequestStageTimeUp();
            return;
        }

        OnStageTimeUp();
    }

    private void OnStageTimeUp()
    {
        if (isTimeUp)
        {
            return;
        }

        isTimeUp = true;
        isTimeUpRequested = true;
        currentTime = 0f;
        UpdateTimerUI();
        TimeUp();
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
