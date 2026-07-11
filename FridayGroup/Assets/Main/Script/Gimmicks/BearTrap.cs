using UnityEngine;
using System.Collections;

public class BearTrap : GimmickBase
{
    [Header("Bear Trap Settings")]
    [SerializeField] private float stopTime = 2.0f;

    /// <summary>
    /// プレイヤーがとらばさみに触れたときに呼ばれる関数
    /// </summary>
    protected override void OnPlayerHit(GameObject playerObject)
    {
        Debug.Log("Bear trap activated");

        // とらばさみ発動時の効果音を再生する
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(0);
        }

        // プレイヤーを一定時間止める処理を開始する
        StartCoroutine(StopPlayer(playerObject));
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