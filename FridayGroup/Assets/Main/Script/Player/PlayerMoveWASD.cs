using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveWASD : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5.0f;

    [Header("Key Settings")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    private Rigidbody playerRigidbody;

    /// <summary>
    /// PlayerのRigidbodyを取得する関数
    /// </summary>
    private void Awake()
    {
        // Rigidbodyを使ってPlayerを動かすため取得する
        playerRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 設定されたキー入力に合わせてPlayerを移動させる関数
    /// </summary>
    private void FixedUpdate()
    {
        Vector3 moveDirection = Vector3.zero;

        // 前方向へ移動する
        if (Input.GetKey(upKey))
        {
            moveDirection += Vector3.forward;
        }

        // 後ろ方向へ移動する
        if (Input.GetKey(downKey))
        {
            moveDirection += Vector3.back;
        }

        // 左方向へ移動する
        if (Input.GetKey(leftKey))
        {
            moveDirection += Vector3.left;
        }

        // 右方向へ移動する
        if (Input.GetKey(rightKey))
        {
            moveDirection += Vector3.right;
        }

        // 斜め移動が速くなりすぎないように正規化する
        moveDirection = moveDirection.normalized;

        // 次の位置を計算する
        Vector3 nextPosition = playerRigidbody.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        // RigidbodyでPlayerを移動させる
        playerRigidbody.MovePosition(nextPosition);
    }
}