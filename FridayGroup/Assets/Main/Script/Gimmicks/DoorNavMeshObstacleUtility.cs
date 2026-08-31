using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 扉の物理ColliderをNPC用NavMeshの動的な障害物として登録します。
/// 扉が移動・回転すると障害物も一緒に動くため、開いた後は通行できます。
/// </summary>
public static class DoorNavMeshObstacleUtility
{
    public static void Ensure(GameObject doorRoot)
    {
        if (doorRoot == null)
        {
            return;
        }

        Collider[] colliders = doorRoot.GetComponentsInChildren<Collider>(true);
        foreach (Collider doorCollider in colliders)
        {
            if (doorCollider == null || !doorCollider.enabled || doorCollider.isTrigger)
            {
                continue;
            }

            NavMeshObstacle obstacle = doorCollider.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = doorCollider.gameObject.AddComponent<NavMeshObstacle>();
            }

            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            if (doorCollider is BoxCollider boxCollider)
            {
                obstacle.center = boxCollider.center;
                obstacle.size = boxCollider.size;
            }
            else
            {
                Bounds bounds = doorCollider.bounds;
                obstacle.center = doorCollider.transform.InverseTransformPoint(bounds.center);
                obstacle.size = new Vector3(
                    SafeLocalSize(bounds.size.x, doorCollider.transform.lossyScale.x),
                    SafeLocalSize(bounds.size.y, doorCollider.transform.lossyScale.y),
                    SafeLocalSize(bounds.size.z, doorCollider.transform.lossyScale.z)
                );
            }
        }
    }

    private static float SafeLocalSize(float worldSize, float scale)
    {
        return Mathf.Abs(scale) > 0.0001f
            ? worldSize / Mathf.Abs(scale)
            : worldSize;
    }
}
