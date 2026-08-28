using UnityEngine;

/// <summary>
/// 相手プレイヤーが送ったスタンプを画面上に表示します。
/// 相手が画面外にいる場合は、その方向の画面端へスタンプを固定します。
/// </summary>
public sealed class StampIndicatorUI : MonoBehaviour
{
    public static StampIndicatorUI Instance { get; private set; }

    [SerializeField] private RectTransform indicatorRoot;
    [SerializeField] private GameObject[] stampIcons;
    [SerializeField] private float screenMargin = 70f;
    [SerializeField] private float playerHeightOffset = 2.3f;
    [SerializeField] private Vector2 sentFeedbackPosition = new Vector2(0f, 180f);

    private RectTransform canvasRect;
    private Transform trackedPlayer;
    private Camera trackedCamera;
    private float visibleUntil;
    private bool followsPlayer;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        Instance = this;

        if (indicatorRoot == null)
        {
            indicatorRoot = transform as RectTransform;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.transform as RectTransform;
        }

        if (indicatorRoot != null)
        {
            originalScale = indicatorRoot.localScale;
        }

        for (int i = 0; i < stampIcons.Length; i++)
        {
            GameObject icon = stampIcons[i];
            if (icon == null)
            {
                continue;
            }

            RectTransform iconRect = icon.transform as RectTransform;
            if (iconRect != null)
            {
                iconRect.anchoredPosition = Vector2.zero;
            }

            icon.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= visibleUntil)
        {
            HideAllIcons();
            return;
        }

        if (followsPlayer && trackedPlayer != null)
        {
            UpdateTrackedPlayerPosition();
        }

        if (indicatorRoot != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.06f;
            indicatorRoot.localScale = originalScale * pulse;
        }
    }

    public void ShowRemoteStamp(Transform sender, Camera localCamera, int stampIndex, float duration)
    {
        trackedPlayer = sender;
        trackedCamera = localCamera;
        followsPlayer = sender != null && localCamera != null;
        visibleUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        ShowOnly(stampIndex);
        UpdateTrackedPlayerPosition();
    }

    public void ShowSentFeedback(int stampIndex, float duration = 0.8f)
    {
        trackedPlayer = null;
        trackedCamera = null;
        followsPlayer = false;
        visibleUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        ShowOnly(stampIndex);

        if (indicatorRoot != null)
        {
            indicatorRoot.anchoredPosition = sentFeedbackPosition;
        }
    }

    private void UpdateTrackedPlayerPosition()
    {
        if (!followsPlayer || trackedPlayer == null || trackedCamera == null ||
            indicatorRoot == null || canvasRect == null)
        {
            return;
        }

        Vector3 worldPosition = trackedPlayer.position + Vector3.up * playerHeightOffset;
        Vector3 screenPosition = trackedCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0f)
        {
            screenPosition.x = Screen.width - screenPosition.x;
            screenPosition.y = Screen.height - screenPosition.y;
        }

        screenPosition.x = Mathf.Clamp(screenPosition.x, screenMargin, Screen.width - screenMargin);
        screenPosition.y = Mathf.Clamp(screenPosition.y, screenMargin, Screen.height - screenMargin);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out Vector2 localPosition))
        {
            indicatorRoot.anchoredPosition = localPosition;
        }
    }

    private void ShowOnly(int stampIndex)
    {
        for (int i = 0; i < stampIcons.Length; i++)
        {
            if (stampIcons[i] != null)
            {
                stampIcons[i].SetActive(i == stampIndex);
            }
        }

        if (indicatorRoot != null)
        {
            indicatorRoot.localScale = originalScale * 1.2f;
        }
    }

    private void HideAllIcons()
    {
        for (int i = 0; i < stampIcons.Length; i++)
        {
            if (stampIcons[i] != null && stampIcons[i].activeSelf)
            {
                stampIcons[i].SetActive(false);
            }
        }

        if (indicatorRoot != null)
        {
            indicatorRoot.localScale = originalScale;
        }
    }
}
