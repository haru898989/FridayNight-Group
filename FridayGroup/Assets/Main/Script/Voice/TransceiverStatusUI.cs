using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the local player's transceiver state.
/// Sprites are loaded from Resources/TransceiverON and Resources/TransceiverOff.
/// </summary>
public class TransceiverStatusUI : MonoBehaviour
{
    private const string OnIconResourcePath = "TransceiverON";
    private const string OffIconResourcePath = "TransceiverOff";

    private static TransceiverStatusUI instance;

    private Image statusImage;
    private Sprite onIcon;
    private Sprite offIcon;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        CreateStatusUI();
        UpdateStatusIcon();
    }

    private void Update()
    {
        UpdateStatusIcon();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (instance == null)
        {
            new GameObject("TransceiverStatusUI").AddComponent<TransceiverStatusUI>();
        }
    }

    private static TransceiverController GetLocalPlayerController()
    {
        TransceiverController[] controllers =
            FindObjectsByType<TransceiverController>(FindObjectsSortMode.None);

        foreach (TransceiverController controller in controllers)
        {
            NetworkObject networkObject = controller.Object;
            if (networkObject != null && networkObject.HasInputAuthority)
            {
                return controller;
            }
        }

        return null;
    }

    private void CreateStatusUI()
    {
        GameObject canvasObject = new GameObject(
            "TransceiverStatusCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject(
            "TransceiverStatusImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(96f, -20f);
        rect.sizeDelta = new Vector2(80f, 80f);

        statusImage = imageObject.GetComponent<Image>();
        onIcon = Resources.Load<Sprite>(OnIconResourcePath);
        offIcon = Resources.Load<Sprite>(OffIconResourcePath);
        statusImage.preserveAspect = true;
        statusImage.raycastTarget = false;

        if (onIcon == null || offIcon == null)
        {
            Debug.LogWarning(
                "Transceiver icons: Resources/TransceiverON or " +
                "Resources/TransceiverOff was not found."
            );
        }
    }

    private void UpdateStatusIcon()
    {
        if (statusImage == null)
        {
            return;
        }

        TransceiverController controller = GetLocalPlayerController();
        TransceiverHolder holder = controller != null
            ? controller.GetComponent<TransceiverHolder>()
            : null;

        if (holder == null || !holder.HasTransceiver())
        {
            statusImage.enabled = false;
            return;
        }

        statusImage.sprite = controller.IsLocalTransmitting ? onIcon : offIcon;
        statusImage.enabled = statusImage.sprite != null;
    }
}
