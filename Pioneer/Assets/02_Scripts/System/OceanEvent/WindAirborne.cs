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
    private float nextAirborneTime;
    private CreatureBase creature;
    public bool CanBeLifted => isActiveAndEnabled && !isAirborne && Time.time >= nextAirborneTime
        && (creature == null || !creature.IsDead)
        && (GameManager.Instance == null || !GameManager.Instance.IsGameResultActive);

    public bool IsAirborne => isAirborne;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        creature = GetComponent<CreatureBase>();
        rb = GetComponent<Rigidbody>();
    }

    public void ApplyAirborne(float height, float duration, Vector3 windDirection, float horizontalDistance = 2f)
    {
        if (!CanBeLifted || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        nextAirborneTime = Time.time + Mathf.Max(0.1f, duration) + 3f;

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

        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(startPosition, out NavMeshHit start, Mathf.Abs(agent.baseOffset) + 0.5f, filter))
        {
            isAirborne = false;
            yield break;
        }
        Vector3 destination = start.position + flatDirection * Mathf.Clamp(horizontalDistance, 0f, 0.75f);
        if (NavMesh.Raycast(start.position, destination, out NavMeshHit edge, filter))
            destination = start.position + flatDirection * Mathf.Max(0f, Vector3.Distance(start.position, edge.position) - 0.05f);
        Vector3 endPosition = new Vector3(destination.x, startPosition.y, destination.z);
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

        float totalDuration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < totalDuration)
        {
            if ((creature != null && creature.IsDead)
                || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive))
            {
                landingPosition = new Vector3(transform.position.x, startPosition.y, transform.position.z);
                break;
            }
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / totalDuration);

            Vector3 nextPosition = GetQuadraticBezierPoint(t, startPosition, middlePosition, endPosition);
            ApplyPosition(nextPosition);

            yield return null;
        }

        ApplyPosition(landingPosition);

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
        Vector3 restoredPosition = transform.position;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.SamplePosition(restoredPosition, out NavMeshHit hit, Mathf.Abs(agent.baseOffset) + 0.5f, filter))
        {
            agent.Warp(hit.position);
            // Warp uses the surface height; keep the character's original visual/body height.
            transform.position = new Vector3(hit.position.x, restoredPosition.y, hit.position.z);
        }
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
