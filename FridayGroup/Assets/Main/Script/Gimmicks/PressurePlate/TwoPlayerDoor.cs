using UnityEngine;

public class TwoPlayerDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private PressurePlate firstPlate;
    [SerializeField] private PressurePlate secondPlate;
    [SerializeField] private float openHeight = 3.0f;
    [SerializeField] private float openSpeed = 3.0f;

    private Vector3 closePosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    /// <summary>
    /// ドアの閉じた位置と開いた位置を設定する関数
    /// </summary>
    private void Start()
    {
        // 現在位置を閉じた位置として保存する
        closePosition = transform.position;

        // 閉じた位置から上方向にopenHeight分移動した場所を開いた位置にする
        openPosition = closePosition + Vector3.up * openHeight;
    }

    /// <summary>
    /// 2つの感圧版の状態を確認し、ドアを開閉する関数
    /// </summary>
    private void Update()
    {
        // 感圧版が設定されていない場合は処理しない
        if (firstPlate == null || secondPlate == null)
        {
            return;
        }

        // 2つの感圧版が同時に押されている場合だけドアを開く
        isOpen = firstPlate.IsPressed() && secondPlate.IsPressed();

        // ドアを現在の状態に合わせて移動させる
        MoveDoor();
    }

    /// <summary>
    /// ドアを開いた位置または閉じた位置へ移動させる関数
    /// </summary>
    private void MoveDoor()
    {
        Vector3 targetPosition;

        // isOpenがtrueなら開いた位置、falseなら閉じた位置を目標にする
        if (isOpen)
        {
            targetPosition = openPosition;
        }
        else
        {
            targetPosition = closePosition;
        }

        // 目標位置へ少しずつ移動させる
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            openSpeed * Time.deltaTime
        );
    }
}