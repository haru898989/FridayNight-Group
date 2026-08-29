using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCBase : PlayerBase
{
    protected override bool UsePlayerInput => false;

    protected NavMeshAgent agent;

    protected NPCState currentState;

    protected Transform player;

    protected Vector3 targetPosition;
    private Coroutine trapStopCoroutine;

    //NPCStamp
    private const int GimmickFoundStampIndex = 0;
    private const int TrapTriggeredStampIndex = 1;
    private const int PressurePlateStampIndex = 2;
    private const int PitfallFallenStampIndex = 3;
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowNpcStamp(int stampIndex)
    {
        ShowStamp(stampIndex);

        if(NPCStampIndicatorUI.Instance != null)
        {
            NPCStampIndicatorUI.Instance.Show(stampIndex, transform);
        }
    }

    public void NotifyGimmickFound()
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        RPC_ShowNpcStamp(GimmickFoundStampIndex);
    }

    public void NotifyPressurePlatePressed()
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        RPC_ShowNpcStamp(PressurePlateStampIndex);
    }

    public void NotifyTrapTriggered()
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }
        RPC_ShowNpcStamp(TrapTriggeredStampIndex);
    }

    public void NotifyPitfallFallen()
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        RPC_ShowNpcStamp(PitfallFallenStampIndex);
    }

    public void StopByTrap(float stopSeconds)
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }
        if(trapStopCoroutine != null)
        {
            StopCoroutine(trapStopCoroutine);
        }

        trapStopCoroutine = StartCoroutine(StopByTrapRoution(stopSeconds));
    }

    private IEnumerator StopByTrapRoution(float stopSeconds)
    {
        ChangeState(NPCState.Stop);

        if(agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(stopSeconds);

        if(agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        ChangeState(NPCState.Patrol);
        trapStopCoroutine = null;
    }



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
        PlayerBase[] players = FindObjectsOfType<PlayerBase>();

        foreach (PlayerBase foundPlayer in players)
        {
            if(foundPlayer.gameObject != gameObject)
            {
                player = foundPlayer.transform;
                return;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReceiveStampCommand(int command)
    {
        ReceiveStampCommand((StampCommand)command);
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

            case StampCommand.Patrol:
                ChangeState(NPCState.Patrol);
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

    public bool IsFollowingPlayer =>
        currentState == NPCState.FollowPlayer;

    public void SetTarget(Vector3 pos)
    {
        targetPosition = pos;
        ChangeState(NPCState.MoveToTarget);
    }

    public void WarpToNavMesh(Vector3 destination)
    {
        if(Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        if(agent == null)
        {
            Debug.LogWarning("NPCのNavMesAgentがありません");
            return;
        }

        if(!NavMesh.SamplePosition(
            destination,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            Debug.LogWarning($"NPCのワープ先付近にNavMEshがありません:{destination}");
            return;
        }

        agent.Warp(hit.position);
        
    }

}
