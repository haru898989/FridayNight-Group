using UnityEngine;

public class TwoPlayerDoor : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private PressurePlate firstPlate;
    [SerializeField] private PressurePlate secondPlate;

    [Header("Door Hinge Settings")]
    [SerializeField] private Transform leftHinge;
    [SerializeField] private Transform rightHinge;
    [SerializeField] private float openAngle = 90.0f;
    [SerializeField] private float openSpeed = 120.0f;

    private Quaternion leftCloseRotation;
    private Quaternion rightCloseRotation;
    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;

    private bool isOpen = false;
    private bool previousIsOpen = false;

    /// <summary>
    /// 左右のドアの閉じた角度と開いた角度を設定する関数
    /// </summary>
    private void Start()
    {
        // 必要な参照が設定されていない場合は処理しない
        if (leftHinge == null || rightHinge == null)
        {
            Debug.LogWarning("LeftHinge or RightHinge is not set");
            return;
        }

        // 現在の角度を閉じた角度として保存する
        leftCloseRotation = leftHinge.localRotation;
        rightCloseRotation = rightHinge.localRotation;

        // 左右のドアを外側へ開く角度を設定する
        leftOpenRotation = leftCloseRotation * Quaternion.Euler(0.0f, openAngle, 0.0f);
        rightOpenRotation = rightCloseRotation * Quaternion.Euler(0.0f, -openAngle, 0.0f);
    }

    /// <summary>
    /// 2つの感圧版の状態を確認し，ドアを開閉する関数
    /// </summary>
    private void Update()
    {
        // 必要な参照が設定されていない場合は処理しない
        if (firstPlate == null || secondPlate == null || leftHinge == null || rightHinge == null)
        {
            return;
        }

        // 前回の開閉状態を保存する
        previousIsOpen = isOpen;

        // 2つの感圧版が同時に押されている場合だけ開く
        isOpen = firstPlate.IsPressed() && secondPlate.IsPressed();

        // 開閉状態が変わった瞬間だけ効果音を鳴らす
        if (previousIsOpen != isOpen)
        {
            if (SoundManager.Instance != null)
            {
                if (isOpen)
                {
                    SoundManager.Instance.PlaySE(7);
                }
                else
                {
                    SoundManager.Instance.PlaySE(8);
                }
            }
        }

        // 現在の状態に合わせてドアを回転させる
        RotateDoor();
    }

    /// <summary>
    /// 左右のドアを開いた角度または閉じた角度へ回転させる関数
    /// </summary>
    private void RotateDoor()
    {
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
}