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
        if (!isActiveAndEnabled) return;
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(duration);

        stunCoroutine = null;
        ClearStun();
    }

    public void ClearStun()
    {
        bool wasStunned = isStunned;
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = null;
        isStunned = false;
        if (!wasStunned) return;
        WindAirborne airborne = GetComponent<WindAirborne>();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh
            && (airborne == null || !airborne.IsAirborne))
            agent.isStopped = false;
    }

    private void OnDisable()
    {
        ClearStun();
    }
}
