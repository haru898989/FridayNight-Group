using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnPoint;
    // プレイヤーがルームに参加した時に呼ばれるコールバック等で処理します
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // サーバー（またはホスト）だけがSpawnを実行する権限を持ちます
        if (runner.IsServer)
        {
            // 第4引数の「player」が振り分けの鍵です。
            // ここで「このオブジェクトの入力権限はこのプレイヤーに与える」と指定しています。
            NetworkObject playerObject = runner.Spawn(
                playerPrefab,
                spawnPoint.position,
                Quaternion.identity,
                player // ← ここで入力権限（Input Authority）を割り当て！
            );

            Debug.Log($"プレイヤー {player.PlayerId} のオブジェクトを生成しました");
        }
    }
}
