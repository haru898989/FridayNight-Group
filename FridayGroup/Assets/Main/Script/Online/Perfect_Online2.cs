using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;

public class Perfect_Online : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    private async void Start()
    {
        Debug.Log("接続テストを開始");


        _runner = gameObject.AddComponent<NetworkRunner>();

        _runner.AddCallbacks(this);

        Debug.Log("Callback登録完了");


        _runner.ProvideInput = true;


        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "Fusion_Test_Room_2026",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });


        if (result.Ok)
        {
            Debug.Log("接続テストに成功!");
            Debug.Log($"現在のルーム名: {_runner.SessionInfo.Name}");
        }
        else
        {
            Debug.LogError($"接続に失敗 理由: {result.ShutdownReason}");
        }
    }



    // プレイヤー参加確認
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"参加したPlayerID : {player.PlayerId}");


        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerJoined(runner, player);
        }
        else
        {
            Debug.LogError("GameManagerが存在しません");
        }


        if (player == runner.LocalPlayer)
        {
            Debug.Log($"ローカルプレイヤー({player})です");
        }
    }



    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("サーバー接続完了");
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Shutdown : {shutdownReason}");
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"退出 PlayerID : {player.PlayerId}");
    }



    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

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