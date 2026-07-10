using UnityEngine;

public class RollingRockTrigger : GimmickBase
{
    [Header("Trigger Settings")]
    [SerializeField] private RollingRock rollingRock;

    /// <summary>
    /// プレイヤーが大岩発動用Triggerに触れたときに呼ばれる関数
    /// </summary>
    protected override void OnPlayerHit(GameObject playerObject)
    {
        // RollingRockが設定されていない場合は、警告を出して処理を終了する
        if (rollingRock == null)
        {
            Debug.LogWarning("RollingRock is not set");
            return;
        }

        // 登録されている大岩のStartRolling関数を呼び出し、大岩を動かし始める
        rollingRock.StartRolling();
    }
}