using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Perfect_Online : MonoBehaviour, INetworkRunnerCallbacks
{
    private const string MapScenePath = "Assets/Main/Scene/Map.unity";
    private const string SessionName = "Fusion_Test_Room_2026";
    

    public static Perfect_Online Instance { get; private set; }

    [Header("ロビーUI（スタートボタン）")]
    [SerializeField] private GameObject startButtonUI;

    private NetworkRunner runner;
    private bool isLoadingMap;

    public NetworkRunner Runner => runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("カーソル非表示設定を実行");

        if (runner != null)
        {
            return;
        }

        SetStartButtonVisible(false);
        Debug.Log("Fusion Shared接続を開始します");

        runner = GetComponent<NetworkRunner>();
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        runner.AddCallbacks(this);
        runner.ProvideInput = true;

        NetworkSceneManagerDefault sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = SessionName,
            PlayerCount = 2,
            SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "StageCursor", "NONE" },
                { "SelectedStage", "NONE" }
            },
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError($"接続に失敗しました: {result.ShutdownReason}");
            return;
        }

        Debug.Log("Fusion Sharedへの接続が完了しました");
        GameManager.Instance?.SetRunner(runner);
        RefreshStartButtonVisibility();
    }

    public void OnStartButtonClicked()
    {
        LoadMap();
    }

    public void LoadMap()
    {
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogError("NetworkRunnerが接続されていません");
            return;
        }

        if (!runner.IsSharedModeMasterClient)
        {
            Debug.Log("Mapへ移動できるのはShared Mode Master Clientだけです");
            return;
        }

        if (isLoadingMap)
        {
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(MapScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"MapシーンがBuild Settingsに登録されていません: {MapScenePath}");
            return;
        }

        isLoadingMap = true;
        SetStartButtonVisible(false);
        GameManager.Instance?.SetMapNotReady();
        SoundManager.Instance.PlaySE(0);

        Debug.Log("OnlineConnectからMapへ移動します");
        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
    }

    public void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        Debug.Log($"プレイヤーが参加しました: {player.PlayerId}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません");
            return;
        }

        GameManager.Instance.OnPlayerJoined(networkRunner, player);
        RefreshStartButtonVisibility();
    }

    public void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player)
    {
        Debug.Log($"プレイヤーが退出しました: {player.PlayerId}");
        GameManager.Instance?.OnPlayerLeft(networkRunner, player);
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();

        if (spawner != null)
        {
            spawner.PlayerLeft();
        }

        RefreshStartButtonVisibility();
    }

    public void OnSceneLoadStart(NetworkRunner networkRunner)
    {
        GameManager.Instance?.SetMapNotReady();
    }

    public void OnSceneLoadDone(NetworkRunner networkRunner)
    {
        isLoadingMap = false;
        GameManager.Instance?.SetRunner(networkRunner);
        Debug.Log($"Fusionのシーン読み込みが完了しました: {SceneManager.GetActiveScene().name}");
    }

    public void OnShutdown(NetworkRunner networkRunner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"セッションが終了しました: {shutdownReason}");
        SetStartButtonVisible(false);
    }

    public void OnConnectedToServer(NetworkRunner networkRunner)
    {
        Debug.Log("サーバー接続完了");
    }

    private void RefreshStartButtonVisibility()
    {
        SetStartButtonVisible(
            runner != null &&
            runner.IsRunning &&
            runner.IsSharedModeMasterClient &&
            !isLoadingMap
        );
    }

    private void SetStartButtonVisible(bool isVisible)
    {
        if (startButtonUI != null)
        {
            startButtonUI.SetActive(isVisible);
        }
    }

    public void OnInput(NetworkRunner networkRunner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner networkRunner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner networkRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner networkRunner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner networkRunner, NetDisconnectReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner networkRunner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner networkRunner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner networkRunner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner networkRunner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
}
