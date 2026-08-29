using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NPCStampIndicatorUI : MonoBehaviour
{
    public static NPCStampIndicatorUI Instance { get; private set; }

    [Header("表示するUI")]
    [SerializeField] private Image stampImage;

    [Header("スタンプ画像の順番")]
    // 0: ギミック発見
    // 1: トラップにかかった
    // 2: 感圧板を踏んだ
    // 3: 落とし穴に落ちた
    [SerializeField] private Sprite[] stampSprites;

    [Header("表示設定")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float npcHeadHeight = 3.0f;
    [SerializeField] private float displayTime = 2.0f;
    [SerializeField] private float edgeMargin = 80.0f;

    private Transform targetNpc;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        Instance = this;

        if (stampImage != null)
        {
            stampImage.gameObject.SetActive(false);
        }
    }

    public void Show(int stampIndex, Transform npcTransform)
    {
        if (stampImage == null || npcTransform == null)
        {
            return;
        }

        if (stampIndex < 0 || stampIndex >= stampSprites.Length)
        {
            Debug.LogWarning("NPCスタンプ番号が不正です: " + stampIndex);
            return;
        }

        targetNpc = npcTransform;
        stampImage.sprite = stampSprites[stampIndex];
        stampImage.gameObject.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterTime());
    }

    private void LateUpdate()
    {
        if (targetNpc == null || stampImage == null || !stampImage.gameObject.activeSelf)
        {
            return;
        }

        Camera cameraToUse = viewCamera != null ? viewCamera : Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        Vector3 worldPosition =
            targetNpc.position + Vector3.up * npcHeadHeight;

        Vector3 screenPosition =
            cameraToUse.WorldToScreenPoint(worldPosition);

        // NPCがカメラの後ろなら、画面端へ向きを反転する
        if (screenPosition.z < 0)
        {
            screenPosition.x = Screen.width - screenPosition.x;
            screenPosition.y = Screen.height - screenPosition.y;
        }

        // 画面外なら、NPCがいる方向の画面端に固定する
        screenPosition.x = Mathf.Clamp(
            screenPosition.x,
            edgeMargin,
            Screen.width - edgeMargin
        );

        screenPosition.y = Mathf.Clamp(
            screenPosition.y,
            edgeMargin,
            Screen.height - edgeMargin
        );

        stampImage.rectTransform.position = screenPosition;
    }

    private IEnumerator HideAfterTime()
    {
        yield return new WaitForSeconds(displayTime);

        if (stampImage != null)
        {
            stampImage.gameObject.SetActive(false);
        }

        targetNpc = null;
        hideCoroutine = null;
    }
}