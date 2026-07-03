using UnityEngine;

public class StageController : MonoBehaviour
{
    [Header("Stage Warp Points")]
    [SerializeField] private Transform pitfallWarpPoint;

    /// <summary>
    /// 落とし穴で移動する先の座標を返す関数
    /// </summary>
    public Vector3 GetPitfallWarpPosition()
    {
        // ワープ先が設定されていない場合は警告を出し、原点座標を返す
        if (pitfallWarpPoint == null)
        {
            Debug.LogWarning("Pitfall warp point is not set");
            return Vector3.zero;
        }

        // PitfallWarpPointの位置を、落とし穴の移動先座標として返す
        return pitfallWarpPoint.position;
    }
}