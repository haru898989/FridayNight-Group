using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ローカルプレイヤーのボイスチャット送信を切り替えます。
/// M キーまたはゲームパッドの Select / View ボタンで操作します。
/// </summary>
public class VoiceChatMuteController : MonoBehaviour
{
    private static VoiceChatMuteController instance;

    public bool IsMuted { get; private set; }

    private Image statusImage;
    private Sprite voiceOnSprite;
    private Sprite voiceOffSprite;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        voiceOnSprite = Resources.Load<Sprite>("VoiceOn");
        voiceOffSprite = Resources.Load<Sprite>("VoiceOff");

        if (voiceOnSprite == null || voiceOffSprite == null)
        {
            Debug.LogWarning("Voice mute: Resources/VoiceOn または Resources/VoiceOff が見つかりません。");
        }

        CreateStatusUI();
        UpdateStatusUI();
    }

    private void Update()
    {
        if (WasMuteTogglePressed())
        {
            ToggleMute();
        }
    }

    public void ToggleMute()
    {
        Recorder recorder = GetLocalPlayerRecorder();

        if (recorder == null)
        {
            Debug.LogWarning("Voice mute: ローカルプレイヤーの Recorder が見つかりません。");
            return;
        }

        IsMuted = !IsMuted;
        recorder.TransmitEnabled = !IsMuted;
        UpdateStatusUI();

        Debug.Log(IsMuted ? "Voice chat muted." : "Voice chat unmuted.");
    }

    private static bool WasMuteTogglePressed()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            return true;
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.selectButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static Recorder GetLocalPlayerRecorder()
    {
        VoiceNetworkObject[] voiceObjects =
            FindObjectsByType<VoiceNetworkObject>(FindObjectsSortMode.None);

        foreach (VoiceNetworkObject voiceObject in voiceObjects)
        {
            // PlayerBase と同じ Input Authority を優先してローカルプレイヤーを判定する。
            if (voiceObject.Object != null &&
                voiceObject.Object.HasInputAuthority &&
                voiceObject.RecorderInUse != null)
            {
                return voiceObject.RecorderInUse;
            }
        }

        // Shared Mode で State Authority を持つ構成にも対応する。
        foreach (VoiceNetworkObject voiceObject in voiceObjects)
        {
            if (voiceObject.IsLocal && voiceObject.RecorderInUse != null)
            {
                return voiceObject.RecorderInUse;
            }
        }

        Recorder[] recorders = FindObjectsByType<Recorder>(FindObjectsSortMode.None);
        foreach (Recorder recorder in recorders)
        {
            if (recorder.enabled && recorder.gameObject.activeInHierarchy)
            {
                return recorder;
            }
        }

        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        new GameObject("VoiceChatMuteController").AddComponent<VoiceChatMuteController>();
    }

    private void CreateStatusUI()
    {
        GameObject canvasObject = new GameObject(
            "VoiceMuteStatusCanvas",
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
            "VoiceMuteStatusImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(64f, 64f);

        statusImage = imageObject.GetComponent<Image>();
        statusImage.preserveAspect = true;
        statusImage.raycastTarget = false;
    }

    private void UpdateStatusUI()
    {
        if (statusImage != null)
        {
            statusImage.sprite = IsMuted ? voiceOffSprite : voiceOnSprite;
        }
    }
}

