using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField]private Transform target;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        //target = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if(target != null)
        {
            agent.SetDestination(target.position);
        }
    }
}
