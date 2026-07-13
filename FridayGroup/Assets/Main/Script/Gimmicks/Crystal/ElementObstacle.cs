using UnityEngine;

public class ElementObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] private CrystalElement requiredElement = CrystalElement.Fire;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool destroyOnCorrect = true;

    private bool isCleared = false;

    /// <summary>
    /// 対象オブジェクトが未設定の場合，自分自身を対象にする関数
    /// </summary>
    private void Start()
    {
        // targetObjectが未設定なら，このオブジェクト自身を解除対象にする
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }

    /// <summary>
    /// TriggerでPlayerが触れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 接触したオブジェクトで解除判定を行う
        TryClearObstacle(other.gameObject);
    }

    /// <summary>
    /// CollisionでPlayerが触れたときに呼ばれる関数
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトで解除判定を行う
        TryClearObstacle(collision.gameObject);
    }

    /// <summary>
    /// Playerが持っているクリスタル属性を確認し，正しければ障害物を解除する関数
    /// </summary>
    private void TryClearObstacle(GameObject playerObject)
    {
        // すでに解除済みなら処理しない
        if (isCleared)
        {
            return;
        }

        // Playerタグ以外なら処理しない
        if (playerObject.CompareTag("Player") == false)
        {
            return;
        }

        PlayerCrystalHolder holder = playerObject.GetComponent<PlayerCrystalHolder>();

        // PlayerCrystalHolderが付いていない場合は処理しない
        if (holder == null)
        {
            Debug.LogWarning("PlayerCrystalHolder is not attached to Player");
            return;
        }

        // クリスタル属性をまだ持っていない場合は処理しない
        if (holder.HasCrystalElement() == false)
        {
            Debug.Log("Player does not have crystal element");
            return;
        }

        CrystalElement currentElement = holder.GetCurrentElement();

        // 必要属性と一致しているか確認する
        if (currentElement == requiredElement)
        {
            isCleared = true;

            Debug.Log("Correct element. Obstacle cleared: " + requiredElement);

            // 正解なら対象オブジェクトを消す
            if (targetObject != null)
            {
                if (destroyOnCorrect)
                {
                    Destroy(targetObject);
                }
                else
                {
                    targetObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.Log("Wrong element. Need: " + requiredElement + " / Current: " + currentElement);
        }
    }
}