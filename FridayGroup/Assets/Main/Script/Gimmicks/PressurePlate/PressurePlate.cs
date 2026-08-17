using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string puzzleId = "two-player-door-1";
    [SerializeField] private int channelId = 1;

    private bool isPressed = false;
    private int activatorId = int.MinValue;

    public string PuzzleId => puzzleId;
    public int ChannelId => channelId;
    public int ActivatorId => activatorId;

    /// <summary>
    /// CSV番号の一の位を連動チャンネルとして設定し、同じチャンネルの色を反映する。
    /// </summary>
    public void ConfigureChannel(int channel, Color channelColor)
    {
        channelId = channel;
        puzzleId = $"csv-channel-{channel}";
        ApplyChannelColor(channelColor);
    }

    /// <summary>
    /// コンポーネント追加時にColliderをTriggerに設定する関数
    /// </summary>
    private void Reset()
    {
        // 感圧版はプレイヤーが乗ったことをTriggerで判定する
        Collider plateCollider = GetComponent<Collider>();
        plateCollider.isTrigger = true;
    }

    /// <summary>
    /// プレイヤーが感圧版に乗ったときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        GameObject playerObject = FindPlayerRoot(other);

        // 一度押された感圧板は、プレイヤーが離れても押された状態を維持する。
        if (!isPressed && playerObject != null)
        {
            isPressed = true;
            activatorId = GetActivatorId(playerObject);
            Debug.Log(gameObject.name + " pressed");

            // 感圧版を踏んだときの効果音を再生する
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(5);
            }
        }
    }

    /// <summary>
    /// プレイヤーが感圧版から離れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // ラッチ式のため、離れても解除しない。
    }

    /// <summary>
    /// 感圧版が押されているかを返す関数
    /// </summary>
    public bool IsPressed()
    {
        // 現在の押下状態を返す
        return isPressed;
    }

    /// <summary>
    /// ルートまたは子ColliderからPlayerBaseを探し、オンラインの両プレイヤーを判定する関数
    /// </summary>
    private GameObject FindPlayerRoot(Collider other)
    {
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(playerTag) || current.GetComponent<PlayerBase>() != null)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private static int GetActivatorId(GameObject playerObject)
    {
        NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.InputAuthority != PlayerRef.None)
        {
            return networkObject.InputAuthority.PlayerId;
        }

        return playerObject.GetInstanceID();
    }

    private void ApplyChannelColor(Color channelColor)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", channelColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", channelColor);
            }
        }
    }
}
