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
        Action  //指示された行動
    }

    public override void Spawned()
    {
        base.Spawned();
        agent = GetComponent<NavMeshAgent>();

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;

        currentState = NPCState.Patrol;
    }

    public void ChangeState(NPCState nextState)
    {
        currentState = nextState;
    }

    public void SetTArget(Vector3 pos)
    {
        targetPosition = pos;
        ChangeState(NPCState.MoveToTarget);
    }

}
