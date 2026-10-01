using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WindAirborne : MonoBehaviour
{
    [SerializeField] private bool isAirborne = false;

    private Coroutine airborneCoroutine;

    private NavMeshAgent agent;
    private Rigidbody rb;

    private bool cachedAgentUpdatePosition;
    private bool cachedAgentStopped;
    private bool hasAgentState;
    private Vector3 landingPosition;

    public bool IsAirborne => isAirborne;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    public void ApplyAirborne(float height, float duration, Vector3 windDirection, float horizontalDistance = 2f)
    {
        if (!isActiveAndEnabled) return;
        CancelAirborne();

        airborneCoroutine = StartCoroutine(AirborneRoutine(height, duration, windDirection, horizontalDistance));
    }

    private IEnumerator AirborneRoutine(float height, float duration, Vector3 windDirection, float horizontalDistance)
    {
        isAirborne = true;

        Vector3 startPosition = transform.position;

        Vector3 flatDirection = windDirection;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
            flatDirection = transform.forward;

        flatDirection.Normalize();

        Vector3 endPosition = startPosition + flatDirection * horizontalDistance;
        landingPosition = endPosition;

        // 포물선처럼 보이도록 중간 제어점을 사용
        Vector3 middlePosition = (startPosition + endPosition) * 0.5f + Vector3.up * height;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            cachedAgentUpdatePosition = agent.updatePosition;
            cachedAgentStopped = agent.isStopped;
            StunHandler currentStun = GetComponent<StunHandler>();
            if (currentStun != null && currentStun.IsStunned) cachedAgentStopped = false;
            hasAgentState = true;
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = false;
        }

        float totalDuration = Mathf.Max(0.01f, duration + 0.35f);
        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / totalDuration);

            Vector3 nextPosition = GetQuadraticBezierPoint(t, startPosition, middlePosition, endPosition);
            ApplyPosition(nextPosition);

            yield return null;
        }

        ApplyPosition(endPosition);

        RestoreAgent();

        isAirborne = false;
        airborneCoroutine = null;
    }

    public void CancelAirborne()
    {
        if (airborneCoroutine != null) StopCoroutine(airborneCoroutine);
        airborneCoroutine = null;
        if (isAirborne) ApplyPosition(landingPosition);
        RestoreAgent();
        isAirborne = false;
    }

    private void RestoreAgent()
    {
        if (!hasAgentState) return;
        hasAgentState = false;
        if (agent == null) return;
        agent.updatePosition = cachedAgentUpdatePosition;
        if (!agent.isActiveAndEnabled) return;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, agent.areaMask))
            agent.Warp(hit.position);
        if (agent.isOnNavMesh)
        {
            StunHandler stun = GetComponent<StunHandler>();
            agent.isStopped = cachedAgentStopped || (stun != null && stun.IsStunned);
        }
    }

    private void OnDisable()
    {
        CancelAirborne();
    }

    private Vector3 GetQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0
             + 2f * oneMinusT * t * p1
             + t * t * p2;
    }

    private void ApplyPosition(Vector3 targetPosition)
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        transform.position = targetPosition;
    }
}
