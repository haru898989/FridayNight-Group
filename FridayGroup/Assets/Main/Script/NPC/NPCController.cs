using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;


public class NPCController : NPCBase
{
    [Header("巡回設定")]
    //NPCの周囲8mいないで巡回場所探す
    public float patrolRadius = 8f;

    //目的地との距離が0.5m以下になったら到着と判断
    public float arriveDistance = 0.5f;

    [Header("探索設定")]
    //NPCから何m探す？
    public float searchRadius = 5f;

    [Header("方向スタンプ設定")]
    [SerializeField, Min(1f)] private float directionMoveBlocks = 3f;

    private Collider targetGimmick;

    private Pitfall pendingPitfall;
    private bool waitingForPitfallCommand;

    private Collider lastSolvedGimmick;
    private MonitorWatchPoint targetMonitor;
    private double nextMonitorStampTime;
    private NavMeshPath monitorPath;

    private readonly HashSet<Pitfall> handledPitfalls = new HashSet<Pitfall>();

    public override void FixedUpdateNetwork()
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        if (!canMove)
        {
            StopMoving();
            return;
        }

        //状態によって行動を変える
        switch(currentState)
        {
            case NPCState.Patrol:
                Patrol();
                break;

            case NPCState.MoveToTarget:
                MoveToTarget(targetPosition);
                break;

            case NPCState.Action:
                Action();
                break;

            case NPCState.FollowPlayer:
                FollowPlayer();
                break;

            case NPCState.Stop:
                StopMoving();
                break;

            case NPCState.DirectionMove:
                UpdateDirectionMove();
                break;

            case NPCState.Monitor:
                UpdateMonitorGuidance();
                break;
        }
    }

    private void LateUpdate()
    {
        if(Object != null && Object.HasStateAuthority)
        {
            Physics.SyncTransforms();
        }
    }

    //巡回処理
    void Patrol()
    {
        if (SearchGimmick())
        {
            ChangeState(NPCState.MoveToTarget);
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
            return;

        if (agent.pathPending) return;

        if (agent.remainingDistance > arriveDistance) return;

        const int maxTry = 20;

        for (int i = 0; i<maxTry; i++)
        {
            Vector3 randomPoint =
                transform.position +
                UnityEngine.Random.insideUnitSphere * patrolRadius;

            randomPoint.y = transform.position.y;

            NavMeshHit hit;

            if(NavMesh.SamplePosition(randomPoint,
                                      out hit,
                                      1f,
                                      NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        Debug.LogWarning("巡回地点が見つかりませんでした．");
    }

    //目的地へ移動
    void MoveToTarget(Vector3 targetPosition)
    {
        if(agent == null || !agent.isOnNavMesh)
            return;
        if(agent.pathPending)
            return;
        if (agent.remainingDistance > arriveDistance)
            return;

        StopMoving();

        if(waitingForPitfallCommand)
        {
            return;
        }

        ChangeState(NPCState.Action);
    }

    //アクション実行
    void Action()
    {
        if(waitingForPitfallCommand && pendingPitfall != null)
        {
            waitingForPitfallCommand = false;
            targetPosition = targetGimmick != null
                ? targetGimmick.bounds.center
                : pendingPitfall.transform.position;
            agent.SetDestination(targetPosition);
            ChangeState(NPCState.MoveToTarget);
            return;
        }

        if (targetMonitor != null)
        {
            EnterMonitorState();
            return;
        }

        if(targetGimmick != null)
        {
            LadderWarp ladder =
                targetGimmick.GetComponentInParent<LadderWarp>();

            if(ladder == null)
            {
                ladder =
                    targetGimmick.GetComponentInChildren<LadderWarp>();
            }

            if(ladder != null)
            {
                ladder.UseByNpc(this);

                lastSolvedGimmick = targetGimmick;
                targetGimmick = null;
                ChangeState(NPCState.Patrol);
                return;
            }

            if (targetGimmick.GetComponentInParent<PressurePlate>() != null ||
                targetGimmick.GetComponentInChildren<PressurePlate>() != null)
            {
                // 感圧板は上に立つことで解くため、その場で待機する。
                lastSolvedGimmick = targetGimmick;
                ChangeState(NPCState.Stop);
                return;
            }
        }

        lastSolvedGimmick = targetGimmick;
        targetGimmick = null;
        ChangeState(NPCState.Patrol);
    }

    //Player追跡
    void FollowPlayer()
    {   
        //Playerが見つかっていなければ探す
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if(agent == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    //停止
    void StopMoving()
    {
        if (agent == null)
            return;

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    /// <summary>
    /// NPCの現在向きを基準に、指定方向へ数マス移動します。
    /// 新しい方向指示が来た場合は現在の移動先を即座に上書きします。
    /// </summary>
    public void MoveToDeadEnd(StampCommand command)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        float snappedYaw = Mathf.Round(transform.eulerAngles.y / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0f, snappedYaw, 0f);
        Physics.SyncTransforms();

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 direction;

        switch (command)
        {
            case StampCommand.MoveBackward:
                direction = -forward;
                break;
            case StampCommand.MoveLeft:
                direction = -right;
                break;
            case StampCommand.MoveRight:
                direction = right;
                break;
            default:
                direction = forward;
                break;
        }

        pendingPitfall = null;
        waitingForPitfallCommand = false;
        targetGimmick = null;
        targetMonitor = null;

        Vector3 destination = transform.position + direction * directionMoveBlocks;
        if (NavMesh.Raycast(transform.position, destination, out NavMeshHit wallHit, NavMesh.AllAreas))
        {
            destination = wallHit.position - direction * Mathf.Max(agent.radius, 0.2f);
        }

        if (NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 1.5f, NavMesh.AllAreas))
        {
            destination = destinationHit.position;
        }

        agent.isStopped = false;
        agent.ResetPath();
        targetPosition = destination;
        agent.SetDestination(destination);
        ChangeState(NPCState.DirectionMove);
    }

    private void UpdateDirectionMove()
    {
        if (agent == null || !agent.isOnNavMesh || agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= arriveDistance)
        {
            StopMoving();
            ChangeState(NPCState.Stop);
        }
    }

    /// <summary>
    /// 近くにある、直前とは別のギミックを探して向かいます。
    /// 監視モニターが近い場合は、3-3の案内役としてモニターを優先します。
    /// </summary>
    public void SolveAnotherGimmick()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (TryStartMonitorAction(false))
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius * 2f);
        float nearestDistance = float.PositiveInfinity;
        Collider nearest = null;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Gimmick") || hit == lastSolvedGimmick)
            {
                continue;
            }

            if (hit.GetComponentInParent<BearTrap>() != null ||
                hit.GetComponentInParent<Pitfall>() != null)
            {
                continue;
            }

            float heightDifference = Mathf.Abs(hit.bounds.center.y - transform.position.y);
            if (heightDifference >= 1.5f)
            {
                continue;
            }

            float distance = (hit.bounds.center - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = hit;
            }
        }

        if (nearest == null)
        {
            NotifyGimmickFound();
            ChangeState(NPCState.Stop);
            return;
        }

        targetMonitor = null;
        targetGimmick = nearest;
        targetPosition = nearest.bounds.center;
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, 2f, NavMesh.AllAreas))
        {
            targetPosition = targetHit.position;
        }

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(targetPosition);
        ChangeState(NPCState.MoveToTarget);
    }

    /// <summary>
    /// 監視モニターを通常の「ギミックを解いて」指示でも使用できるようにする。
    /// nearbyOnly=true の場合は、NPCがモニター前にいるときだけ反応する。
    /// </summary>
    public bool TryStartMonitorAction(bool nearbyOnly)
    {
        if (waitingForPitfallCommand)
        {
            return false;
        }

        MonitorWatchPoint monitor = FindNearestMonitorWatchPoint();
        if (monitor == null)
        {
            return false;
        }

        Vector3 difference = monitor.transform.position - transform.position;
        difference.y = 0f;
        float horizontalDistance = difference.magnitude;

        const float monitorUseDistance = 4f;
        if (nearbyOnly && horizontalDistance > monitorUseDistance)
        {
            return false;
        }

        targetMonitor = monitor;
        targetGimmick = null;

        // モニター前にいる場合は、壁座標への経路探索を挟まず即座に監視状態へ入る。
        if (horizontalDistance <= monitorUseDistance)
        {
            EnterMonitorState();
            return true;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            targetMonitor = null;
            return false;
        }

        Vector3 awayFromMonitor = transform.position - monitor.transform.position;
        awayFromMonitor.y = 0f;
        if (awayFromMonitor.sqrMagnitude < 0.01f)
        {
            awayFromMonitor = -monitor.transform.forward;
        }

        Vector3 standingPosition =
            monitor.transform.position + awayFromMonitor.normalized * 1.2f;

        if (!NavMesh.SamplePosition(
                standingPosition,
                out NavMeshHit monitorHit,
                2f,
                NavMesh.AllAreas))
        {
            targetMonitor = null;
            return false;
        }

        targetPosition = monitorHit.position;
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(targetPosition);
        ChangeState(NPCState.MoveToTarget);
        return true;
    }

    private MonitorWatchPoint FindNearestMonitorWatchPoint()
    {
        if (StageSelectionContext.SelectedStageResourcePath != "Stage/Stage3/3-3")
        {
            return null;
        }

        MonitorWatchPoint[] monitors = FindObjectsOfType<MonitorWatchPoint>();
        MonitorWatchPoint nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (MonitorWatchPoint monitor in monitors)
        {
            float heightDifference = Mathf.Abs(monitor.transform.position.y - transform.position.y);
            if (heightDifference >= 2f)
            {
                continue;
            }

            float distance = (monitor.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = monitor;
            }
        }

        return nearest;
    }

    private void EnterMonitorState()
    {
        StopMoving();
        if (targetMonitor != null)
        {
            Vector3 lookDirection = targetMonitor.transform.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

        nextMonitorStampTime = 0f;
        ChangeState(NPCState.Monitor);
    }

    private void UpdateMonitorGuidance()
    {
        StopMoving();

        if (Runner == null || Runner.SimulationTime < nextMonitorStampTime)
        {
            return;
        }

        nextMonitorStampTime = Runner.SimulationTime + 1f;
        if (player == null)
        {
            FindPlayer();
        }

        if (monitorPath == null)
        {
            monitorPath = new NavMeshPath();
        }

        if (player == null || !TryFindMazeGoal(player.position, out Vector3 goalPosition) ||
            !NavMesh.SamplePosition(player.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(goalPosition, out NavMeshHit goalHit, 2f, NavMesh.AllAreas) ||
            !NavMesh.CalculatePath(playerHit.position, goalHit.position, NavMesh.AllAreas, monitorPath) ||
            monitorPath.status == NavMeshPathStatus.PathInvalid || monitorPath.corners.Length < 2)
        {
            return;
        }

        Vector3 nextCorner = monitorPath.corners[1];
        for (int i = 1; i < monitorPath.corners.Length; i++)
        {
            if ((monitorPath.corners[i] - playerHit.position).sqrMagnitude > 0.09f)
            {
                nextCorner = monitorPath.corners[i];
                break;
            }
        }

        Vector3 pathDirection = Vector3.ProjectOnPlane(nextCorner - playerHit.position, Vector3.up).normalized;
        Vector3 playerForward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        Vector3 playerRight = Vector3.ProjectOnPlane(player.right, Vector3.up).normalized;

        float forwardAmount = Vector3.Dot(pathDirection, playerForward);
        float rightAmount = Vector3.Dot(pathDirection, playerRight);
        int stampIndex = Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount)
            ? (forwardAmount >= 0f ? MoveForwardStampIndex : MoveBackwardStampIndex)
            : (rightAmount >= 0f ? MoveRightStampIndex : MoveLeftStampIndex);

        SendNpcStamp(stampIndex);
    }

    private static bool TryFindMazeGoal(Vector3 playerPosition, out Vector3 goalPosition)
    {
        LadderWarp[] ladders = FindObjectsOfType<LadderWarp>();
        LadderWarp nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (LadderWarp ladder in ladders)
        {
            float distance = (ladder.transform.position - playerPosition).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = ladder;
            }
        }

        goalPosition = nearest != null ? nearest.transform.position : Vector3.zero;
        return nearest != null;
    }

    public void HandlePitfallWarped()
    {
        pendingPitfall = null;
        waitingForPitfallCommand = false;
        targetGimmick = null;
        targetMonitor = null;
        StopMoving();

        ChangeState(StageSelectionContext.SelectedStageResourcePath == "Stage/Stage3/3-3"
            ? NPCState.Stop
            : NPCState.Patrol);
    }

    //ギミックを探す
    bool SearchGimmick()
    {
        Collider[] hits =
            Physics.OverlapSphere(transform.position, searchRadius);

        float nearestDistance = Mathf.Infinity;
        Collider nearestHit = null;

        foreach (Collider hit in hits)
        {
            //ギミック以外は無視する
            if (!hit.CompareTag("Gimmick"))
            {
                continue;
            }

            Pitfall pitfallOnHit = hit.GetComponentInParent<Pitfall>();
            if (pitfallOnHit == null)
            {
                pitfallOnHit = hit.GetComponentInChildren<Pitfall>();
            }

            if (pitfallOnHit != null && handledPitfalls.Contains(pitfallOnHit))
            {
                continue;
            }

            //高さの差が1m以上なら除外
            float heightDiff = Mathf.Abs(hit.transform.position.y - transform.position.y);
            if (heightDiff >= 1.0f)
            {
                continue;
            }

            //距離計算
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            //今までで一番近ければ更新
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHit = hit;
            }

        }

        if (nearestHit != null)
        {
            Debug.Log("ギミック発見!:" + nearestHit.name);
            targetPosition = nearestHit.transform.position;
            agent.SetDestination(targetPosition);
            targetGimmick = nearestHit;

            bool isPressurePlate = nearestHit.GetComponentInParent<PressurePlate>() != null;
            bool isTrap = nearestHit.GetComponentInParent<BearTrap>() != null || nearestHit.GetComponentInParent<Pitfall>() != null;

            Pitfall pitfall = nearestHit.GetComponentInParent<Pitfall>();
            if (pitfall == null)
            {
                pitfall = nearestHit.GetComponentInChildren<Pitfall>();
            }

            if (pitfall != null)
            {

                //落とし穴
                pendingPitfall = pitfall;
                waitingForPitfallCommand = true;
                targetGimmick = nearestHit;
                
                Vector3 center = nearestHit.bounds.center;
                Vector3 direction = transform.position - center;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = -transform.forward;
                }

                float npcRadius = agent != null ? agent.radius : 0.5f;

                float distance = Mathf.Max(
                    nearestHit.bounds.extents.x,
                    nearestHit.bounds.extents.z
                    ) + npcRadius + 0.2f;

                Vector3 waitingPoint = center + direction.normalized * distance;

                if (NavMesh.SamplePosition(waitingPoint, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    waitingPoint = hit.position;
                }

                targetPosition = waitingPoint;
                agent.SetDestination(targetPosition);

                NotifyGimmickFound();
                return true;
            }

            if (!isPressurePlate && !isTrap)
            {
                NotifyGimmickFound();
            }
            return true;
        }

        return false;
    }

    public void MarkPitfallAsHandled(Pitfall pitfall)
    {
        if (pitfall == null)
        {
            return;
        }

        handledPitfalls.Add(pitfall);

        if (pendingPitfall == pitfall)
        {
            pendingPitfall = null;
            waitingForPitfallCommand = false;
        }
    }

    //探索範囲変更
    private void ChangeReserchSize(float num)
    {
        searchRadius = num;
    }
}
