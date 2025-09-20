using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerBase : CreatureBase
{
    // 승무원 공통 설정
    public LayerMask targetLayer;

    // 상태 관리
    public enum CrewState { Wandering, Idle, Attacking, Chasing }
    protected CrewState currentState = CrewState.Wandering;

    // 이동 및 대기 시간
    protected float moveDuration = 2f;
    protected float idleDuration = 4f;
    protected float stateTimer = 0f;
    private Vector3 moveDirection;

    // 공격 관리
    protected UnityEngine.Transform target;
    protected bool isShowingAttackBox = false;
    protected Coroutine attackRoutine;

    // 추격 시스템 관련 변수
    protected float chaseRange = 8f;
    protected bool isChasing = false;
    protected float chaseUpdateInterval = 0.2f;
    protected float lastChaseUpdate = 0f;

    // 수리 관련 공통 변수
    [Header("수리 설정")]
    public bool isRepairing = false;
    protected DefenseObject targetRepairObject;
    protected int repairAmount = 30;
    protected bool isSecondPriorityStarted = false;

    // 개별 경계 탐색 설정
    [Header("개별 경계 탐색 설정")]
    public int personalRayCount = 8; // 개인별 Ray 개수 (45도씩)
    public float personalMaxDistance = 50f; // 개인별 최대 탐색 거리

    protected Vector3 personalEdgePoint;
    protected bool hasFoundPersonalEdge = false;

    // NavMeshAgent 공통 사용
    protected NavMeshAgent agent;

    public virtual void Start()
    {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = speed;
            agent.acceleration = 12f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.5f;
        }
    }

    protected bool IsTargetInFOV()
    {
        if (target == null || fov == null)
            return false;

        fov.DetectTargets(targetLayer);
        return fov.visibleTargets.Contains(target);
    }

    protected virtual void Wander()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                SetRandomDestination();
            }
        }
        else
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            EnterIdleState();
        }
    }

    protected virtual void Idle()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            EnterWanderingState();
        }
    }

    protected virtual void EnterWanderingState()
    {
        SetRandomDirection();
        currentState = CrewState.Wandering;
        stateTimer = moveDuration;
        Debug.Log($"{gameObject.name} - 랜덤 방향으로 이동 시작");
    }

    protected virtual void EnterIdleState()
    {
        currentState = CrewState.Idle;
        stateTimer = idleDuration;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        Debug.Log($"{gameObject.name} - 대기 상태로 전환");
    }

    protected virtual void EnterChasingState()
    {
        currentState = CrewState.Chasing;
        isChasing = true;
        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 추격 상태로 전환");
    }

    protected void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    protected void SetRandomDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        Vector3 randomDirection = Random.insideUnitSphere * 5f;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    protected void LookAtTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    protected virtual void ValidateCurrentTarget()
    {
        if (target != null)
        {
            CommonBase targetBase = target.GetComponent<CommonBase>();
            if (targetBase != null && targetBase.IsDead)
            {
                Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 타겟 {target.name}이 죽었습니다. 새로운 타겟을 찾습니다.");
                target = null;
                isChasing = false;
                EnterWanderingState();
            }
        }
    }

    protected virtual void TryFindNewTarget()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(chaseRange / 2f, 0.5f, chaseRange / 2f),
            Quaternion.identity,
            targetLayer
        );

        float minDist = float.MaxValue;
        UnityEngine.Transform nearestTarget = null;

        foreach (var hit in hits)
        {
            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null && targetBase.IsDead)
                continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestTarget = hit.transform;
            }
        }

        if (nearestTarget != null)
        {
            target = nearestTarget;
            EnterChasingState();
            Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: {target.name} 추격 시작!");
        }
    }

    protected virtual void HandleChasing()
    {
        if (target == null)
        {
            isChasing = false;
            EnterWanderingState();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange && GetAttackCooldown() <= 0f)
        {
            if (IsTargetInFOV() && attackRoutine == null)
            {
                attackRoutine = StartCoroutine(GetAttackSequence());
            }
        }
        else
        {
            ChaseTarget();
        }
    }

    protected virtual void ChaseTarget()
    {
        if (target == null || agent == null || !agent.isOnNavMesh) return;

        if (Time.time - lastChaseUpdate >= chaseUpdateInterval)
        {
            agent.SetDestination(target.position);
            lastChaseUpdate = Time.time;
        }

        LookAtTarget();
    }

    protected virtual void HandleNormalBehavior()
    {
        switch (currentState)
        {
            case CrewState.Wandering:
                Wander();
                break;
            case CrewState.Idle:
                Idle();
                break;
            case CrewState.Chasing:
                HandleChasing();
                break;
            case CrewState.Attacking:
                break;
        }
    }

    protected virtual float GetAttackCooldown()
    {
        return 0f;
    }

    protected virtual IEnumerator GetAttackSequence()
    {
        yield return null;
    }

    // ===== 개별 경계 탐색 기능 =====
    protected Vector3 FindMyOwnEdgePoint()
    {
        List<Vector3> candidatePoints = new List<Vector3>();
        Vector3 myPosition = transform.position;

        float angleStep = 360f / personalRayCount;

        for (int i = 0; i < personalRayCount; i++)
        {
            float angle = i * angleStep + Random.Range(-10f, 10f);
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector3 edgePoint = FindEdgeInDirection(myPosition, direction);
            if (edgePoint != Vector3.zero && Vector3.Distance(myPosition, edgePoint) > 5f)
            {
                candidatePoints.Add(edgePoint);
            }
        }

        if (candidatePoints.Count > 0)
        {
            Vector3 bestPoint = candidatePoints[0];
            float maxDistance = Vector3.Distance(myPosition, bestPoint);

            foreach (var point in candidatePoints)
            {
                float distance = Vector3.Distance(myPosition, point);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPoint = point;
                }
            }

            Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계 지점 발견 - {bestPoint}");
            return bestPoint;
        }

        Debug.LogWarning($"{GetCrewTypeName()} {GetMarinerId()}: 경계 지점을 찾지 못함");
        return Vector3.zero;
    }

    private Vector3 FindEdgeInDirection(Vector3 startPoint, Vector3 direction)
    {
        float stepSize = 2f;
        Vector3 lastValidPoint = startPoint;

        for (float distance = stepSize; distance < personalMaxDistance; distance += stepSize)
        {
            Vector3 testPoint = startPoint + direction * distance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPoint, out hit, stepSize, NavMesh.AllAreas))
            {
                lastValidPoint = hit.position;
            }
            else
            {
                return lastValidPoint;
            }
        }

        return lastValidPoint;
    }

    protected IEnumerator MoveToMyEdgeAndFarm()
    {
        if (!hasFoundPersonalEdge)
        {
            personalEdgePoint = FindMyOwnEdgePoint();
            if (personalEdgePoint == Vector3.zero)
            {
                Debug.LogWarning($"{GetCrewTypeName()} {GetMarinerId()}: 경계 지점을 찾을 수 없어 현재 위치에서 파밍");
                personalEdgePoint = transform.position;
            }
            hasFoundPersonalEdge = true;
        }

        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계 지점으로 이동 - {personalEdgePoint}");

        MoveTo(personalEdgePoint);

        while (!IsArrived())
        {
            if (!isSecondPriorityStarted)
            {
                yield break;
            }
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        yield return StartCoroutine(PerformPersonalEdgeFarming());
    }

    protected virtual IEnumerator PerformPersonalEdgeFarming()
    {
        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계에서 파밍 시작");

        float farmingTime = 0f;
        float totalFarmingTime = 10f;

        while (farmingTime < totalFarmingTime)
        {
            if (!isSecondPriorityStarted)
            {
                yield break;
            }

            if (GameManager.Instance.TimeUntilNight() <= 30f)
            {
                Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 밤이 가까워 파밍 중단");
                OnNightApproaching();
                yield break;
            }

            yield return new WaitForSeconds(1f);
            farmingTime += 1f;
        }

        OnPersonalFarmingCompleted();
        hasFoundPersonalEdge = false;
    }

    protected virtual void OnPersonalFarmingCompleted()
    {
        // 각 AI에서 오버라이드
    }

    // ===== 수리 관련 함수들 =====
    protected virtual void StartRepair()
    {
        Debug.Log($"승무원 {GetMarinerId()}: StartRepair() 호출됨");

        MarinerManager.Instance.UpdateRepairTargets();
        List<DefenseObject> needRepairList = MarinerManager.Instance.GetNeedsRepair();
        Debug.Log($"승무원 {GetMarinerId()}: 수리 대상 개수: {needRepairList.Count}");

        for (int i = 0; i < needRepairList.Count; i++)
        {
            DefenseObject obj = needRepairList[i];

            if (MarinerManager.Instance.TryOccupyRepairObject(obj, GetMarinerId()))
            {
                targetRepairObject = obj;

                if (MarinerManager.Instance.CanMarinerRepair(GetMarinerId(), targetRepairObject))
                {
                    Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 시작: {targetRepairObject.name}");
                    isRepairing = true;
                    StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
                    return;
                }
                else
                {
                    MarinerManager.Instance.ReleaseRepairObject(obj);
                }
            }
        }

        if (!isSecondPriorityStarted)
        {
            Debug.Log($"{GetCrewTypeName()} 수리 대상 없음 -> 2순위 행동 시작");
            isSecondPriorityStarted = true;
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    protected IEnumerator MoveToRepairObject(Vector3 targetPosition)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPosition);

            while (!IsArrived())
            {
                yield return null;
            }
        }

        StartCoroutine(RepairProcess());
    }

    protected virtual IEnumerator RepairProcess()
    {
        float repairDuration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bool repairSuccess = GetRepairSuccessRate() > Random.value;
        int actualRepairAmount = repairSuccess ? repairAmount : 0;

        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 {(repairSuccess ? "성공" : "실패")}: {targetRepairObject.name}/ 수리량: {actualRepairAmount}");
        targetRepairObject.Repair(actualRepairAmount);

        isRepairing = false;
        MarinerManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"{GetCrewTypeName()} 밤 도달 예외행동 시작");
            OnNightApproaching();
            yield break;
        }

        StartRepair();
        MarinerManager.Instance.ReleaseRepairObject(targetRepairObject);
    }

    public virtual IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"{GetCrewTypeName()} 2순위 행동 - 기본 구현");
        yield return StartCoroutine(MoveToMyEdgeAndFarm());
    }

    // ===== NavMeshAgent 관련 공통 함수들 =====
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    public bool IsArrived()
    {
        if (agent == null || !agent.isOnNavMesh) return true;

        bool navMeshCondition = !agent.pathPending &&
                              agent.remainingDistance <= (agent.stoppingDistance + 1f);

        if (agent.destination != Vector3.zero)
        {
            float directDistance = Vector3.Distance(transform.position, agent.destination);
            bool directCondition = directDistance <= 1.0f;

            return navMeshCondition || directCondition;
        }

        return navMeshCondition;
    }

    public IEnumerator MoveToThenReset(Vector3 destination)
    {
        MoveTo(destination);

        while (!IsArrived())
        {
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        Debug.Log($"{GetCrewTypeName()} ResetPath 호출");
    }

    protected virtual float GetRepairSuccessRate()
    {
        return 1.0f;
    }

    protected virtual int GetMarinerId()
    {
        return 0;
    }

    protected virtual string GetCrewTypeName()
    {
        return "승무원";
    }

    protected virtual void OnNightApproaching()
    {
        Debug.Log($"{GetCrewTypeName()} 기본 밤 처리");
    }

    protected virtual void OnDrawGizmos()
    {
        if (isShowingAttackBox)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * 1f;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 2f));
        }

        // 개인 경계 지점 표시
        if (hasFoundPersonalEdge && personalEdgePoint != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(personalEdgePoint, 1f);
            Gizmos.DrawLine(transform.position, personalEdgePoint);
        }
    }
}