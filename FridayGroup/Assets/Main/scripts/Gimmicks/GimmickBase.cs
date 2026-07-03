using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class GimmickBase : MonoBehaviour
{
    [Header("共通設定")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isOnlyOnce = true;

    private bool isActivated = false;

    private void Reset()
    {
        Collider gimmickCollider = GetComponent<Collider>();
        gimmickCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated && isOnlyOnce)
        {
            return;
        }

        if (other.CompareTag(playerTag))
        {
            isActivated = true;
            OnPlayerHit(other.gameObject);
        }
    }

    protected abstract void OnPlayerHit(GameObject playerObject);
}