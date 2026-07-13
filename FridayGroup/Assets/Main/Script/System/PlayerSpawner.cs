using Fusion;
using UnityEngine;

// IPlayerJoined をつけることで、「繋いだ瞬間」を検知できるようになります
public class PlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    [SerializeField]
    private NetworkPrefabRef playerPrefab; // 出現させるプレイヤーのプレハブ

    [SerializeField]
    private Transform spawnPoint; // 出現させる場所

    // 誰かがネットワークに「繋いだ（Joinした）」瞬間に自動で呼ばれる機能
    public void PlayerJoined(PlayerRef player)
    {
        // 自分がサーバー（ホスト）として部屋を管理している場合のみ生成処理を行う
        if (Runner.IsServer)
        {
            // ここでプレイヤーオブジェクトを生成（Spawn）し、繋いできた人に権限（player）を渡す
            Runner.Spawn(playerPrefab, spawnPoint.position, Quaternion.identity, player);
            Debug.Log("プレイヤーが接続しました！プレハブを生成します。");
        }
    }
}