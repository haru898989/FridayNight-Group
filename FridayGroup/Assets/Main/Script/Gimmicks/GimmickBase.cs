using Fusion;
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

        // プレイヤーの子Colliderが接触した場合も、PlayerBaseを持つルートまで辿る。
        PlayerBase player = other.GetComponentInParent<PlayerBase>();
        GameObject playerObject = player != null ? player.gameObject : other.gameObject;

        // リモートプレイヤーは各クライアント上でUntaggedになるため、
        // ローカルプレイヤーのギミックだけを発動する。
        if (playerObject.CompareTag(playerTag))
        {
            // 発動済みにして、同じギミックが何度も動かないようにする
            isActivated = true;

            NetworkObject playerNetworkObject =
            playerObject.GetComponent<NetworkObject>();

        if (LogGenerator.Instance != null &&
            playerNetworkObject != null &&
            playerNetworkObject.InputAuthority != PlayerRef.None)
        {
            LogGenerator.Instance.SendEventLog
            (
                $"Player_{playerNetworkObject.InputAuthority.PlayerId}",
                "State",
                $"{GetType().Name}_activated",
                transform.position
            );
        }

            // 子クラスで実装したギミックごとの処理を呼び出す
            OnPlayerHit(playerObject);
        }
    }

    /// <summary>
    /// プレイヤーがギミックに触れたときの処理を子クラスで実装するための関数
    /// </summary>
    protected abstract void OnPlayerHit(GameObject playerObject);
}
