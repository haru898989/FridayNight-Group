using System.Collections.Generic;
using UnityEngine;

public class TwoPlayerDoor : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private PressurePlate firstPlate;
    [SerializeField] private PressurePlate secondPlate;
    [SerializeField] private string puzzleId = "two-player-door-1";
    [SerializeField] private int channelId = 1;
    [SerializeField] private int requiredPlateCount = 2;

    [Header("Sliding Door Settings")]
    [SerializeField] private Transform doorPanel;
    [SerializeField] private Vector3 openOffset = new Vector3(0.0f, 2.2f, 0.0f);

    [Header("Legacy Door Hinge Settings")]
    [SerializeField] private Transform leftHinge;
    [SerializeField] private Transform rightHinge;
    [SerializeField] private float openAngle = 90.0f;
    [SerializeField] private float openSpeed = 120.0f;
    [SerializeField] private float slideSpeed = 2.5f;

    private Vector3 panelClosePosition;
    private Vector3 panelOpenPosition;
    private Quaternion leftCloseRotation;
    private Quaternion rightCloseRotation;
    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;

    private bool isOpen = false;
    private PressurePlate[] scenePlates;
    private float nextPlateRefreshTime;

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
    /// 左右のドアの閉じた角度と開いた角度を設定する関数
    /// </summary>
    private void Start()
    {
        if (doorPanel != null)
        {
            panelClosePosition = doorPanel.localPosition;
            panelOpenPosition = panelClosePosition + openOffset;
        }

        // 旧Prefabも動かせるよう、ヒンジが設定されている場合は回転値を保存する。
        if (leftHinge != null && rightHinge != null)
        {
            leftCloseRotation = leftHinge.localRotation;
            rightCloseRotation = rightHinge.localRotation;

            leftOpenRotation = leftCloseRotation * Quaternion.Euler(0.0f, openAngle, 0.0f);
            rightOpenRotation = rightCloseRotation * Quaternion.Euler(0.0f, -openAngle, 0.0f);
        }

        RefreshPressurePlates();
    }

    /// <summary>
    /// 2つの感圧版の状態を確認し，ドアを開閉する関数
    /// </summary>
    private void Update()
    {
        // 一度開いた扉は閉じない。開くまでは同じPuzzleIdの感圧板を自動検索する。
        if (!isOpen)
        {
            if (Time.time >= nextPlateRefreshTime)
            {
                RefreshPressurePlates();
                nextPlateRefreshTime = Time.time + 1.0f;
            }

            isOpen = CountDistinctActivators() >= requiredPlateCount;

            if (isOpen && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(7);
            }
        }

        RotateDoor();
    }

    /// <summary>
    /// 左右のドアを開いた角度または閉じた角度へ回転させる関数
    /// </summary>
    private void RotateDoor()
    {
        if (doorPanel != null)
        {
            Vector3 targetPosition = isOpen ? panelOpenPosition : panelClosePosition;
            doorPanel.localPosition = Vector3.MoveTowards(
                doorPanel.localPosition,
                targetPosition,
                slideSpeed * Time.deltaTime
            );
        }

        if (leftHinge == null || rightHinge == null)
        {
            return;
        }

        Quaternion leftTargetRotation;
        Quaternion rightTargetRotation;

        // 開く場合は開いた角度，閉じる場合は元の角度を目標にする
        if (isOpen)
        {
            leftTargetRotation = leftOpenRotation;
            rightTargetRotation = rightOpenRotation;
        }
        else
        {
            leftTargetRotation = leftCloseRotation;
            rightTargetRotation = rightCloseRotation;
        }

        // 左右のヒンジを少しずつ回転させる
        leftHinge.localRotation = Quaternion.RotateTowards(
            leftHinge.localRotation,
            leftTargetRotation,
            openSpeed * Time.deltaTime
        );

        rightHinge.localRotation = Quaternion.RotateTowards(
            rightHinge.localRotation,
            rightTargetRotation,
            openSpeed * Time.deltaTime
        );
    }

    private void RefreshPressurePlates()
    {
        scenePlates = FindObjectsOfType<PressurePlate>();
    }

    private int CountDistinctActivators()
    {
        HashSet<int> activatorIds = new HashSet<int>();

        AddActivator(firstPlate, activatorIds);
        AddActivator(secondPlate, activatorIds);

        if (scenePlates == null)
        {
            return activatorIds.Count;
        }

        for (int i = 0; i < scenePlates.Length; i++)
        {
            PressurePlate plate = scenePlates[i];
            if (plate == firstPlate || plate == secondPlate)
            {
                continue;
            }

            if (IsMatchingPressedPlate(plate))
            {
                activatorIds.Add(plate.ActivatorId);
            }
        }

        return activatorIds.Count;
    }

    private bool IsMatchingPressedPlate(PressurePlate plate)
    {
        return plate != null
            && plate.ChannelId == channelId
            && plate.PuzzleId == puzzleId
            && plate.IsPressed();
    }

    private void AddActivator(PressurePlate plate, HashSet<int> activatorIds)
    {
        if (IsMatchingPressedPlate(plate))
        {
            activatorIds.Add(plate.ActivatorId);
        }
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
