using UnityEngine;

public class RollingRockTrigger : GimmickBase
{
    [Header("Trigger Settings")]
    [SerializeField] private RollingRock rollingRock;

    protected override void OnPlayerHit(GameObject playerObject)
    {
        if (rollingRock == null)
        {
            Debug.LogWarning("RollingRock is not set");
            return;
        }

        rollingRock.StartRolling();
    }
}