using System;
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

    public override void FixedUpdateNetwork()
    {
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
        }
    }

    //巡回処理
    void Patrol()
    {
        if (SearchGimmick())
        {
            ChangeState(NPCState.Action);
            return;
        }

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
                                      patrolRadius,
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
        agent.SetDestination(targetPosition);
    }

    //アクション実行
    void Action()
    {
        //Debug.Log("アクション実行");

        ChangeState(NPCState.Patrol);
    }

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
            MoveToTarget(nearestHit.transform.position);
            return true;
        }

        return false;
    }

    //探索範囲変更
    private void ChangeReserchSize(float num)
    {
        searchRadius = num;
    }
}
