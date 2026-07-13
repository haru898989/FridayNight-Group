using UnityEngine;

public class StoneTabletDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Transform doorObject;
    [SerializeField] private Vector3 openOffset = new Vector3(0.0f, 3.0f, 0.0f);
    [SerializeField] private float openSpeed = 3.0f;

    private Vector3 closePosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    /// <summary>
    /// ドアの初期位置と開いた位置を設定する関数
    /// </summary>
    private void Start()
    {
        // doorObjectが未設定なら自分自身を動かす
        if (doorObject == null)
        {
            doorObject = transform;
        }

        // 閉じた位置と開いた位置を保存する
        closePosition = doorObject.position;
        openPosition = closePosition + openOffset;
    }

    /// <summary>
    /// ドアを開く関数
    /// </summary>
    public void OpenDoor()
    {
        // ドアを開く状態にする
        isOpen = true;
        Debug.Log("Stone tablet door opened");
    }

    /// <summary>
    /// ドアを開いた位置へ移動させる関数
    /// </summary>
    private void Update()
    {
        // 開く状態でなければ処理しない
        if (isOpen == false)
        {
            return;
        }

        // ドアを開いた位置へ少しずつ移動させる
        doorObject.position = Vector3.MoveTowards(
            doorObject.position,
            openPosition,
            openSpeed * Time.deltaTime
        );
    }
}