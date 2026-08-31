using UnityEngine;
using System.Collections;

public class BearTrap : GimmickBase
{
    [Header("Bear Trap Settings")]
    [SerializeField] private float stopTime = 2.0f;
    private bool isConsumed;

    /// <summary>
    /// プレイヤーがとらばさみに触れたときに呼ばれる関数
    /// </summary>
    protected override void OnPlayerHit(GameObject playerObject)
    {
        Debug.Log("Bear trap activated");

        //NPC
        NPCBase npc = playerObject.GetComponent<NPCBase>();

        if(npc != null)
        {
            npc.NotifyTrapTriggered();
            npc.StopByTrap(stopTime);
        }
        else
        {
            StartCoroutine(StopPlayer(playerObject));
        }

        // とらばさみ発動時の効果音を再生する
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(0);
        }

        PlayerBase player = playerObject.GetComponent<PlayerBase>();
        if (player != null && player.Object != null)
        {
            player.RPC_RemoveBearTrap(transform.position);
        }
        else
        {
            ConsumeAtPosition(transform.position);
        }
    }

    /// <summary>
    /// 同じ座標に生成されている各クライアントのトラバサミを使用済みにします。
    /// </summary>
    public static void ConsumeAtPosition(Vector3 trapPosition)
    {
        BearTrap[] traps = FindObjectsByType<BearTrap>(FindObjectsSortMode.None);

        foreach (BearTrap trap in traps)
        {
            if ((trap.transform.position - trapPosition).sqrMagnitude <= 0.01f)
            {
                trap.Consume();
            }
        }
    }

    private void Consume()
    {
        if (isConsumed)
        {
            return;
        }

        isConsumed = true;
        GameObject trapRoot = transform.parent != null
            ? transform.parent.gameObject
            : gameObject;

        foreach (Collider trapCollider in trapRoot.GetComponentsInChildren<Collider>(true))
        {
            trapCollider.enabled = false;
        }

        foreach (Renderer trapRenderer in trapRoot.GetComponentsInChildren<Renderer>(true))
        {
            trapRenderer.enabled = false;
        }

        // プレイヤーの固定解除コルーチンが完了してから実体を破棄する。
        Destroy(trapRoot, stopTime + 0.1f);
    }

    /// <summary>
    /// プレイヤーのRigidbodyを一時的に固定して、一定時間動けなくする関数
    /// </summary>
    private IEnumerator StopPlayer(GameObject playerObject)
    {
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();

        // Rigidbodyが付いていない場合は、停止処理を行わず終了する
        if (playerRigidbody == null)
        {
            yield break;
        }

        // 複数のトラバサミを同時に踏んだ場合、2個目がFreezeAllを
        // 「元の制約」として保存すると、解除後も永久に固定されてしまう。
        // すでに停止中なら最初の解除処理へ任せる。
        if (playerRigidbody.constraints == RigidbodyConstraints.FreezeAll)
        {
            yield break;
        }

        // 元の制約を保存しておき、停止後に元へ戻せるようにする
        RigidbodyConstraints oldConstraints = playerRigidbody.constraints;

        // 現在の移動速度と回転速度を0にして、動きを止める
        playerRigidbody.velocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        // Rigidbodyを完全に固定して、プレイヤーを動けない状態にする
        playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        // stopTime秒だけ待つ
        yield return new WaitForSeconds(stopTime);

        // 保存しておいた元の制約に戻し、プレイヤーを再び動ける状態にする
        playerRigidbody.constraints = oldConstraints;
    }
}
