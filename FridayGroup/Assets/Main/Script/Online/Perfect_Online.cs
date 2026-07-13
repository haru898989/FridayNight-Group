using UnityEngine;
using Fusion;

public class Test : MonoBehaviour
{
    private NetworkRunner _runner;

    //ゲーム開始時に接続テストを実行 async:非同期
    private async void Start()
    {
        Debug.Log("接続テストを開始");

        //ネットワークを管理するRunnerを作成
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        //Sharedモードで接続を試みる
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,           //Sharedモード
            SessionName = "Fusion_Test_Room_2026", //テスト用の部屋名
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() //画面管理
        });

        //結果
        if (result.Ok)
        {
            Debug.Log("接続テストに成功!");
            Debug.Log($"現在のルーム名: {_runner.SessionInfo.Name}");
        }
        else
        {
            Debug.LogError($"接続に失敗 理由: {result.ShutdownReason}");
            Debug.LogError("AppIDが間違っているか、正しく設定されていない可能性があります。");
        }
    }
}