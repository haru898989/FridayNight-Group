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
    protected const int MoveForwardStampIndex = 4;
    protected const int MoveBackwardStampIndex = 5;
    protected const int MoveLeftStampIndex = 6;
    protected const int MoveRightStampIndex = 7;
    

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

    public void SendNpcStamp(int stampIndex)
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        RPC_ShowNpcStamp(stampIndex);
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
        Stop,  //止まる
        DirectionMove,
        Monitor
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
                DropHeldLantern();
                ChangeState(NPCState.Stop);
                break;

            case StampCommand.Patrol:
                ChangeState(NPCState.Patrol);
                break;

            case StampCommand.Action:
                if (this is NPCController actionController &&
                    actionController.TryStartMonitorAction(true))
                {
                    break;
                }

                ChangeState(NPCState.Action);
                break;

            case StampCommand.MoveForward:
            case StampCommand.MoveBackward:
            case StampCommand.MoveLeft:
            case StampCommand.MoveRight:
                if (this is NPCController directionController)
                {
                    directionController.MoveToDeadEnd(command);
                }
                break;

            case StampCommand.SolveOtherGimmick:
                if (this is NPCController gimmickController)
                {
                    gimmickController.SolveAnotherGimmick();
                }
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

        agent.isStopped = true;
        agent.ResetPath();

        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Teleport(hit.position, transform.rotation);
        }

        agent.Warp(hit.position);
        agent.nextPosition = hit.position;
        agent.isStopped = false;
        Physics.SyncTransforms();
        
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (Object == null || !Object.HasStateAuthority || heldObject != null)
        {
            return;
        }

        GameObject pickup = FindPickupRoot(other);
        if (!IsLantern(pickup))
        {
            return;
        }

        RPC_PickupLantern(pickup.transform.position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PickupLantern(Vector3 pickupPosition)
    {
        if (heldObject != null)
        {
            return;
        }

        GameObject lantern = FindLanternNear(pickupPosition);
        if (lantern == null)
        {
            return;
        }

        heldObject = lantern;
        heldObject.transform.SetParent(transform, true);
        heldObject.transform.localPosition = new Vector3(0f, 1.2f, 0.6f);
        heldObject.transform.localRotation = Quaternion.identity;
        SetHeldObjectCollidersEnabled(heldObject, false);
    }

    public void DropHeldLantern()
    {
        if (Object == null || !Object.HasStateAuthority || heldObject == null)
        {
            return;
        }

        RPC_DropLantern(CalculateLanternDropPosition());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DropLantern(Vector3 dropPosition)
    {
        if (heldObject == null)
        {
            Light carriedLight = GetComponentInChildren<Light>(true);
            if (carriedLight != null)
            {
                heldObject = FindPickupRoot(carriedLight.GetComponentInParent<Collider>());
            }
        }

        if (heldObject == null)
        {
            return;
        }

        GameObject lantern = heldObject;
        heldObject = null;
        lantern.transform.SetParent(null, true);
        lantern.transform.position = dropPosition;
        lantern.transform.rotation = Quaternion.identity;
        SetHeldObjectCollidersEnabled(lantern, true);
    }

    private Vector3 CalculateLanternDropPosition()
    {
        Vector3 origin = transform.position + transform.forward * 0.55f + Vector3.up;
        if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                4f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.08f;
        }

        return transform.position + transform.forward * 0.55f;
    }

    private static GameObject FindPickupRoot(Collider other)
    {
        Transform current = other != null ? other.transform : null;
        while (current != null)
        {
            if (current.CompareTag("Pickup"))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private static bool IsLantern(GameObject target)
    {
        return target != null &&
               target.CompareTag("Pickup") &&
               target.GetComponentInChildren<Light>(true) != null;
    }

    private static GameObject FindLanternNear(Vector3 position)
    {
        GameObject[] pickups = GameObject.FindGameObjectsWithTag("Pickup");
        GameObject nearest = null;
        float nearestDistance = 1.5f * 1.5f;

        foreach (GameObject pickup in pickups)
        {
            if (!IsLantern(pickup))
            {
                continue;
            }

            float distance = (pickup.transform.position - position).sqrMagnitude;
            if (distance <= nearestDistance)
            {
                nearest = pickup;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

}
