using UnityEngine;

public class PlayerCrystalHolder : MonoBehaviour
{
    [Header("Current Crystal Element")]
    [SerializeField] private CrystalElement currentElement = CrystalElement.Fire;

    private bool hasCrystalElement = false;

    /// <summary>
    /// 現在クリスタル属性を持っているかを返す関数
    /// </summary>
    public bool HasCrystalElement()
    {
        // 属性を取得済みか返す
        return hasCrystalElement;
    }

    /// <summary>
    /// 現在持っているクリスタル属性を返す関数
    /// </summary>
    public CrystalElement GetCurrentElement()
    {
        // 現在の属性を返す
        return currentElement;
    }

    /// <summary>
    /// クリスタル属性を設定する関数
    /// </summary>
    public void SetCrystalElement(CrystalElement element)
    {
        // 取得した属性を保存する
        currentElement = element;
        hasCrystalElement = true;

        Debug.Log("Player got crystal element: " + currentElement);
    }

    /// <summary>
    /// プレイヤーがクリスタルに触れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 触れた相手からCrystalControllerを取得する
        CrystalController crystal = other.GetComponent<CrystalController>();

        // クリスタルでなければ処理しない
        if (crystal == null)
        {
            return;
        }

        // クリスタルの属性を取得してPlayerに保存する
        SetCrystalElement(crystal.GetElementType());
    }
}