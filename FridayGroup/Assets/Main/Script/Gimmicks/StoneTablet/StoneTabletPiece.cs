using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StoneTabletPiece : MonoBehaviour
{
    [Header("Stone Tablet Settings")]
    [SerializeField] private int tabletId = 0;

    /// <summary>
    /// 石板IDを返す関数
    /// </summary>
    public int GetTabletId()
    {
        // この石板のIDを返す
        return tabletId;
    }

    /// <summary>
    /// コンポーネント追加時にColliderをTriggerにする関数
    /// </summary>
    private void Reset()
    {
        // 石板は拾う判定をTriggerで行う
        Collider pieceCollider = GetComponent<Collider>();
        pieceCollider.isTrigger = true;
    }

    /// <summary>
    /// プレイヤーが石板に触れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグ以外なら処理しない
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        StoneTabletCarrier carrier = other.GetComponent<StoneTabletCarrier>();

        // プレイヤーが石板所持用スクリプトを持っていない場合は処理しない
        if (carrier == null)
        {
            Debug.LogWarning("StoneTabletCarrier is not attached to Player");
            return;
        }

        // 石板を拾えないプレイヤーなら処理しない
        if (carrier.CanPickupTablet() == false)
        {
            Debug.Log("This player cannot pick up stone tablets");
            return;
        }

        // すでに石板を持っている場合は拾わない
        if (carrier.HasTablet())
        {
            Debug.Log("Player already has a stone tablet");
            return;
        }

        // プレイヤーにこの石板を持たせる
        carrier.PickupTablet(tabletId, gameObject);

        Debug.Log("Picked up tablet ID: " + tabletId);

        // 拾った石板を非表示にする
        gameObject.SetActive(false);
    }
}