using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerBase : CreatureBase
{
    // 승무원 공통 설정
    public LayerMask targetLayer;

    public enum CrewState { Wandering, Idle, Attacking, Chasing }
    protected CrewState currentState = CrewState.Wandering;

    protected float moveDuration = 2f;
    protected float idleDuration = 4f;
    protected float stateTimer = 0f;
    private Vector3 moveDirection;

    // 공격 관리
    protected UnityEngine.Transform target;
    protected bool isShowingAttackBox = false;
    protected Coroutine attackRoutine;

    // 추격 시스템
    protected float chaseRange = 8f;
    protected bool isChasing = false;
    protected float chaseUpdateInterval = 0.2f;
    protected float lastChaseUpdate = 0f;

    // 수리 관련 공통 변수
    [Header("수리 설정")]
    public bool isRepairing = false;

    //DefenseObject -> StructureBase
    protected StructureBase targetRepairObject;
    protected int repairAmount = 30;
    protected bool isSecondPriorityStarted = false;
    protected Coroutine secondPriorityRoutine;   // 2순위 코루틴 추적

    // 개별 경계 탐색 설정
    [Header("개별 경계 탐색 설정")]
    public int personalRayCount = 8;
    public float personalMaxDistance = 50f;

    protected Vector3 personalEdgePoint;
    protected Vector3 personalSeaDirection;
    protected bool hasFoundPersonalEdge = false;

    // NavMeshAgent 공통
    protected NavMeshAgent agent;

    private float originalSpeed;

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

        originalSpeed = speed;

        if (OceanEventManager.instance != null && OceanEventManager.instance.currentEvent is OceanEventThunder)
        {
            ApplyThunderSpeedModifier(0.8f);
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
        //Debug.Log("base nterWanderingState 실행");
        SetRandomDirection();
        currentState = CrewState.Wandering;
        stateTimer = moveDuration;
        //Debug.Log($"{gameObject.name} - 랜덤 방향으로 이동 시작");
    }

    protected virtual void EnterIdleState()
    {
        currentState = CrewState.Idle;
        stateTimer = idleDuration;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        //Debug.Log($"{gameObject.name} - 대기 상태로 전환");
    }

    protected virtual void EnterChasingState()
    {
        currentState = CrewState.Chasing;
        isChasing = true;
        //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 추격 상태로 전환");
    }

    protected void SetRandomDirection()
    {
        //Debug.Log("base SetRandomDirection 실행");
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
                //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 타겟 {target.name}이 죽었습니다. 새로운 타겟을 찾습니다.");
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
            //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: {target.name} 추격 시작!");
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

    protected virtual float GetAttackCooldown() => 0f;
    protected virtual IEnumerator GetAttackSequence() { yield return null; }

    // 경계 탐색 & 파밍 
    protected Vector3 FindMyOwnEdgePoint()
    {
        // Use the real platform tile bounds; a NavMesh hole around a building is not sea.
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return Vector3.zero;
        int deckMask = LayerMask.GetMask("Platform");
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        float bestScore = float.MaxValue;
        Vector3 bestPoint = Vector3.zero;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        var path = new NavMeshPath();
        MarinerBase[] crew = FindObjectsOfType<MarinerBase>();
        foreach (ItemDeck deck in FindObjectsOfType<ItemDeck>())
        {
            if (deck.IsDead || !deck.TryGetComponent(out Collider floor)) continue;
            Bounds bounds = floor.bounds;
            foreach (Vector3 direction in directions)
            {
                float extent = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.z) * bounds.extents.z;
                Vector3 edge = bounds.center + direction * extent;
                Vector3 sea = edge + direction * 0.3f;
                if (Physics.Raycast(sea + Vector3.up * 3f, Vector3.down, 6f, deckMask, QueryTriggerInteraction.Ignore)) continue;
                Vector3 candidate = edge - direction * 0.45f;
                candidate.y = bounds.max.y;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.8f, filter)) continue;
                Vector3 edgeDelta = hit.position - edge; edgeDelta.y = 0f;
                if (edgeDelta.magnitude > 0.85f) continue;
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                Vector3 delta = hit.position - transform.position; delta.y = 0f;
                float score = delta.sqrMagnitude;
                foreach (MarinerBase other in crew)
                    if (other != this && !other.IsDead
                        && (other.transform.position - hit.position).sqrMagnitude < 2f) score += 16f;
                if (score >= bestScore) continue;
                bestScore = score;
                bestPoint = hit.position;
                personalSeaDirection = direction;
            }
        }
        //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계 지점 발견 - {bestPoint}");
        //Debug.LogWarning($"{GetCrewTypeName()} {GetMarinerId()}: 경계 지점을 찾지 못함");
        return bestPoint;
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
                //Debug.LogWarning($"{GetCrewTypeName()} {GetMarinerId()}: 경계 지점을 찾을 수 없어 현재 위치에서 파밍");
                yield return new WaitForSeconds(2f);
                yield break;
            }
            hasFoundPersonalEdge = true;
        }

        //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계 지점으로 이동 - {personalEdgePoint}");

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) yield break;
        float oldStoppingDistance = agent.stoppingDistance;
        try
        {
            agent.stoppingDistance = 0.1f;
            agent.isStopped = false;
            MoveTo(personalEdgePoint);
            float deadline = Time.time + 15f;
            while (true)
            {
                if (!isSecondPriorityStarted || IsDead || Time.time > deadline
                    || !agent.isActiveAndEnabled || !agent.isOnNavMesh) yield break;
                if (!agent.pathPending)
                {
                    if (agent.pathStatus != NavMeshPathStatus.PathComplete) yield break;
                    Vector3 delta = transform.position - personalEdgePoint; delta.y = 0f;
                    if (delta.sqrMagnitude <= 0.25f * 0.25f) break;
                }
                yield return null;
            }
            if (!HasSeaAtFishingPoint()) yield break;
            agent.ResetPath();
            yield return PerformPersonalEdgeFarming();
        }
        finally
        {
            hasFoundPersonalEdge = false;
            if (agent != null) agent.stoppingDistance = oldStoppingDistance;
            GetComponentInChildren<MarinerAnimControll>(true)?.StopFishing();
        }
    }

    protected bool HasSeaAtFishingPoint()
    {
        StunHandler stun = GetComponent<StunHandler>();
        if (IsDead || (stun != null && stun.IsStunned) || personalSeaDirection.sqrMagnitude < 0.5f) return false;
        WindAirborne wind = GetComponent<WindAirborne>();
        if (wind != null && wind.IsAirborne) return false;
        if (!Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, 6f,
            LayerMask.GetMask("Platform"), QueryTriggerInteraction.Ignore)) return false;
        Vector3 probe = transform.position + personalSeaDirection * 1.2f;
        return !Physics.Raycast(probe + Vector3.up * 2f, Vector3.down, 6f,
            LayerMask.GetMask("Platform"), QueryTriggerInteraction.Ignore);
    }

    protected virtual IEnumerator PerformPersonalEdgeFarming()
    {
        //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 개인 경계에서 파밍 시작");

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        var anim = GetComponentInChildren<MarinerAnimControll>(true);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null) anim.StartFishing(transform.position + personalSeaDirection, transform);

        float endTime = Time.time + 10f;

        try
        {
            while (Time.time < endTime)
            {
                if (!isSecondPriorityStarted || !HasSeaAtFishingPoint()) yield break;

                if (GameManager.Instance.TimeUntilNight() <= 30f)
                {
                    //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 밤이 가까워 파밍 중단");
                    OnNightApproaching();
                    yield break;
                }

                yield return null;
            }

            OnPersonalFarmingCompleted();
            hasFoundPersonalEdge = false;
        }
        finally
        {
            anim?.StopFishing();
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.GetFishing);
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    protected void CancelSecondPriorityAction()
    {
        isSecondPriorityStarted = false;
        hasFoundPersonalEdge = false;
        if (secondPriorityRoutine != null)
        {
            StopCoroutine(secondPriorityRoutine);
            secondPriorityRoutine = null;
        }
        GetComponentInChildren<MarinerAnimControll>(true)?.StopFishing();
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 2순위 작업 취소");
    }

    protected virtual void OnPersonalFarmingCompleted() { /* 각 AI에서 오버라이드 */ }

    // 수리 관련 로직 (StructureBase 기반)
    protected virtual void StartRepair()
    {
        // 이미 수리 중이면 재진입 방지
        if (isRepairing || isSecondPriorityStarted) return;

        // 파밍(2순위) 중이면 즉시 취소하고 수리 우선
        if (isSecondPriorityStarted) CancelSecondPriorityAction();

        MarinerManager.Instance.UpdateRepairTargets();
        List<StructureBase> needRepairList = MarinerManager.Instance.GetNeedsRepair();
        //Debug.Log($"승무원 {GetMarinerId()}: 수리 대상 개수: {needRepairList.Count}");

        foreach (var obj in needRepairList)
        {
            if (MarinerManager.Instance.TryOccupyRepairObject(obj, GetMarinerId()))
            {
                targetRepairObject = obj;

                if (MarinerManager.Instance.CanMarinerRepair(GetMarinerId(), targetRepairObject))
                {
                    //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 시작: {targetRepairObject.name}");
                    isRepairing = true;

                    // 이동 시작 전 공격/추격 끊기(의도치 않은 전환 방지)
                    isChasing = false;
                    target = null;
                    if (agent != null && agent.isOnNavMesh) agent.ResetPath();

                    StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
                    return;
                }
                else
                {
                    MarinerManager.Instance.ReleaseRepairObject(obj);
                }
            }
        }

        // 여기까지 왔다 = 현재 수리할 게 없음 → 그때만 2순위 시작
        if (!isSecondPriorityStarted)
        {
            //Debug.Log($"{GetCrewTypeName()} 수리 대상 없음 -> 2순위 행동 시작");
            secondPriorityRoutine = StartCoroutine(StartSecondPriorityAction());
        }
    }

    protected IEnumerator MoveToRepairObject(Vector3 _)
    {
        if (agent != null && agent.isOnNavMesh && targetRepairObject != null)
        {
            Vector3 approach = GetRepairApproachPoint(targetRepairObject.transform, 0.5f);
            agent.SetDestination(approach);

            while (!IsArrivedWithRadius(0.5f))
            {
                if (!isRepairing) yield break;
                if (isSecondPriorityStarted) { isRepairing = false; yield break; }
                yield return null;
            }
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return StartCoroutine(RepairProcess());

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }



    protected virtual IEnumerator RepairProcess()
    {
        float repairDuration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            if (!isRepairing) yield break;
            if (isSecondPriorityStarted) { isRepairing = false; yield break; }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bool repairSuccess = GetRepairSuccessRate() > Random.value;
        int actualRepairAmount = repairSuccess ? repairAmount : 0;

        if (targetRepairObject != null)
        {
            //Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 {(repairSuccess ? "성공" : "실패")}: {targetRepairObject.name}/ 수리량: {actualRepairAmount}");

            targetRepairObject.Heal(actualRepairAmount);

            MarinerManager.Instance.ReleaseRepairObject(targetRepairObject);
            targetRepairObject = null;
        }

        isRepairing = false;

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            OnNightApproaching();
            yield break;
        }

        StartRepair();
    }



    public virtual IEnumerator StartSecondPriorityAction()
    {
        //Debug.Log($"{GetCrewTypeName()} 2순위 행동 - 기본 구현");
        yield return MoveToMyEdgeAndFarm();
    }

    // ===== NavMeshAgent 공통 =====
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
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
            yield return null;

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        //Debug.Log($"{GetCrewTypeName()} ResetPath 호출");
    }

    protected Vector3 GetRepairApproachPoint(Transform t, float approachRadius = 0.5f)
    {
        Vector3 target = t.position;

        // 표면 기준으로 접근점 계산
        var col = t.GetComponent<Collider>();
        if (col != null)
        {
            // 내 현재 위치에서 본 가장 가까운 표면점
            Vector3 surface = col.ClosestPoint(transform.position);
            target = surface;

            // 약간 물러서서(접근 반경만큼) NavMesh 위 점을 다시 샘플
            Vector3 back = (transform.position - surface);
            back.y = 0f;
            if (back.sqrMagnitude > 0.0001f)
                target = surface + back.normalized * Mathf.Clamp(approachRadius, 0.2f, 1.0f);
        }

        // NavMesh 보정
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 1.0f, NavMesh.AllAreas))
            return hit.position;

        return target; // 실패해도 원점 리턴
    }

    // 도착 판정 보강: 남은거리 + 여유 반경
    protected bool IsArrivedWithRadius(float extraRadius = 0.5f)
    {
        if (agent == null || !agent.isOnNavMesh) return true;

        if (agent.pathPending) return false;

        float thresh = Mathf.Max(agent.stoppingDistance, 0.0f) + Mathf.Clamp(extraRadius, 0f, 1.5f);
        if (agent.remainingDistance <= thresh) return true;

        // 안전망: 실제 좌표 거리
        if (agent.destination != Vector3.zero)
            return Vector3.Distance(transform.position, agent.destination) <= thresh;

        return false;
    }

    public void ApplyThunderSpeedModifier(float multiplier)
    {
        speed = originalSpeed * multiplier;

        if (agent != null)
            agent.speed = speed;
    }

    public void ResetThunderSpeedModifier()
    {
        speed = originalSpeed;

        if (agent != null)
            agent.speed = speed;
    }

    protected virtual float GetRepairSuccessRate() => 1.0f;
    protected virtual int GetMarinerId() => 0;
    protected virtual string GetCrewTypeName() => "승무원";
    protected virtual void OnNightApproaching() { UtilityFunctions.Log($"{GetCrewTypeName()} 기본 밤 처리"); }

    protected virtual void OnDrawGizmos()
    {
        if (isShowingAttackBox)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * 1f;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 2f));
        }

        if (hasFoundPersonalEdge && personalEdgePoint != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(personalEdgePoint, 1f);
            Gizmos.DrawLine(transform.position, personalEdgePoint);
        }
    }
}
