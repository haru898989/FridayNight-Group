using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class Perfect_Online : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [Header("生成するプレイヤーのプレハブ")]
    [SerializeField] private NetworkPrefabRef playerPrefab;

    // プレイヤーの接続情報を管理する辞書
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private async void Start()
    {
        Debug.Log("Fusion 2.1.1 接続テストを開始");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // コールバックの登録
        _runner.AddCallbacks(this);

        // Sharedモードで接続を試みる
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,           // Sharedモード
            SessionName = "Fusion_Test_Room_2026", // テスト用の部屋名
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // 画面管理
        });

        if (result.Ok)
        {
            Debug.Log("接続テストに成功!");
        }
        else
        {
            Debug.LogError($"接続に失敗 理由: {result.ShutdownReason}");
        }
    }

    // --- INetworkRunnerCallbacks の実装 ---

    // プレイヤーが入室したとき
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            Debug.Log($"ローカルプレイヤー({player})が参加しました。キューブを生成します。");

            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-3f, 3f), 1f, UnityEngine.Random.Range(-3f, 3f));
            NetworkObject playerObj = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            _spawnedPlayers.Add(player, playerObj);
        }
    }

    // プレイヤーが退室・回線落ちしたとき
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"プレイヤー({player})が退室、または切断されました。");

        if (_spawnedPlayers.TryGetValue(player, out NetworkObject playerObj))
        {
            if (playerObj != null)
            {
                runner.Despawn(playerObj);
            }
            _spawnedPlayers.Remove(player);
        }
    }

    // 回線が完全に切断（シャットダウン）されたとき
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"セッションが終了しました。理由: {shutdownReason}");

        // 【修正】Fusion 2に確実に存在する定義のみでタイムアウトやエラーを判定
        if (shutdownReason == ShutdownReason.ConnectionTimeout ||
            shutdownReason == ShutdownReason.Error)
        {
            Debug.LogError("通信エラーまたはタイムアウトによる切断を検知しました。ここに復帰ロジックを組めます。");
        }
    }

    // --- 以下、Fusion 2.1.1 のインターフェースを満たすためのコールバック関数群 ---

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}