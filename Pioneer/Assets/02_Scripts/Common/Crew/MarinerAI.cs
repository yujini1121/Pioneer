using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerAI : CreatureBase, IBegin
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;
    private int repairAmount = 30;

    private bool isSecondPriorityStarted = false;

    public enum MarinerState { Wandering, Idle, Attacking }

    private MarinerState currentState = MarinerState.Wandering;

    public LayerMask targetLayer;
    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;
    private Transform target;

    // 공격관련
    private float moveDuration = 2f;    
    private float idleDuration = 4f;
    private float stateTimer = 0f;
    private Vector3 moveDirection;

    private bool isShowingAttackBox = false;
    private Coroutine attackRoutine;

    private NavMeshAgent agent;

    private void Awake()
    {
        // 상위 클래스 변수들에 값 할당 (Inspector에 표시되도록)
        maxHp = 100;  // Mariner HP
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        // CreatureBase의 fov 변수 사용
        fov = GetComponent<FOVController>();

        gameObject.layer = LayerMask.NameToLayer("Mariner");
        targetLayer = LayerMask.GetMask("Enemy");
    }

    private bool IsTargetInFOV()
    {
        if (target == null || fov == null)
            return false;

        // FOV에서 타겟 감지 수행
        fov.DetectTargets(targetLayer);
        return fov.visibleTargets.Contains(target);
    }

    public override void Init()
    {
        SetRandomDirection();
        stateTimer = moveDuration;
        agent = GetComponent<NavMeshAgent>();

        // FOVController 초기화
        if (fov != null)
        {
            fov.Init();
        }

        Debug.Log($"Mariner {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}, 속도: {speed}, 공격범위: {attackRange}");

        base.Init();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RegisterMariner(this);

        if (GameManager.Instance.IsDaytime)
        {
            // 낮: 수리 및 2순위 행동
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
                isShowingAttackBox = false;
            }

            if (!isRepairing)
                StartRepair();
        }
        else
        {
            // 밤: Mariner AI
            if (IsDead) return;

            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0f)
            {
                if (DetectTarget())
                {
                    if (IsTargetInFOV())
                    {
                        LookAtTarget();

                        if (attackRoutine == null)
                        {
                            attackRoutine = StartCoroutine(AttackSequence());
                        }
                    }
                }

                attackCooldown = attackInterval;
            }

            switch (currentState)
            {
                case MarinerState.Wandering:
                    Wander();
                    break;
                case MarinerState.Idle:
                    Idle();
                    break;
                case MarinerState.Attacking:
                    break;
            }
        }
    }

    private void StartRepair()
    {
        List<DefenseObject> needRepairList = GameManager.Instance.GetNeedsRepair();

        for (int i = 0; i < needRepairList.Count; i++)
        {
            DefenseObject obj = needRepairList[i];

            if (GameManager.Instance.TryOccupyRepairObject(obj, marinerId))
            {
                targetRepairObject = obj;

                if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
                {
                    Debug.Log($"Mariner {marinerId} 수리 시작: {targetRepairObject.name}");
                    isRepairing = true;
                    StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
                    return;
                }
                else
                {
                    GameManager.Instance.ReleaseRepairObject(obj); // 점유 해제
                }
            }
        }

        // 점유할 수 있는 수리 대상이 없는 경우
        if (!isSecondPriorityStarted)
        {
            Debug.Log("수리 대상 없음 -> 2순위 행동 시작");
            isSecondPriorityStarted = true;
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    // 수리할 오브젝트로 이동하는 함수
    private IEnumerator MoveToRepairObject(Vector3 targetPosition)
    {
        agent.SetDestination(targetPosition);

        while (!IsArrived())
        {
            yield return null;
        }

        StartCoroutine(RepairProcess());
    }

    private IEnumerator RepairProcess()
    {
        float repairDuration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"Mariner {marinerId} 수리 완료: {targetRepairObject.name}/ 수리량: {repairAmount}");
        targetRepairObject.Repair(repairAmount);

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("일반 승무원 밤 도달 예외행동 시작");
            GameManager.Instance.StoreItemsAndReturnToBase(this); // 임시 수정 필요
            yield break;
        }

        StartRepair();
        GameManager.Instance.ReleaseRepairObject(targetRepairObject); // 수리 완료 후 점유 해제

    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("일반 승무원 2순위 낮 행동 시작");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : spawnPoints.Length - 1; // 짝수는 0, 홀수는 마지막 인덱스
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? (marinerId % 2 == 0 ? fallbackIndex + marinerId : spawnPoints.Length - 1 - marinerId) : Random.Range(0, spawnPoints.Length);

            // 현재 0과 1만 사용 중 나중에 스포너 범위 들어오면 수정

            if (triedIndexes.Contains(index)) continue; // 이미 시도한 스포너는 건뛰

            if (!GameManager.Instance.IsSpawnerOccupied(index)) // 비 점유 중
                                                                // 선택된 스포너가 이미 다른 유닛이 선택했는가? 플로우차트 확인
            {
                GameManager.Instance.OccupySpawner(index);
                chosenIndex = index;
                Debug.Log("현재 다른 승무원이 사용중 인 스포너");

                break;
            }
            else // 점유중
            {
                triedIndexes.Add(index);
                float waitTime = Random.Range(0f, 1f);
                Debug.Log("다른 승무원이 점유 중이라 랜덤 시간 후 다시 탐색");
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (chosenIndex == -1) // 예외 처리 필요할까?
        {
            Debug.LogWarning("모든 승무원이 사용중 임으로 처음 위치로 이동함.");
            chosenIndex = fallbackIndex; // 첫 위치로 이동
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        MoveTo(targetSpawn.position);

        // 도착 대기
        while (!IsArrived())
        {
            yield return null;
        }

        agent.ResetPath(); // 초기화 코드

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("승무원 밤 행동 시작");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log("승무원 10초 동안 파밍");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood"); // 출력만
        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log("승무원 수리 대상 발견으로 1순위 행동 실행");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log("승무원 수리 대상 미발견으로 2순위 행동 실행");
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    // ↓ 기존 MoveToTarget은 삭제하고 아래로 대체

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    public bool IsArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // --- 밤 행동 함수들 ---

    private void Wander()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Debug.Log("Night Mariner AI 이동 후 대기 상태");
            EnterIdleState();
        }
    }

    private void Idle()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Debug.Log("Night Mariner AI 대기에서 다시 이동 상태");
            EnterWanderingState();
        }
    }

    private void EnterWanderingState()
    {
        SetRandomDirection();
        currentState = MarinerState.Wandering;
        stateTimer = moveDuration;
        Debug.Log("랜덤 방향으로 이동 시작");
    }

    private void EnterIdleState()
    {
        currentState = MarinerState.Idle;
        stateTimer = idleDuration;
        Debug.Log("Night Mariner AI 대기 상태로 전환");
    }

    private void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    private bool DetectTarget()
    {
        // attackRange 변수 사용
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(attackRange / 2f, 0.5f, attackRange / 2f),
            Quaternion.identity,
            targetLayer
        );

        float minDist = float.MaxValue;
        target = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                target = hit.transform;
            }
        }

        return target != null;
    }

    private void LookAtTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    private IEnumerator AttackSequence()
    {
        currentState = MarinerState.Attacking;

        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        while (Vector3.Distance(transform.position, attackPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, speed * Time.deltaTime);
            yield return null;
        }

        isShowingAttackBox = true;
        // CreatureBase의 attackDelayTime 변수 사용
        yield return new WaitForSeconds(attackDelayTime);
        isShowingAttackBox = false;

        Vector3 boxCenter = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapBox(boxCenter, new Vector3(1f, 0.5f, 1f), transform.rotation, targetLayer);

        foreach (var hit in hits)
        {
            Debug.Log($"{hit.name} 공격 범위 내");

            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                // CreatureBase의 attackDamage 변수 사용
                targetBase.TakeDamage(attackDamage);
                Debug.Log($"{hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }

        currentState = MarinerState.Wandering;
        stateTimer = moveDuration;
        SetRandomDirection();
        attackRoutine = null;
    }

    private void OnDrawGizmos()
    {
        if (isShowingAttackBox)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * 1f;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 2f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // CreatureBase의 attackRange 변수 사용
        Gizmos.DrawWireCube(transform.position, new Vector3(attackRange, 1f, attackRange));
    }

    //목적지 초기화 코드
    public IEnumerator MoveToThenReset(Vector3 destination)
    {
        MoveTo(destination);

        while (!IsArrived())
        {
            yield return null;
        }

        agent.ResetPath();
        Debug.Log(" ResetPath 호출");
    }
}