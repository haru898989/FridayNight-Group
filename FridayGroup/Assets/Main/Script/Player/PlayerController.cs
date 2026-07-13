using UnityEngine;
using Fusion; // 必要に応じて追加

// PlayerBase（NetworkBehaviour）を継承
public class PlayerController : PlayerBase
{
    public int playerID; // このプレイヤー固有のID

    // 【修正】override する対象を Start から Spawned に変更します
    public override void Spawned()
    {
        // base.Spawned() で親クラス(PlayerBase)のSpawned()を先に実行する
        base.Spawned();

        // --- ここから子クラス(PlayerController)独自の初期化処理 ---

        // 例: プレイヤーIDの割り当てなど（必要に応じてコメントアウトを解除）
        // playerID = 1;
        // Debug.Log($"プレイヤー{playerID}が操作可能になりました！");
    }
}