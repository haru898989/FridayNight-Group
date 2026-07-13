using UnityEngine;

public class CrystalController : MonoBehaviour
{
    [Header("Crystal Settings")]
    [SerializeField] private CrystalElement elementType = CrystalElement.Fire;

    [Header("Visual Settings")]
    [SerializeField] private Renderer crystalRenderer;
    [SerializeField] private Material fireMaterial;
    [SerializeField] private Material iceMaterial;
    [SerializeField] private Material thunderMaterial;

    /// <summary>
    /// クリスタルの見た目を属性に合わせて変更する関数
    /// </summary>
    private void Start()
    {
        // Rendererが未設定なら自分自身から取得する
        if (crystalRenderer == null)
        {
            crystalRenderer = GetComponent<Renderer>();
        }

        // 属性に合わせて見た目を変更する
        ChangeVisualByElement();
    }

    /// <summary>
    /// このクリスタルの属性を返す関数
    /// </summary>
    public CrystalElement GetElementType()
    {
        // クリスタルに設定されている属性を返す
        return elementType;
    }

    /// <summary>
    /// 属性に合わせてMaterialを変更する関数
    /// </summary>
    private void ChangeVisualByElement()
    {
        // Rendererがない場合は処理しない
        if (crystalRenderer == null)
        {
            return;
        }

        // 火属性の見た目に変更する
        if (elementType == CrystalElement.Fire && fireMaterial != null)
        {
            crystalRenderer.material = fireMaterial;
        }

        // 氷属性の見た目に変更する
        if (elementType == CrystalElement.Ice && iceMaterial != null)
        {
            crystalRenderer.material = iceMaterial;
        }

        // 電気属性の見た目に変更する
        if (elementType == CrystalElement.Thunder && thunderMaterial != null)
        {
            crystalRenderer.material = thunderMaterial;
        }
    }
}