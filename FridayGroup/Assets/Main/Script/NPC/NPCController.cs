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

    private Collider targetGimmick;

    private Pitfall pendingPitfall;
    private bool waitingForPitfallCommand;

    private readonly HashSet<Pitfall> handledPitfalls = new HashSet<Pitfall>();

    // 処理済みの落とし穴を、現在の探索範囲で通知済みかを記録する。
    private readonly HashSet<Pitfall> notifiedHandledPitfalls =
        new HashSet<Pitfall>();

    public override void FixedUpdateNetwork()
    {
        if(Object == null || !Object.HasStateAuthority)
        {
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

                targetGimmick = null;
                ChangeState(NPCState.Patrol);
                return;
            }
        }

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
        
        agent.SetDestination(player.position);
    }

    //停止
    void StopMoving()
    {
        if (agent == null)
            return;

        agent.ResetPath();
    }

    //ギミックを探す
    bool SearchGimmick()
    {
        Collider[] hits =
            Physics.OverlapSphere(transform.position, searchRadius);

        HashSet<Pitfall> handledPitfallsFoundThisSearch =
            new HashSet<Pitfall>();

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
                // 一度落ちた穴にはもう向かわない・もう落ちない。
                // ただし、再び探索範囲へ入った時は「ギミック発見」を送る。
                handledPitfallsFoundThisSearch.Add(pitfallOnHit);

                if (notifiedHandledPitfalls.Add(pitfallOnHit))
                {
                    NotifyGimmickFound();
                }

                continue;
            }

            // 高さ差が大きい別階層のギミックは除外する
            float heightDiff = Mathf.Abs(hit.transform.position.y - transform.position.y);
            if (heightDiff >= 3.0f)
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

        // 一度探索範囲の外へ出た穴は、再発見時に再通知できるようにする。
        notifiedHandledPitfalls.RemoveWhere(
            pitfall => !handledPitfallsFoundThisSearch.Contains(pitfall)
        );

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
