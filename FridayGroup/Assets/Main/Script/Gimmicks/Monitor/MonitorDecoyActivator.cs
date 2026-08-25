using UnityEngine;

public class MonitorDecoyActivator : MonoBehaviour
{
    private bool activated = false;
    
    private void Start()
    {
        Debug.Log("MonitorDecoyActivator Start");
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"MonitorDecoyActivator Trigger: {other.name}");

        if (activated)
        {
            return;
        }
        if (activated)
        {
            return;
        }

        // プレイヤーが踏んだか確認
        PlayerBase player = other.GetComponentInParent<PlayerBase>();

        if (player == null)
        {
            return;
        }

        // 非表示状態のMonitorDecoyも含めて探す
        MonitorDecoyMirror decoy =
            UnityEngine.Object.FindObjectOfType<MonitorDecoyMirror>(true);

        if (decoy == null)
        {
            Debug.LogWarning("MonitorDecoyが見つかりません。");
            return;
        }

        decoy.gameObject.SetActive(true);
        activated = true;

        Debug.Log("監視用デコイを起動しました。");
    }
}