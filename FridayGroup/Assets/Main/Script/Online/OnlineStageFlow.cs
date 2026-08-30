using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージ選択にだけ必要なオンライン処理です。
/// 既存のPerfect_Onlineへ責務を追加しすぎないため、別コンポーネントに分離しています。
/// </summary>
public sealed class OnlineStageFlow : MonoBehaviour, INetworkRunnerCallbacks
{
    private const string OnlineConnectSceneName = "OnlineConnect";
    private const string StageSelectSceneName = "StageSelect";
    private const string MapSceneName = "Map";
    private const string StageSelectScenePath = "Assets/Main/Scene/StageSelect.unity";
    private const string MapScenePath = "Assets/Main/Scene/Map.unity";
    private const string ResultScenePath = "Assets/Main/Scene/Result.unity";
    private const string GameCompleteScenePath = "Assets/Main/Scene/GameComplete.unity";
    private const string TitleScenePath = "Assets/Main/Scene/Title.unity";
    private const int MinimumPlayerCountToStart = 1;
    private const int SessionPlayerCapacity = 2;
    private const float GoalCelebrationDelaySeconds = 2.7f;
    private const string SelectedStageSessionProperty = "SelectedStage";
    private const string StageCursorSessionProperty = "StageCursor";
    private const string EmptyStageSessionValue = "NONE";

    private const string CursorMessagePrefix = "CURSOR|";
    private const string SelectionMessagePrefix = "SELECT|";
    private const string AcknowledgementMessagePrefix = "ACK|";
    private const string StageClearRequestMessage = "FLOW|CLEAR";
    private const string StageSelectRequestMessage = "FLOW|STAGE_SELECT";
    private const string NextStageRequestMessage = "FLOW|NEXT_STAGE";
    private const string RestartStageRequestMessage = "FLOW|RESTART_STAGE";
    private const string TitleRequestMessage = "FLOW|TITLE_REQUEST";
    private const string TimeUpRequestMessage = "FLOW|TIME_UP_REQUEST";
    private const string TimeUpNotificationMessage = "FLOW|TIME_UP";

    private readonly HashSet<int> pendingStageAcknowledgements = new HashSet<int>();
    private readonly HashSet<int> playersAtGoal = new HashSet<int>();

    private NetworkRunner runner;
    private Coroutine stageLoadCoroutine;
    private Coroutine disconnectCoroutine;
    private Coroutine stageClearCoroutine;
    private bool isInitialized;
    private bool isLoadingScene;
    private bool hasBroadcastStageTimeUp;
    private int reliableMessageSequence;
    private string operationMessage = "CONNECTING...";
    private string currentStageCursorResourcePath;
    private GameObject lobbyButton;

    public static OnlineStageFlow Instance { get; private set; }

    public event Action StateChanged;
    public event Action<string> StageCursorChanged;
    public event Action<string> OperationMessageChanged;
    public event Action StageTimeUp;

    public NetworkRunner Runner => runner;
    public bool IsConnected => runner != null && runner.IsRunning;
    public bool IsSharedModeMasterClient => IsConnected && runner.IsSharedModeMasterClient;
    public int ConnectedPlayerCount => CountActivePlayers();
    public int NeededPlayerCount => SessionPlayerCapacity;
    public string OperationMessage => operationMessage;
    public string CurrentStageCursorResourcePath => currentStageCursorResourcePath;
    public bool CanControlStageSelection =>
        IsSharedModeMasterClient &&
        ConnectedPlayerCount >= MinimumPlayerCountToStart &&
        !isLoadingScene;
    public bool CanOpenStageSelect =>
        CanControlStageSelection &&
        SceneManager.GetActiveScene().name == OnlineConnectSceneName;

    public static OnlineStageFlow EnsureExists(GameObject stageSelectButton = null)
    {
        if (Instance == null)
        {
            GameObject flowObject = new GameObject("OnlineStageFlow");
            flowObject.AddComponent<OnlineStageFlow>();
        }

        if (stageSelectButton != null)
        {
            Instance.lobbyButton = stageSelectButton;
            Instance.TryInitializeLobbyUI();
        }

        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        while (Perfect_Online.Instance == null ||
               Perfect_Online.Instance.Runner == null ||
               !Perfect_Online.Instance.Runner.IsRunning)
        {
            yield return null;
        }

        Initialize(Perfect_Online.Instance.Runner);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (runner != null && isInitialized)
        {
            runner.RemoveCallbacks(this);
        }
    }

    public void Initialize(NetworkRunner networkRunner)
    {
        if (isInitialized && runner == networkRunner)
        {
            RefreshState();
            return;
        }

        runner = networkRunner;
        if (runner == null)
        {
            SetOperationMessage("NETWORK IS NOT READY");
            return;
        }

        runner.AddCallbacks(this);
        isInitialized = true;
        TryInitializeLobbyUI();
        RefreshState();
    }

    private void TryInitializeLobbyUI()
    {
        if (!isInitialized || lobbyButton == null)
        {
            return;
        }

        OnlineLobbyUI lobbyUI = GetComponent<OnlineLobbyUI>();
        if (lobbyUI == null)
        {
            lobbyUI = gameObject.AddComponent<OnlineLobbyUI>();
        }

        lobbyUI.Initialize(this, lobbyButton);
    }

    public bool LoadStageSelect()
    {
        Debug.Log($"[StageSelect] isInitialized = {isInitialized}");
        Debug.Log($"[StageSelect] runner = {runner}");

        if (!isInitialized || runner == null)
        {
            SetOperationMessage("NETWORK IS NOT READY");
            return false;
        }

        Debug.Log($"[StageSelect] ConnectedPlayerCount = {ConnectedPlayerCount}");
        Debug.Log($"[StageSelect] MinimumPlayerCountToStart = {MinimumPlayerCountToStart}");
        Debug.Log($"[StageSelect] isLoadingScene = {isLoadingScene}");

        if (!ValidateMasterClientOperation())
        {
            Debug.LogError("[StageSelect] ValidateMasterClientOperation() = false");
            return false;
        }

        if (isLoadingScene)
        {
            Debug.LogError("[StageSelect] isLoadingScene = true");
            return false;
        }

        if (ConnectedPlayerCount < MinimumPlayerCountToStart)
        {
            SetOperationMessage($"WAITING FOR PLAYER... ({ConnectedPlayerCount}/{MinimumPlayerCountToStart})");
            Debug.LogError("[StageSelect] Player count is insufficient");
            return false;
        }

        if (!TryGetSceneBuildIndex(StageSelectScenePath, out int buildIndex))
        {
            Debug.LogError($"[StageSelect] Scene not found: {StageSelectScenePath}");
            return false;
        }

        isLoadingScene = true;
        SetOperationMessage("OPENING STAGE SELECT...");

        GameManager.Instance.SetNpcSpawnDecisionForNextStage(ConnectedPlayerCount == 1);
        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);

        return true;
    }

    public void RequestStageClear()
    {
        if (!IsConnected || isLoadingScene)
        {
            return;
        }

        RegisterPlayerAtGoal(runner.LocalPlayer);

        if (IsSharedModeMasterClient)
        {
            TryOpenStageClearDestinationWhenEveryoneReachedGoal();
            return;
        }

        SendReliableMessage(runner.GetMasterClient(), StageClearRequestMessage);
    }

    public void RequestStageTimeUp()
    {
        if (!IsConnected)
        {
            return;
        }

        if (IsSharedModeMasterClient)
        {
            BroadcastStageTimeUp();
            return;
        }

        SendReliableMessage(runner.GetMasterClient(), TimeUpRequestMessage);
    }

    private void BroadcastStageTimeUp()
    {
        if (!IsSharedModeMasterClient || hasBroadcastStageTimeUp)
        {
            return;
        }

        hasBroadcastStageTimeUp = true;
        StageTimeUp?.Invoke();
        BroadcastReliableMessage(TimeUpNotificationMessage);
    }

    public void ReportPlayerReachedGoal(PlayerRef player)
    {
        if (!IsConnected || isLoadingScene)
        {
            return;
        }

        RegisterPlayerAtGoal(player);

        if (IsSharedModeMasterClient)
        {
            TryOpenStageClearDestinationWhenEveryoneReachedGoal();
        }
    }

    private void RegisterPlayerAtGoal(PlayerRef player)
    {
        if (player == PlayerRef.None || !IsActivePlayer(player))
        {
            return;
        }

        if (playersAtGoal.Add(player.PlayerId))
        {
            Debug.Log($"Player {player.PlayerId} reached the goal.");
        }
    }

    private bool IsActivePlayer(PlayerRef player)
    {
        if (!IsConnected)
        {
            return false;
        }

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            if (activePlayer == player)
            {
                return true;
            }
        }

        return false;
    }

    private void TryOpenStageClearDestinationWhenEveryoneReachedGoal()
    {
        if (!IsSharedModeMasterClient ||
            isLoadingScene ||
            SceneManager.GetActiveScene().name != MapSceneName)
        {
            return;
        }

        int activePlayerCount = 0;
        int reachedGoalCount = 0;

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            activePlayerCount++;

            if (playersAtGoal.Contains(activePlayer.PlayerId))
            {
                reachedGoalCount++;
            }
        }

        if (activePlayerCount == 0 || reachedGoalCount < activePlayerCount)
        {
            SetOperationMessage($"WAITING FOR PLAYERS... ({reachedGoalCount}/{activePlayerCount})");
            return;
        }

        if (stageClearCoroutine == null)
        {
            stageClearCoroutine = StartCoroutine(OpenStageClearAfterCelebration());
        }
    }

    private IEnumerator OpenStageClearAfterCelebration()
    {
        SetOperationMessage("ALL PLAYERS REACHED GOAL");
        yield return new WaitForSecondsRealtime(GoalCelebrationDelaySeconds);
        stageClearCoroutine = null;

        if (!IsSharedModeMasterClient ||
            isLoadingScene ||
            SceneManager.GetActiveScene().name != MapSceneName)
        {
            yield break;
        }

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            if (!playersAtGoal.Contains(activePlayer.PlayerId))
            {
                TryOpenStageClearDestinationWhenEveryoneReachedGoal();
                yield break;
            }
        }

        OpenStageClearDestination();
    }

    public void ReturnToStageSelect()
    {
        if (!IsConnected || isLoadingScene)
        {
            return;
        }

        if (!IsSharedModeMasterClient)
        {
            SendReliableMessage(runner.GetMasterClient(), StageSelectRequestMessage);
            SetOperationMessage("WAITING FOR HOST...");
            return;
        }

        GameManager.Instance.SetNpcSpawnDecisionForNextStage(ConnectedPlayerCount == 1);

        LoadNetworkScene(StageSelectScenePath, "OPENING STAGE SELECT...");
    }

    public void ContinueToNextStage()
    {
        if (!IsConnected || isLoadingScene)
        {
            return;
        }

        if (!IsSharedModeMasterClient)
        {
            SendReliableMessage(runner.GetMasterClient(), NextStageRequestMessage);
            SetOperationMessage("WAITING FOR HOST...");
            return;
        }

        if (!TryGetNextPlayableStage(out StageCatalogEntry nextStage))
        {
            LoadNetworkScene(GameCompleteScenePath, "OPENING COMPLETE SCREEN...");
            return;
        }

        if (stageLoadCoroutine == null)
        {
            stageLoadCoroutine = StartCoroutine(LoadSelectedStageRoutine(nextStage.resourcePath));
        }
    }

    public void RestartCurrentStage()
    {
        if (!IsConnected || isLoadingScene)
        {
            return;
        }

        if (!IsSharedModeMasterClient)
        {
            SendReliableMessage(runner.GetMasterClient(), RestartStageRequestMessage);
            SetOperationMessage("WAITING FOR HOST...");
            return;
        }

        string currentStagePath = StageSelectionContext.SelectedStageResourcePath;
        if (string.IsNullOrWhiteSpace(currentStagePath))
        {
            SetOperationMessage("STAGE DATA IS NOT READY");
            return;
        }

        GameManager.Instance.SetNpcSpawnDecisionForNextStage(ConnectedPlayerCount == 1);

        if (stageLoadCoroutine == null)
        {
            stageLoadCoroutine = StartCoroutine(LoadSelectedStageRoutine(currentStagePath));
        }
    }

    public void ReturnToTitle()
    {
        if (!IsConnected)
        {
            SceneManager.LoadScene("Title");
            return;
        }

        if (!IsSharedModeMasterClient)
        {
            SendReliableMessage(runner.GetMasterClient(), TitleRequestMessage);
            SetOperationMessage("WAITING FOR HOST...");
            return;
        }

        LoadNetworkScene(TitleScenePath, "RETURNING TO TITLE...");
    }

    private void OpenStageClearDestination()
    {
        string destination = HasNextPlayableStage()
            ? ResultScenePath
            : GameCompleteScenePath;
        string message = destination == ResultScenePath
            ? "OPENING RESULT..."
            : "OPENING COMPLETE SCREEN...";
        LoadNetworkScene(destination, message);
    }

    private bool LoadNetworkScene(string scenePath, string message)
    {
        if (!ValidateMasterClientOperation() || isLoadingScene)
        {
            return false;
        }

        if (!TryGetSceneBuildIndex(scenePath, out int buildIndex))
        {
            return false;
        }

        isLoadingScene = true;
        SetOperationMessage(message);
        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
        return true;
    }

    private bool HasNextPlayableStage()
    {
        return TryGetNextPlayableStage(out _);
    }

    private bool TryGetNextPlayableStage(out StageCatalogEntry nextStage)
    {
        nextStage = null;
        List<StageCatalogEntry> stages = StageCatalog.Load();
        string currentPath = StageSelectionContext.SelectedStageResourcePath;
        int currentIndex = -1;

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].resourcePath == currentPath)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
        {
            return false;
        }

        for (int i = currentIndex + 1; i < stages.Count; i++)
        {
            if (stages[i].HasMapData)
            {
                nextStage = stages[i];
                return true;
            }
        }

        return false;
    }

    private void BeginDisconnectAndReturnToTitle()
    {
        if (disconnectCoroutine == null)
        {
            disconnectCoroutine = StartCoroutine(DisconnectAndReturnToTitleRoutine());
        }
    }

    private IEnumerator DisconnectAndReturnToTitleRoutine()
    {
        isLoadingScene = true;
        SetOperationMessage("DISCONNECTING...");

        // Titleシーンの初期化が終わってから接続オブジェクトを破棄します。
        yield return new WaitForSecondsRealtime(0.1f);

        NetworkRunner activeRunner = runner;
        if (activeRunner != null && activeRunner.IsRunning)
        {
            var shutdownTask = activeRunner.Shutdown();
            while (!shutdownTask.IsCompleted)
            {
                yield return null;
            }
        }

        StageSelectionContext.Clear();

        if (Perfect_Online.Instance != null)
        {
            Destroy(Perfect_Online.Instance.gameObject);
        }

        if (SceneManager.GetActiveScene().name != "Title")
        {
            SceneManager.LoadScene("Title");
        }

        Destroy(gameObject);
    }

    public void BroadcastStageCursor(string stageResourcePath)
    {
        if (!IsSharedModeMasterClient || string.IsNullOrWhiteSpace(stageResourcePath))
        {
            return;
        }

        currentStageCursorResourcePath = stageResourcePath;
        StageCursorChanged?.Invoke(stageResourcePath);
        runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
        {
            { StageCursorSessionProperty, stageResourcePath }
        });
        BroadcastReliableMessage(CursorMessagePrefix + stageResourcePath);
    }

    public void RefreshStageCursorFromSession()
    {
        TryRestoreStageCursorFromSession(runner);
    }

    public bool ConfirmStageSelection(string stageResourcePath)
    {
        if (!ValidateMasterClientOperation() || isLoadingScene)
        {
            return false;
        }

        if (SceneManager.GetActiveScene().name != StageSelectSceneName)
        {
            SetOperationMessage("STAGE SELECT SCENE IS NOT ACTIVE");
            return false;
        }

        if (ConnectedPlayerCount < MinimumPlayerCountToStart)
        {
            SetOperationMessage("PLAYER DISCONNECTED");
            return false;
        }

        if (string.IsNullOrWhiteSpace(stageResourcePath))
        {
            SetOperationMessage("STAGE IS NOT SELECTED");
            return false;
        }

        if (stageLoadCoroutine != null)
        {
            return false;
        }

        stageLoadCoroutine = StartCoroutine(LoadSelectedStageRoutine(stageResourcePath));
        return true;
    }

    private IEnumerator LoadSelectedStageRoutine(string stageResourcePath)
    {
        if (!TryGetSceneBuildIndex(MapScenePath, out int mapBuildIndex))
        {
            stageLoadCoroutine = null;
            yield break;
        }

        StageSelectionContext.SetSelectedStage(stageResourcePath);
        currentStageCursorResourcePath = stageResourcePath;
        pendingStageAcknowledgements.Clear();

        runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
        {
            { SelectedStageSessionProperty, stageResourcePath }
        });

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (player != runner.LocalPlayer)
            {
                pendingStageAcknowledgements.Add(player.PlayerId);
            }
        }

        SetOperationMessage("SYNCHRONIZING STAGE...");
        SendStageSelectionToPendingPlayers(stageResourcePath);

        float timeoutAt = Time.realtimeSinceStartup + 1.5f;
        float resendAt = Time.realtimeSinceStartup + 0.5f;

        while (pendingStageAcknowledgements.Count > 0 && Time.realtimeSinceStartup < timeoutAt)
        {
            if (!IsConnected || ConnectedPlayerCount < MinimumPlayerCountToStart)
            {
                SetOperationMessage("PLAYER DISCONNECTED");
                stageLoadCoroutine = null;
                yield break;
            }

            if (Time.realtimeSinceStartup >= resendAt)
            {
                SendStageSelectionToPendingPlayers(stageResourcePath);
                resendAt = Time.realtimeSinceStartup + 0.5f;
            }

            yield return null;
        }

        if (ConnectedPlayerCount < MinimumPlayerCountToStart)
        {
            SetOperationMessage("PLAYER DISCONNECTED");
            stageLoadCoroutine = null;
            yield break;
        }

        if (pendingStageAcknowledgements.Count > 0)
        {
            // Reliable Dataに加えてSession Propertyにも保存しているため、
            // ACKだけ欠けた場合は遷移を止めずにMap側で同じ選択を復元する。
            Debug.LogWarning("ステージ同期ACKが届きませんでした。Session Propertyを使って遷移を続行します");
            pendingStageAcknowledgements.Clear();
        }

        isLoadingScene = true;
        SetOperationMessage("LOADING STAGE...");
        Debug.Log($"選択ステージへ移動します: {stageResourcePath}");
        runner.LoadScene(SceneRef.FromIndex(mapBuildIndex), LoadSceneMode.Single);
        stageLoadCoroutine = null;
    }

    private void SendStageSelectionToPendingPlayers(string stageResourcePath)
    {
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (pendingStageAcknowledgements.Contains(player.PlayerId))
            {
                SendReliableMessage(player, SelectionMessagePrefix + stageResourcePath);
            }
        }
    }

    private void BroadcastReliableMessage(string message)
    {
        if (!IsConnected)
        {
            return;
        }

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (player != runner.LocalPlayer)
            {
                SendReliableMessage(player, message);
            }
        }
    }

    private void SendReliableMessage(PlayerRef target, string message)
    {
        if (!IsConnected)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(message);
        ReliableKey key = ReliableKey.FromInts(
            0x465247,
            runner.LocalPlayer.PlayerId,
            target.PlayerId,
            ++reliableMessageSequence
        );

        runner.SendReliableDataToPlayer(target, key, bytes);
    }

    private bool ValidateMasterClientOperation()
    {
        Debug.Log($"[Validate] runner = {runner}");

        // 最初にrunnerそのものを確認
        if (runner == null)
        {
            Debug.LogError("[Validate] runner is NULL");
            SetOperationMessage("NETWORK IS NOT READY");
            return false;
        }

        Debug.Log($"[Validate] IsRunning = {runner.IsRunning}");

        if (!runner.IsRunning)
        {
            Debug.LogError("[Validate] runner is not running");
            SetOperationMessage("NETWORK IS NOT CONNECTED");
            return false;
        }

        Debug.Log($"[Validate] GameMode = {runner.GameMode}");
        Debug.Log($"[Validate] IsSharedModeMasterClient = {runner.IsSharedModeMasterClient}");
        Debug.Log($"[Validate] LocalPlayer = {runner.LocalPlayer}");

        if (!runner.SessionInfo.IsValid)
        {
            Debug.LogError("[Validate] SessionInfo is invalid");
            SetOperationMessage("SESSION IS NOT READY");
            return false;
        }

        Debug.Log($"[Validate] SessionInfo.Name = {runner.SessionInfo.Name}");

        if (!runner.IsSharedModeMasterClient)
        {
            Debug.LogWarning("[Validate] This client is not the Shared Mode Master Client");
            SetOperationMessage("WAITING FOR HOST...");
            return false;
        }

        Debug.Log("[Validate] SUCCESS");

        return true;
    }

    private bool TryGetSceneBuildIndex(string scenePath, out int buildIndex)
    {
        buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

        if (buildIndex >= 0)
        {
            return true;
        }

        SetOperationMessage("SCENE IS NOT IN BUILD SETTINGS");
        Debug.LogError($"Build Settingsにシーンが登録されていません: {scenePath}");
        return false;
    }

    private void RefreshState()
    {
        int count = ConnectedPlayerCount;
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (IsConnected && activeSceneName == OnlineConnectSceneName)
        {
            if (count >= MinimumPlayerCountToStart)
            {
                SetOperationMessage(IsSharedModeMasterClient ? "READY - PRESS STAGE SELECT" : "READY - WAITING FOR HOST");
            }
            else
            {
                SetOperationMessage($"WAITING FOR PLAYER... ({count}/{MinimumPlayerCountToStart})");
            }
        }
        else if (IsConnected && activeSceneName == StageSelectSceneName && count < MinimumPlayerCountToStart)
        {
            SetOperationMessage("PLAYER DISCONNECTED");
        }

        StateChanged?.Invoke();
    }

    private int CountActivePlayers()
    {
        if (!IsConnected)
        {
            return 0;
        }

        int count = 0;
        foreach (PlayerRef unused in runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    private void SetOperationMessage(string message)
    {
        operationMessage = message;
        OperationMessageChanged?.Invoke(message);
        StateChanged?.Invoke();
    }

    public void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        RefreshState();
    }

    public void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player)
    {
        pendingStageAcknowledgements.Remove(player.PlayerId);
        playersAtGoal.Remove(player.PlayerId);

        if (networkRunner.IsSharedModeMasterClient)
        {
            TryOpenStageClearDestinationWhenEveryoneReachedGoal();
        }

        RefreshState();
    }

    public void OnSceneLoadStart(NetworkRunner networkRunner)
    {
        hasBroadcastStageTimeUp = false;

        if (stageClearCoroutine != null)
        {
            StopCoroutine(stageClearCoroutine);
            stageClearCoroutine = null;
        }

        playersAtGoal.Clear();
        TryRestoreSelectedStageFromSession(networkRunner);
        TryRestoreStageCursorFromSession(networkRunner);
    }

    public void OnSceneLoadDone(NetworkRunner networkRunner)
    {
        isLoadingScene = false;
        TryRestoreSelectedStageFromSession(networkRunner);
        TryRestoreStageCursorFromSession(networkRunner);

        if (SceneManager.GetActiveScene().name == "Title")
        {
            BeginDisconnectAndReturnToTitle();
            return;
        }

        if (SceneManager.GetActiveScene().name == StageSelectSceneName)
        {
            StageSelectionContext.Clear();
            pendingStageAcknowledgements.Clear();
            SetOperationMessage(IsSharedModeMasterClient ? "SELECT A STAGE" : "HOST IS SELECTING A STAGE");
        }

        RefreshState();
    }

    private static void TryRestoreSelectedStageFromSession(NetworkRunner networkRunner)
    {
        if (networkRunner == null)
        {
            return;
        }

        // SessionInfo can be cleared while a runner is shutting down. Read it
        // once so the null check and property access use the same instance.
        var sessionInfo = networkRunner.SessionInfo;
        if (sessionInfo == null || sessionInfo.Properties == null)
        {
            return;
        }

        // The master sets its local selection before the session property has
        // necessarily propagated. Keep that newest local value. Other players
        // must always replace the previous stage with the master's current one.
        if (networkRunner.IsSharedModeMasterClient && StageSelectionContext.HasSelection)
        {
            return;
        }

        if (sessionInfo.Properties.TryGetValue(
                SelectedStageSessionProperty,
                out SessionProperty stageProperty) &&
            stageProperty.IsString)
        {
            string selectedStage = stageProperty;
            if (!string.IsNullOrWhiteSpace(selectedStage) &&
                !string.Equals(selectedStage, EmptyStageSessionValue, StringComparison.OrdinalIgnoreCase))
            {
                StageSelectionContext.SetSelectedStage(selectedStage);
            }
        }
    }

    private void TryRestoreStageCursorFromSession(NetworkRunner networkRunner)
    {
        if (networkRunner == null || networkRunner.IsSharedModeMasterClient)
        {
            return;
        }

        // SessionInfo can be cleared while a runner is shutting down. Read it
        // once so the null check and property access use the same instance.
        var sessionInfo = networkRunner.SessionInfo;
        if (sessionInfo == null || sessionInfo.Properties == null)
        {
            return;
        }

        if (sessionInfo.Properties.TryGetValue(
                StageCursorSessionProperty,
                out SessionProperty cursorProperty) &&
            cursorProperty.IsString)
        {
            string cursorPath = cursorProperty;
            if (!string.IsNullOrWhiteSpace(cursorPath) &&
                cursorPath != currentStageCursorResourcePath)
            {
                currentStageCursorResourcePath = cursorPath;
                StageCursorChanged?.Invoke(cursorPath);
            }
        }
    }

    public void OnReliableDataReceived(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (data.Array == null || data.Count <= 0)
        {
            return;
        }

        string message = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);

        if (networkRunner.IsSharedModeMasterClient)
        {
            if (message == StageClearRequestMessage)
            {
                RegisterPlayerAtGoal(player);
                TryOpenStageClearDestinationWhenEveryoneReachedGoal();
                return;
            }

            if (message == StageSelectRequestMessage)
            {
                ReturnToStageSelect();
                return;
            }

            if (message == NextStageRequestMessage)
            {
                ContinueToNextStage();
                return;
            }

            if (message == RestartStageRequestMessage)
            {
                RestartCurrentStage();
                return;
            }

            if (message == TitleRequestMessage)
            {
                ReturnToTitle();
                return;
            }

            if (message == TimeUpRequestMessage)
            {
                BroadcastStageTimeUp();
                return;
            }
        }

        if (message == TimeUpNotificationMessage)
        {
            if (player == networkRunner.GetMasterClient())
            {
                StageTimeUp?.Invoke();
            }

            return;
        }

        if (message.StartsWith(CursorMessagePrefix, StringComparison.Ordinal))
        {
            if (player != networkRunner.GetMasterClient())
            {
                return;
            }

            currentStageCursorResourcePath = message.Substring(CursorMessagePrefix.Length);
            StageCursorChanged?.Invoke(currentStageCursorResourcePath);
            return;
        }

        if (message.StartsWith(SelectionMessagePrefix, StringComparison.Ordinal))
        {
            if (player != networkRunner.GetMasterClient())
            {
                return;
            }

            string selectedPath = message.Substring(SelectionMessagePrefix.Length);
            StageSelectionContext.SetSelectedStage(selectedPath);
            currentStageCursorResourcePath = selectedPath;
            StageCursorChanged?.Invoke(selectedPath);
            SetOperationMessage("STAGE SYNCHRONIZED");
            SendReliableMessage(player, AcknowledgementMessagePrefix + selectedPath);
            return;
        }

        if (message.StartsWith(AcknowledgementMessagePrefix, StringComparison.Ordinal) && IsSharedModeMasterClient)
        {
            string acknowledgedPath = message.Substring(AcknowledgementMessagePrefix.Length);
            if (acknowledgedPath == StageSelectionContext.SelectedStageResourcePath)
            {
                pendingStageAcknowledgements.Remove(player.PlayerId);
            }
        }
    }

    public void OnShutdown(NetworkRunner networkRunner, ShutdownReason shutdownReason)
    {
        SetOperationMessage($"SESSION ENDED: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner networkRunner)
    {
        RefreshState();
    }

    public void OnDisconnectedFromServer(NetworkRunner networkRunner, NetDisconnectReason reason)
    {
        SetOperationMessage($"DISCONNECTED: {reason}");
    }

    public void OnConnectFailed(NetworkRunner networkRunner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        SetOperationMessage($"CONNECTION FAILED: {reason}");
    }

    public void OnInput(NetworkRunner networkRunner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner networkRunner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner networkRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner networkRunner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner networkRunner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner networkRunner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner networkRunner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataProgress(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
}
