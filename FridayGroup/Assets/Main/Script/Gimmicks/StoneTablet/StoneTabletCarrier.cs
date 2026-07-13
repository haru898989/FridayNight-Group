using UnityEngine;

public class StoneTabletCarrier : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private bool canPickupTablet = true;

    [Header("Drop Settings")]
    [SerializeField] private KeyCode dropKey = KeyCode.R;

    private bool hasTablet = false;
    private int currentTabletId = -1;
    private GameObject currentTabletObject = null;

    /// <summary>
    /// 石板を拾えるプレイヤーかを返す関数
    /// </summary>
    public bool CanPickupTablet()
    {
        // このプレイヤーが石板を拾えるか返す
        return canPickupTablet;
    }

    /// <summary>
    /// 石板を持っているかを返す関数
    /// </summary>
    public bool HasTablet()
    {
        return hasTablet;
    }

    /// <summary>
    /// 持っている石板IDを返す関数
    /// </summary>
    public int GetCurrentTabletId()
    {
        return currentTabletId;
    }

    /// <summary>
    /// 石板を拾う関数
    /// </summary>
    public void PickupTablet(int tabletId, GameObject tabletObject)
    {
        hasTablet = true;
        currentTabletId = tabletId;
        currentTabletObject = tabletObject;
    }

    /// <summary>
    /// 持っている石板を消費する関数
    /// </summary>
    public void ClearTablet()
    {
        hasTablet = false;
        currentTabletId = -1;
        currentTabletObject = null;
    }

    /// <summary>
    /// 設定されたキーで石板を捨てる関数
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropTablet();
        }
    }

    /// <summary>
    /// 持っている石板をプレイヤーの前に戻す関数
    /// </summary>
    private void DropTablet()
    {
        if (hasTablet == false || currentTabletObject == null)
        {
            return;
        }

        Vector3 dropPosition = transform.position + transform.forward * 1.0f;
        dropPosition.y = 0.2f;

        currentTabletObject.transform.position = dropPosition;
        currentTabletObject.SetActive(true);

        Debug.Log("Dropped tablet ID: " + currentTabletId);

        hasTablet = false;
        currentTabletId = -1;
        currentTabletObject = null;
    }
}