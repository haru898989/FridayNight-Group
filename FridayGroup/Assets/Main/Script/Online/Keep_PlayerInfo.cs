using UnityEngine;

public class Keep_PlayerInfo : MonoBehaviour
{
    /// <summary>
    /// シーンを移動してもプレイヤー情報を保持するための初期化関数
    /// </summary>
    private void Awake()
    {
        // シーンを移動してもこのオブジェクトを削除しない
        DontDestroyOnLoad(gameObject);
    }
}