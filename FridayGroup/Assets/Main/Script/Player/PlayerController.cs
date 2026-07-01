using UnityEngine;

// 「: PlayerBase」と書くことで、PlayerBaseの機能を全て引き継ぐ（継承）
public class PlayerController : PlayerBase
{
    public int playerID; // このプレイヤー固有のID

    // override: 親クラスのStart()を上書きしつつ、拡張する
    protected override void Start()
    {
        // base.Start() で親クラス(PlayerBase)のStart()を先に実行する
        base.Start();

        // --- ここから子クラス(PlayerController)独自の処理 ---

        // プレイヤーIDの割り当てなどの初期設定
        //playerID = 1;
        //Debug.Log($"プレイヤー{playerID}が操作可能になりました！");
    }

    // 親クラスで書いた Update や OnMove, OnActionA などの関数は、
    // ここに書かなくても自動的に使えるようになっています！
}
