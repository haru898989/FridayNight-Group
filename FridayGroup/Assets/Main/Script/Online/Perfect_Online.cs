using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class Perfect_Online : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    [Header("ロビーUI（スタートボタン）")]
    [SerializeField] private GameObject startButtonUI;

    private async void Start()
    {
        Debug.Log("Fusion 2.1.1 接続開始");

        if (startButtonUI != null)
        {
            startButtonUI.SetActive(true);
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "Fusion_Test_Room_2026",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("接続成功！");
        }
        else
        {
            Debug.LogError($"接続失敗 理由: {result.ShutdownReason}");
        }
    }

    // Startボタンから呼び出し（マスタークライアントのみシーン遷移可能）
    public void OnStartButtonClicked()
    {
        Debug.Log("Startボタンが押されました");

        if (_runner != null && _runner.IsSharedModeMasterClient)
        {
            Debug.Log("Mainシーンへ移動");
            _runner.LoadScene("Main");
        }
    }

    // プレイヤーが参加したとき（ローカルプレイヤーなら自身のキャラを生成）
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("参加：" + player);

        if (GameManager.Instance != null)
        {
            Debug.Log("GameManager発見");
            GameManager.Instance.OnPlayerJoined(runner, player);
        }
        else
        {
            Debug.LogError("GameManager.Instanceがnullです");
        }
    }

    // プレイヤーが退出・回線落ちしたとき
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"退出 PlayerID : {player.PlayerId}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerLeft(runner, player);
        }
    }

    // 回線が切断（シャットダウン）されたときの処理
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"セッションが終了しました。理由: {shutdownReason}");

        if (shutdownReason == ShutdownReason.ConnectionTimeout ||
            shutdownReason == ShutdownReason.Error)
        {
            Debug.LogError("通信エラーまたはタイムアウトによる切断を検知しました。");
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("サーバー接続完了");
    }

    // --- 以下、Fusionのインターフェースを満たすためのコールバック関数群 ---
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}