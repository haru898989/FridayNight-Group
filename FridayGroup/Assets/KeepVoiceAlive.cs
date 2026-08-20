using UnityEngine;

public class KeepVoiceAlive : MonoBehaviour
{
    private void Awake()
    {
        // シーン移動（onlineconnect -> map）してもボイス管理オブジェクトを破棄しない
        DontDestroyOnLoad(gameObject);
    }
}