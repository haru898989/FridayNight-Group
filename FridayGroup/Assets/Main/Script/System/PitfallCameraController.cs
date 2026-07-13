using UnityEngine;
using System.Collections;

public class PitfallCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera pitfallCamera;

    [Header("Move Settings")]
    [SerializeField] private float moveTime = 0.5f;

    private AudioListener mainAudioListener;
    private AudioListener pitfallAudioListener;
    private Coroutine moveCoroutine;

    /// <summary>
    /// カメラとAudioListenerを取得し，初期状態を設定する関数
    /// </summary>
    private void Awake()
    {
        // PitfallCameraが未設定なら，自分についているCameraを取得する
        if (pitfallCamera == null)
        {
            pitfallCamera = GetComponent<Camera>();
        }

        // AudioListenerを取得する
        if (mainCamera != null)
        {
            mainAudioListener = mainCamera.GetComponent<AudioListener>();
        }

        if (pitfallCamera != null)
        {
            pitfallAudioListener = pitfallCamera.GetComponent<AudioListener>();
        }

        // 最初は通常カメラを使い，落とし穴用カメラは無効にする
        EndPitfallCamera();
    }

    /// <summary>
    /// 落とし穴演出用カメラに切り替える関数
    /// </summary>
    public void StartPitfallCamera()
    {
        // 通常カメラを無効にする
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
        }

        if (mainAudioListener != null)
        {
            mainAudioListener.enabled = false;
        }

        // 落とし穴用カメラを有効にする
        if (pitfallCamera != null)
        {
            pitfallCamera.enabled = true;
        }

        if (pitfallAudioListener != null)
        {
            pitfallAudioListener.enabled = true;
        }
    }

    /// <summary>
    /// 通常の一人称カメラに戻す関数
    /// </summary>
    public void EndPitfallCamera()
    {
        // 落とし穴用カメラを無効にする
        if (pitfallCamera != null)
        {
            pitfallCamera.enabled = false;
        }

        if (pitfallAudioListener != null)
        {
            pitfallAudioListener.enabled = false;
        }

        // 通常カメラを有効にする
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }

        if (mainAudioListener != null)
        {
            mainAudioListener.enabled = true;
        }
    }

    /// <summary>
    /// 落とし穴用カメラを指定した位置と角度へ移動させる関数
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
    /// 落とし穴用カメラを滑らかに移動させる関数
    /// </summary>
    private IEnumerator MoveCamera(Transform cameraPoint)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = cameraPoint.position;
        Quaternion targetRotation = cameraPoint.rotation;

        float timer = 0.0f;

        // moveTime秒かけて指定位置へ移動する
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