using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapシーン上のUIへ、ローカルプレイヤーのトランシーバー状態を表示する。
/// </summary>
public class TransceiverStatusUI : MonoBehaviour
{
    [SerializeField] private Image statusImage;
    [SerializeField] private Sprite onIcon;
    [SerializeField] private Sprite offIcon;

    private void Awake()
    {
        if (statusImage != null)
        {
            statusImage.enabled = false;
        }
    }

    private void Update()
    {
        UpdateStatusIcon();
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
}
