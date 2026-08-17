using UnityEngine;

public class CrystalGear : MonoBehaviour
{
    [Header("Gear Settings")]
    [SerializeField] private CrystalElement requiredCrystalType;
    [SerializeField] private TwoPlayerDoor targetDoor;
    [SerializeField] private int channelId = 1;

    // この歯車がすでに作動済みかどうか
    private bool isActivated = false;

    /// <summary>
    /// プレイヤーが歯車に触れたとき、
    /// 正しいクリスタルを所持しているか確認する
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // すでに作動済みなら何もしない
        if (isActivated)
        {
            return;
        }

        PlayerCrystalHolder holder = other.GetComponent<PlayerCrystalHolder>();

        if (holder == null)
        {
            holder = other.GetComponentInParent<PlayerCrystalHolder>();
        }

        if (holder == null || !holder.HasCrystalElement())
        {
            return;
        }

        if (holder.GetCurrentElement() != requiredCrystalType)
        {
            Debug.Log("違う種類のクリスタルです");
            return;
        }
        isActivated = true;

        Debug.Log($"{requiredCrystalType} gear activated");

        holder.RemoveCrystalElement();

        if (targetDoor == null)
        {
            FindTargetDoor();
        }

        if (targetDoor != null)
        {
            targetDoor.OpenDoor();
        }
    }

    /// <summary>
    /// 同じチャンネル番号の扉を探す
    /// </summary>
    private void FindTargetDoor()
    {
        TwoPlayerDoor[] doors = FindObjectsOfType<TwoPlayerDoor>();

        foreach (TwoPlayerDoor door in doors)
        {
            if (door.ChannelId == channelId)
            {
                targetDoor = door;
                return;
            }
        }

        Debug.LogWarning($"Channel {channelId} の扉が見つかりません");
    }
}