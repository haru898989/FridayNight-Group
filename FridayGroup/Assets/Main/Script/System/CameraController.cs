using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Camera Move Settings")]
    [SerializeField] private float moveTime = 0.5f;

    private Coroutine moveCoroutine;

    /// <summary>
    /// カメラを指定したTransformの位置と角度へ移動させる関数
    /// </summary>
    public void MoveToCameraPoint(Transform cameraPoint)
    {
        // カメラ位置が設定されていない場合は処理しない
        if (cameraPoint == null)
        {
            return;
        }

        // すでに移動中なら前の移動処理を止める
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        // カメラ移動処理を開始する
        moveCoroutine = StartCoroutine(MoveCamera(cameraPoint));
    }

    /// <summary>
    /// カメラを滑らかに指定位置へ移動させる関数
    /// </summary>
    private IEnumerator MoveCamera(Transform cameraPoint)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = cameraPoint.position;
        Quaternion targetRotation = cameraPoint.rotation;

        float timer = 0.0f;

        // moveTime秒かけてカメラを移動させる
        while (timer < moveTime)
        {
            timer += Time.deltaTime;

            float rate = timer / moveTime;

            transform.position = Vector3.Lerp(startPosition, targetPosition, rate);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rate);

            yield return null;
        }

        // 最後に正確な位置と角度へ合わせる
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}