using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NPCBase : PlayerBase
{
    protected override bool UsePlayerInput => false;

    protected NavMeshAgent agent;

    protected NPCState currentState;

    protected Transform player;

    protected Vector3 targetPosition;

    public enum NPCState
    {
        Patrol,  //自由行動
        FollowPlayer,  //プレイヤーについてくる
        MoveToTarget,  //指示された場所へ移動
        Action,  //指示された行動
        Stop  //止まる
    }


    public override void Spawned()
    {
        base.Spawned();

        agent = GetComponent<NavMeshAgent>();

        if(agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogError("NPCをNavMesh上に配置できませんでした．");
        }

        currentState = NPCState.Patrol;
        FindPlayer();
    }

    protected void FindPlayer()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
        {
            player = obj.transform;
            Debug.Log("NPCがPlayerを発見: " + obj.name);
        }
    }

    public virtual void ReceiveStampCommand(StampCommand command)
    {
        Debug.Log($"NPCがスタンプ命令を受信:{command}");

        switch(command)
        {
            case StampCommand.FollowPlayer:
                ChangeState(NPCState.FollowPlayer);
                break;

            case StampCommand.Stop:
                ChangeState(NPCState.Stop);
                break;

            case StampCommand.MoveToTarget:
                ChangeState(NPCState.MoveToTarget);
                break;

            case StampCommand.Action:
                ChangeState(NPCState.Action);
                break;
        }
    }

    public void ChangeState(NPCState nextState)
    {
        currentState = nextState;
    }

    public void SetTarget(Vector3 pos)
    {
        targetPosition = pos;
        ChangeState(NPCState.MoveToTarget);
    }

}
