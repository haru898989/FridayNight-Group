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

    private Renderer[] markerRenderers;

    private void Awake()
    {
        markerRenderers = GetComponentsInChildren<Renderer>(true);
        Shader markerShader = Shader.Find("Unlit/Color");

        if (markerShader != null)
        {
            Material markerMaterial = new Material(markerShader);
            markerMaterial.color = new Color(0.1f, 1.0f, 0.25f);

            for (int i = 0; i < markerRenderers.Length; i++)
            {
                markerRenderers[i].material = markerMaterial;
            }
        }

        SetMarkerVisible(false);
    }

    private void Update()
    {
        if (sourcePlayer != null)
        {
            Vector3 sourceLocalPosition =
                realMazeOrigin.InverseTransformPoint(sourcePlayer.position);

            if (!IsInsideRealMaze(sourceLocalPosition))
            {
                sourcePlayer = null;
                SetMarkerVisible(false);
            }
        }

        // まだプレイヤーを取得していない場合
        if (sourcePlayer == null)
        {
            FindMazePlayer();
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
    /// 実際の地下迷路にいるPlayerを探す
    /// </summary>
    private void FindMazePlayer()
    {
        if (realMazeOrigin == null)
        {
            return;
        }

        Transform bestPlayer = null;
        float bestHeightDifference = float.MaxValue;
        NetworkRunner runner = OnlineStageFlow.Instance != null
            ? OnlineStageFlow.Instance.Runner
            : null;

        // 両クライアントで同じプレイヤーを取得できるよう、
        // Fusionが管理している参加プレイヤー一覧を優先して参照する。
        if (runner != null && runner.IsRunning)
        {
            foreach (PlayerRef playerRef in runner.ActivePlayers)
            {
                if (runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
                {
                    ConsiderMazePlayer(
                        playerObject.transform,
                        ref bestPlayer,
                        ref bestHeightDifference
                    );
                }
            }
        }

        // プレイヤーオブジェクトの登録直後など、Fusion側からまだ取得できない瞬間の補助。
        if (bestPlayer == null)
        {
            NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
            foreach (NetworkObject networkObject in networkObjects)
            {
                if (networkObject.GetComponent<PlayerBase>() == null)
                {
                    continue;
                }

                ConsiderMazePlayer(
                    networkObject.transform,
                    ref bestPlayer,
                    ref bestHeightDifference
                );
            }
        }

        if (bestPlayer != null)
        {
            sourcePlayer = bestPlayer;
            SetMarkerVisible(true);

            Debug.Log(
                $"監視用デコイの追従対象を取得しました: {sourcePlayer.name}"
            );
        }
    }

    private void ConsiderMazePlayer(
        Transform candidate,
        ref Transform bestPlayer,
        ref float bestHeightDifference
    )
    {
        if (candidate == null)
        {
            return;
        }

        Vector3 mazeLocalPosition =
            realMazeOrigin.InverseTransformPoint(candidate.position);

        if (!IsInsideRealMaze(mazeLocalPosition))
        {
            return;
        }

        // 地下床上のプレイヤー中心はおよそY=1。最も地下に近い人を選ぶ。
        float heightDifference = Mathf.Abs(mazeLocalPosition.y - 1.0f);
        if (heightDifference < bestHeightDifference)
        {
            bestHeightDifference = heightDifference;
            bestPlayer = candidate;
        }
    }

    private static bool IsInsideRealMaze(Vector3 localPosition)
    {
        return localPosition.x >= 0.0f && localPosition.x <= 29.0f &&
               localPosition.z >= 0.0f && localPosition.z <= 29.0f &&
               localPosition.y >= 0.0f && localPosition.y <= 2.75f;
    }

    private void SetMarkerVisible(bool isVisible)
    {
        if (markerRenderers == null)
        {
            return;
        }

        for (int i = 0; i < markerRenderers.Length; i++)
        {
            markerRenderers[i].enabled = isVisible;
        }
    }
}
