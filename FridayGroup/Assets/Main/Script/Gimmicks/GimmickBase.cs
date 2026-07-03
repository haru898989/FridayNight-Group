using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class GimmickBase : MonoBehaviour
{
    [Header("Common Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isOnlyOnce = true;

    private bool isActivated = false;

    /// <summary>
    /// コンポーネント追加時にColliderをTriggerに設定する関数
    /// </summary>
    private void Reset()
    {
        // ギミックは接触判定をTriggerで行うため、ColliderをTriggerにする
        Collider gimmickCollider = GetComponent<Collider>();
        gimmickCollider.isTrigger = true;
    }

    /// <summary>
    /// プレイヤーがギミックのTrigger範囲に入ったときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 一度だけ発動する設定で、すでに発動済みなら処理を終了する
        if (isActivated && isOnlyOnce)
        {
            return;
        }

        // 接触したオブジェクトがPlayerタグを持っているか確認する
        if (other.CompareTag(playerTag))
        {
            // 発動済みにして、同じギミックが何度も動かないようにする
            isActivated = true;

            // 子クラスで実装したギミックごとの処理を呼び出す
            OnPlayerHit(other.gameObject);
        }
    }

    /// <summary>
    /// プレイヤーがギミックに触れたときの処理を子クラスで実装するための関数
    /// </summary>
    protected abstract void OnPlayerHit(GameObject playerObject);
}