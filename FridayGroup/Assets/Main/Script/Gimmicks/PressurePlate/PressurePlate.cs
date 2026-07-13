using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool isPressed = false;

    /// <summary>
    /// コンポーネント追加時にColliderをTriggerに設定する関数
    /// </summary>
    private void Reset()
    {
        // 感圧版はプレイヤーが乗ったことをTriggerで判定する
        Collider plateCollider = GetComponent<Collider>();
        plateCollider.isTrigger = true;
    }

    /// <summary>
    /// プレイヤーが感圧版に乗ったときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグのオブジェクトが乗った場合だけ押された状態にする
        if (other.CompareTag(playerTag))
        {
            isPressed = true;
            Debug.Log(gameObject.name + " pressed");

            // 感圧版を踏んだときの効果音を再生する
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(5);
            }
        }
    }

    /// <summary>
    /// プレイヤーが感圧版から離れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Playerタグのオブジェクトが離れた場合，押されていない状態に戻す
        if (other.CompareTag(playerTag))
        {
            isPressed = false;
            Debug.Log(gameObject.name + " released");

            // 感圧版から離れたときの効果音を再生する
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(6);
            }
        }
    }

    /// <summary>
    /// 感圧版が押されているかを返す関数
    /// </summary>
    public bool IsPressed()
    {
        // 現在の押下状態を返す
        return isPressed;
    }
}