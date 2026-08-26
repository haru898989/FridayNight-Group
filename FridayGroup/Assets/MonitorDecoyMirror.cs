using UnityEngine;
using Fusion;

/// <summary>
/// 自分が操作しているプレイヤーの位置と向きを
/// 監視用マップ上のデコイに反映する
/// </summary>
public class MonitorDecoyMirror : MonoBehaviour
{
    [Header("追従するプレイヤー")]
    [SerializeField]
    private Transform sourcePlayer;

    [Header("元の地下マップの基準点")]
    [SerializeField]
    private Transform realMazeOrigin;

    [Header("監視用地下マップの基準点")]
    [SerializeField]
    private Transform monitorMazeOrigin;

    private void Update()
    {
        // まだプレイヤーを取得していない場合
        if (sourcePlayer == null)
        {
            FindLocalPlayer();
        }
    }

    private void LateUpdate()
    {
        if (sourcePlayer == null ||
            realMazeOrigin == null ||
            monitorMazeOrigin == null)
        {
            return;
        }

        // 元マップ上でのプレイヤーの相対位置
        Vector3 localPosition =
            realMazeOrigin.InverseTransformPoint(sourcePlayer.position);

        // 監視用マップの同じ位置へ移動
        transform.position =
            monitorMazeOrigin.TransformPoint(localPosition);

        // 向きもコピー
        Quaternion relativeRotation =
            Quaternion.Inverse(realMazeOrigin.rotation) *
            sourcePlayer.rotation;

        transform.rotation =
            monitorMazeOrigin.rotation *
            relativeRotation;
    }

    /// <summary>
    /// 自分が操作権を持っているPlayerを探す
    /// </summary>
    private void FindLocalPlayer()
    {
        GameObject[] players =
            GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            NetworkObject networkObject =
                player.GetComponentInParent<NetworkObject>();

            if (networkObject != null &&
                networkObject.HasInputAuthority)
            {
                sourcePlayer = networkObject.transform;

                Debug.Log(
                    $"監視用デコイの追従対象を取得しました: {sourcePlayer.name}"
                );

                return;
            }
        }
    }
}