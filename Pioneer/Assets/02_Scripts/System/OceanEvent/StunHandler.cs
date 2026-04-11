using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StunHandler : MonoBehaviour
{
    [SerializeField] private bool isStunned = false;

    private NavMeshAgent agent;
    private Coroutine stunCoroutine;

    public bool IsStunned => isStunned;

    public void ApplyStun(float duration)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        stunCoroutine = null;
    }
}