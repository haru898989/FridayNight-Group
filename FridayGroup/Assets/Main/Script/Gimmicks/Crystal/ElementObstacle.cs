using UnityEngine;

public class ElementObstacle : GimmickBase
{
    [Header("Element Obstacle Settings")]
    [SerializeField] private CrystalElement requiredElement = CrystalElement.Fire;
    [SerializeField] private bool isDestroyOnSuccess = true;
    [SerializeField] private GameObject targetObject;

    /// <summary>
    /// プレイヤーが属性障害物に触れたときに呼ばれる関数
    /// </summary>
    protected override void OnPlayerHit(GameObject playerObject)
    {
        // プレイヤーからCrystalControllerを取得する
        CrystalController crystalController = playerObject.GetComponent<CrystalController>();

        // CrystalControllerが付いていない場合は、属性判定ができないので処理を終了する
        if (crystalController == null)
        {
            Debug.LogWarning("CrystalController is not set on player");
            return;
        }

        // 現在の属性が、障害物に必要な属性と一致しているか確認する
        if (crystalController.GetCurrentElement() == requiredElement)
        {
            ActivateObstacle();
        }
        else
        {
            // 属性が違う場合は障害物を突破できない
            Debug.Log("Wrong element");
        }
    }

    /// <summary>
    /// 必要な属性が一致したときに、障害物を突破する関数
    /// </summary>
    private void ActivateObstacle()
    {
        Debug.Log("Element obstacle activated : " + requiredElement);

        // 操作対象が設定されている場合は、そのオブジェクトを対象にする
        GameObject objectToControl = targetObject;

        // 操作対象が未設定の場合は、このオブジェクト自身を対象にする
        if (objectToControl == null)
        {
            objectToControl = gameObject;
        }

        // 成功時に削除する設定なら、対象オブジェクトを削除する
        if (isDestroyOnSuccess)
        {
            Destroy(objectToControl);
        }
        else
        {
            // 削除しない場合は非表示にして、装置起動のような挙動にする
            objectToControl.SetActive(false);
        }
    }
}