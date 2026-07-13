using UnityEngine;
using System.Collections;

public class Pitfall : GimmickBase
{
    [Header("Pitfall Settings")]
    [SerializeField] private StageController stageController;
    [SerializeField] private float fallTime = 1.0f;
    [SerializeField] private float fallDistance = 3.0f;
    [SerializeField] private float landSoundDelay = 0.2f;

    [Header("Camera Settings")]
    [SerializeField] private PitfallCameraController pitfallCameraController;
    [SerializeField] private Transform entryCameraPoint;
    [SerializeField] private Transform fallingCameraPoint;
    [SerializeField] private Transform landCameraPoint;
    [SerializeField] private float entryCameraWaitTime = 0.3f;
    [SerializeField] private float landCameraWaitTime = 0.6f;

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

        // プレイヤーを落下させてからワープする処理を開始する
        StartCoroutine(FallAndWarp(playerObject));
    }

    /// <summary>
    /// プレイヤーを下に落とし，指定地点へワープさせる関数
    /// </summary>
    private IEnumerator FallAndWarp(GameObject playerObject)
    {
        isWarping = true;

        Debug.Log("Pitfall started");

        // 落とし穴演出用カメラに切り替える
        if (pitfallCameraController != null)
        {
            pitfallCameraController.StartPitfallCamera();
            pitfallCameraController.MoveToCameraPoint(entryCameraPoint);
        }

        // 落とし穴に入った瞬間のカメラを少し見せる
        yield return new WaitForSeconds(entryCameraWaitTime);

        // 落下開始と同時に効果音を鳴らす
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(1);
        }

        Collider playerCollider = playerObject.GetComponent<Collider>();
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();

        // 落下中に引っかからないようにColliderを一時的に無効化する
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // 落下前に速度を止める
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // 落下中のカメラアングルに切り替える
        if (pitfallCameraController != null)
        {
            pitfallCameraController.MoveToCameraPoint(fallingCameraPoint);
        }

        // 現在位置を落下開始位置として保存する
        Vector3 startPosition = playerObject.transform.position;

        // 現在位置から下方向にfallDistance分だけ移動した位置を落下先にする
        Vector3 fallPosition = startPosition + Vector3.down * fallDistance;

        float timer = 0.0f;

        // fallTime秒かけて下に落とす
        while (timer < fallTime)
        {
            timer += Time.deltaTime;

            float rate = timer / fallTime;
            playerObject.transform.position = Vector3.Lerp(startPosition, fallPosition, rate);

            yield return null;
        }

        // StageControllerが設定されていない場合は終了する
        if (stageController == null)
        {
            Debug.LogWarning("StageController is not set");

            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }

            if (pitfallCameraController != null)
            {
                pitfallCameraController.EndPitfallCamera();
            }

            isWarping = false;
            yield break;
        }

        // ワープ先を取得する
        Vector3 warpPosition = stageController.GetPitfallWarpPosition();


        // Rigidbodyがある場合は，Rigidbodyの位置を直接変更する
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = warpPosition;
        }

        // Transformの位置も直接変更して，確実にワープさせる
        playerObject.transform.position = warpPosition;

        // 物理演算とTransformの位置を同期する
        Physics.SyncTransforms();


        // 着地時のカメラアングルに切り替える
        if (pitfallCameraController != null)
        {
            pitfallCameraController.MoveToCameraPoint(landCameraPoint);
        }

        // Colliderを戻す
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // ワープ直後ではなく，少し待ってから着地音を鳴らす
        yield return new WaitForSeconds(landSoundDelay);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(2);
        }

        // 着地カメラを少し見せる
        yield return new WaitForSeconds(landCameraWaitTime);

        // 通常の一人称カメラへ戻す
        if (pitfallCameraController != null)
        {
            pitfallCameraController.EndPitfallCamera();
        }

        Debug.Log("Pitfall finished");

        isWarping = false;
    }
}