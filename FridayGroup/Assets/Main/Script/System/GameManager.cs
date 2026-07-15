using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public int playerID;          // 1P, 2P
        public bool useController;    // Controllerならtrue
        public string objectName;     // A / B
        public PlayerRef playerRef;   // Fusion Player（デフォルト値は PlayerRef.None）
    }

    // 2人分のデータを保存
    public PlayerData[] players = new PlayerData[2];

    [SerializeField] private NetworkPrefabRef playerPrefabA;
    [SerializeField] private NetworkPrefabRef playerPrefabB;
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // シーン遷移してもGameManagerを破壊しない
        DontDestroyOnLoad(gameObject);

        // 配列の要素を初期化
        players[0] = new PlayerData { playerRef = PlayerRef.None };
        players[1] = new PlayerData { playerRef = PlayerRef.None };

        Debug.Log("GameManagerがシングルトンとして保持されました");
    }

    /// <summary>
    /// プレイヤーがルームに参加したときにホスト/サーバー側で呼ばれるコールバック
    /// </summary>
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // サーバー（またはホスト）だけがSpawnを実行する権限を持ちます
        if (!runner.IsServer) return;

        int index = GetEmptyPlayerIndex();

        if (index == -1)
        {
            Debug.LogError("プレイヤー上限（2名）に達しているため、参加を拒否しました。");
            return;
        }

        // 基本情報の保存
        players[index].playerID = index + 1;
        players[index].playerRef = player;

        if (index == 0)
        {
            // 1Pの設定
            players[index].useController = true;
            players[index].objectName = "A";
        }
        else
        {
            // 2Pの設定
            players[index].useController = false;
            players[index].objectName = "B";
        }

        // 1Pと2Pで生成するプレハブを分岐
        NetworkPrefabRef prefab = (index == 0) ? playerPrefabA : playerPrefabB;

        if (spawnPoint == null)
        {
            Debug.LogError("SpawnPointがインスペクターで設定されていません！");
            return;
        }

        // プレイヤーの生成（最後の引数で入力権限: Input Authority を割り当て）
        NetworkObject playerObject = runner.Spawn(
            prefab,
            spawnPoint.position,
            Quaternion.identity,
            player // ← ここで入力権限（Input Authority）を割り当て！
        );

        Debug.Log($"プレイヤー {player.PlayerId} のオブジェクトを生成しました（{players[index].playerID}Pとして登録）");
        Debug.Log($"{players[index].playerID}P設定: Controller={players[index].useController}, Object={players[index].objectName}");
    }

    /// <summary>
    /// プレイヤーが退出した際にデータをクリアする処理（推奨追加機能）
    /// </summary>
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].playerRef == player)
            {
                Debug.Log($"{players[i].playerID}P (Player {player.PlayerId}) が退出したためデータをクリアします。");
                players[i].playerID = 0;
                players[i].playerRef = PlayerRef.None;
                players[i].useController = false;
                players[i].objectName = string.Empty;
                break;
            }
        }
    }

    /// <summary>
    /// 空いているデータ枠のインデックスを取得
    /// </summary>
    private int GetEmptyPlayerIndex()
    {
        for (int i = 0; i < players.Length; i++)
        {
            // PlayerRef.None（デフォルト状態）の枠を探す
            if (players[i].playerRef == PlayerRef.None)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// プレイヤーデータをインデックスから取得
    /// </summary>
    public PlayerData GetPlayerData(int index)
    {
        if (index < 0 || index >= players.Length) return null;
        return players[index];
    }
}