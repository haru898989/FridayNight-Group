using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollingRock : MonoBehaviour
{
    [Header("Rock Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    private bool isRolling = false;
    private Rigidbody rockRigidbody;

    /// <summary>
    /// 大岩のRigidbodyを取得する関数
    /// </summary>
    private void Start()
    {
        // 大岩を物理演算で動かすため、Rigidbodyを取得する
        rockRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 大岩が発動中のとき、指定した方向へ移動させる関数
    /// </summary>
    private void FixedUpdate()
    {
        // 発動していない場合は移動処理を行わない
        if (isRolling == false)
        {
            return;
        }

        // 移動方向と速度から、次に移動する座標を計算する
        Vector3 nextPosition = rockRigidbody.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;

        // Rigidbodyを使って大岩を移動させる
        rockRigidbody.MovePosition(nextPosition);
    }

    /// <summary>
    /// 外部から呼び出して、大岩を動き始めさせる関数
    /// </summary>
    public void StartRolling()
    {
        Debug.Log("Rolling rock started");

        // 大岩の移動状態を有効にする
        isRolling = true;
    }

    /// <summary>
    /// 大岩の移動を停止する関数
    /// </summary>
    private void StopRolling()
    {
        Debug.Log("Rolling rock stopped");

        // 大岩の移動状態を無効にする
        isRolling = false;
    }

    /// <summary>
    /// 大岩が他のオブジェクトに衝突したときに呼ばれる関数
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Wallタグのオブジェクトに当たった場合、大岩を停止する
        if (collision.gameObject.CompareTag("Wall"))
        {
            StopRolling();
        }
    }
}