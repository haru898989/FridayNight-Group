using UnityEngine;
using System.Collections;

public class Pitfall : GimmickBase
{
    [Header("Pitfall Settings")]
    [SerializeField] private StageController stageController;
    [SerializeField] private float fallTime = 1.0f;
    [SerializeField] private float fallDistance = 3.0f;

    private bool isWarping = false;

    /// <summary>
    /// プレイヤーが落とし穴に触れたときに呼ばれる関数
    /// </summary>
    protected override void OnPlayerHit(GameObject playerObject)
    {
        // ワープ処理中に再度発動しないようにする
        if (isWarping)
        {
            return;
        }

        // プレイヤーを落下させてからワープさせる処理を開始する
        StartCoroutine(FallAndWarp(playerObject));
    }

    /// <summary>
    /// プレイヤーを下に落とす演出を行い、その後ステージ側で設定した座標へワープさせる関数
    /// </summary>
    private IEnumerator FallAndWarp(GameObject playerObject)
    {
        isWarping = true;

        Debug.Log("Pitfall started");

        Collider playerCollider = playerObject.GetComponent<Collider>();
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();

        // 落下中に床や他のオブジェクトに引っかからないようにColliderを一時的に無効化する
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // 落下前にプレイヤーの速度と回転を止める
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // 現在位置を落下開始位置として保存する
        Vector3 startPosition = playerObject.transform.position;

        // 現在位置から下方向にfallDistance分だけ移動した位置を落下先にする
        Vector3 fallPosition = startPosition + Vector3.down * fallDistance;

        float timer = 0.0f;

        // fallTime秒かけて、開始位置から落下先まで少しずつ移動させる
        while (timer < fallTime)
        {
            timer += Time.deltaTime;

            float rate = timer / fallTime;
            playerObject.transform.position = Vector3.Lerp(startPosition, fallPosition, rate);

            yield return null;
        }

        // StageControllerが設定されていない場合は、ワープできないので処理を終了する
        if (stageController == null)
        {
            Debug.LogWarning("StageController is not set");

            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }

            isWarping = false;
            yield break;
        }

        // ステージ側から落とし穴のワープ先座標を取得する
        Vector3 warpPosition = stageController.GetPitfallWarpPosition();

        PlayerWarp playerWarp = playerObject.GetComponent<PlayerWarp>();

        // PlayerWarpがある場合は、x, y, zの座標を引数として渡してワープさせる
        if (playerWarp != null)
        {
            playerWarp.WarpToPosition(warpPosition.x, warpPosition.y, warpPosition.z);
        }
        else
        {
            // PlayerWarpがない場合は、直接Transformの座標を変更する
            playerObject.transform.position = warpPosition;
        }

        // ワープ後にColliderを有効化し、通常の当たり判定に戻す
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        Debug.Log("Pitfall finished");

        isWarping = false;
    }
}