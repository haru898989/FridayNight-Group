using UnityEngine;

public class PlayerWarp : MonoBehaviour
{
    private Rigidbody playerRigidbody;

    /// <summary>
    /// プレイヤーのRigidbodyを取得する関数
    /// </summary>
    private void Awake()
    {
        // ワープ時に速度を止めるため、Rigidbodyを取得しておく
        playerRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// x, y, z の座標を受け取り、プレイヤーを指定位置へワープさせる関数
    /// </summary>
    public void WarpToPosition(float x, float y, float z)
    {
        // 引数で受け取った座標から、移動先のVector3を作成する
        Vector3 targetPosition = new Vector3(x, y, z);

        // Rigidbodyがある場合は、物理挙動を考慮して座標を変更する
        if (playerRigidbody != null)
        {
            // ワープ前に速度と回転を止め、移動後に勢いが残らないようにする
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            // Rigidbodyの位置を指定座標へ変更する
            playerRigidbody.position = targetPosition;
        }
        else
        {
            // Rigidbodyがない場合は、Transformの座標を直接変更する
            transform.position = targetPosition;
        }
    }
}