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
        // 大岩を物理演算で動かすため，Rigidbodyを取得する
        rockRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 大岩が発動中のとき，指定した方向へ移動させる関数
    /// </summary>
    private void FixedUpdate()
    {
        // 発動していない場合は移動しない
        if (isRolling == false)
        {
            return;
        }

        // 移動方向と速度から次の位置を計算する
        Vector3 nextPosition = rockRigidbody.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;

        // Rigidbodyを使って大岩を移動させる
        rockRigidbody.MovePosition(nextPosition);
    }

    /// <summary>
    /// 大岩を動かし始める関数
    /// </summary>
    public void StartRolling()
    {
        Debug.Log("Rolling rock started");

        // 大岩が動き始める効果音を再生する
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(3);
        }

        isRolling = true;
    }

    /// <summary>
    /// 大岩を停止させる関数
    /// </summary>
    private void StopRolling()
    {
        // すでに止まっている場合は処理しない
        if (isRolling == false)
        {
            return;
        }

        Debug.Log("Rolling rock stopped");

        isRolling = false;

        // 停止時に速度も0にする
        rockRigidbody.velocity = Vector3.zero;
        rockRigidbody.angularVelocity = Vector3.zero;

        // 大岩が止まる効果音を再生する
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(4);
        }
    }

    /// <summary>
    /// 大岩が他のオブジェクトに衝突したときに呼ばれる関数
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Wallタグのオブジェクトに当たった場合，大岩を停止する
        if (collision.gameObject.CompareTag("Wall"))
        {
            StopRolling();
        }
    }
}