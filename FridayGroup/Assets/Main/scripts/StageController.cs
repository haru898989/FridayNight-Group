using UnityEngine;

public class StageController : MonoBehaviour
{
    [Header("Stage Warp Points")]
    [SerializeField] private Transform pitfallWarpPoint;

    public Vector3 GetPitfallWarpPosition()
    {
        if (pitfallWarpPoint == null)
        {
            Debug.LogWarning("Pitfall warp point is not set");
            return Vector3.zero;
        }

        return pitfallWarpPoint.position;
    }
}