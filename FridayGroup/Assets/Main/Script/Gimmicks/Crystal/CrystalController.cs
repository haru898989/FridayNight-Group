using UnityEngine;

public class CrystalController : MonoBehaviour
{
    [Header("Crystal Settings")]
    [SerializeField] private CrystalElement currentElement = CrystalElement.Fire;

    /// <summary>
    /// 毎フレーム入力を確認し、クリスタルの属性を切り替える関数
    /// </summary>
    private void Update()
    {
        // 1キーを押したら炎属性に切り替える
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeElement(CrystalElement.Fire);
        }

        // 2キーを押したら氷属性に切り替える
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeElement(CrystalElement.Ice);
        }

        // 3キーを押したら雷属性に切り替える
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeElement(CrystalElement.Thunder);
        }
    }

    /// <summary>
    /// クリスタルの属性を指定された属性に変更する関数
    /// </summary>
    public void ChangeElement(CrystalElement newElement)
    {
        // 現在の属性を新しい属性に変更する
        currentElement = newElement;

        // 現在の属性を確認しやすいようにConsoleへ表示する
        Debug.Log("Crystal element changed : " + currentElement);
    }

    /// <summary>
    /// 現在のクリスタル属性を取得する関数
    /// </summary>
    public CrystalElement GetCurrentElement()
    {
        // 現在設定されている属性を返す
        return currentElement;
    }
}