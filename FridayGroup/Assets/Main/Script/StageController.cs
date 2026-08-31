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

        Vector3 warpPosition = pitfallWarpPoint.position;

        // 3-4は入口マスの角に着地するとカプセルが隣の壁へ食い込むため、
        // 地下入口（CSVの39番）の空きマス中央へ着地させる。
        string selectedStage = StageSelectionContext.SelectedStageResourcePath;
        if (selectedStage == "Stage/Stage3/3-3" ||
            selectedStage == "Stage/Stage3/3-4")
        {
            warpPosition.x = 3.0f;
            warpPosition.z = 8.0f;
        }

        return warpPosition;
    }
}
