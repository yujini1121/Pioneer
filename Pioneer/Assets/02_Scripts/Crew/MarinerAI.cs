using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerAI : MonoBehaviour
{
    public enum MarinerState { Wandering, Idle, Attacking }

    private MarinerState currentState = MarinerState.Wandering;

    public LayerMask targetLayer;
    public float detectionRange = 3f;
    public float attackInterval = 0.5f;
    private float attackCooldown = 0f;
    private Transform target;

    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;
    private int repairAmount = 30;
    private bool isSecondPriorityStarted = false;

    // 공격관련
    private float speed = 1f;
    private float moveDuration = 2f;
    private float idleDuration = 4f;
    private float stateTimer = 0f;
    private Vector3 moveDirection;

    private bool isShowingAttackBox = false;
    private float attackVisualDuration = 1f;
    private Coroutine attackRoutine;


    private NavMeshAgent agent;

    //ray
    private FOVController fovController;

    private void Awake()
    {
        fovController = GetComponent<FOVController>();
    }

    private bool IsTargetInFOV()
    {
        if (target == null || fovController == null)
            return false;

        return fovController.visibleTargets.Contains(target);

    }
    private void Start()
    {
        SetRandomDirection();
        stateTimer = moveDuration;
        agent = GetComponent<NavMeshAgent>();
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
            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0f)
            {
                if (DetectTarget())
                {
                    // 시야 내에 적이 있는지 확인
                    if (IsTargetInFOV())
                    {
                        LookAtTarget();  // 시야 내에 있으면 적을 바라봄

                        // 공격 조건을 체크하여 공격 시작
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

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0]; // 임시로 index 0번 테스트 수리

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log("승무원 수리 중");
                isRepairing = true;
                StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
            }
            Debug.Log($"Mariner {marinerId} 수리된 오브젝트 : {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log("수리 오브젝트 없음으로 2순위 행동 시작");
                isSecondPriorityStarted = true;
                StartCoroutine(StartSecondPriorityAction());
            }
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
        float repairDuration = 3f;
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
    }


    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("일반 승무원 2순위 낮 행동 시작");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : 1; // 임시로 스포너는 0 과 1로 홀짝 구현은 나중에?
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? fallbackIndex : Random.Range(0, spawnPoints.Length);
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

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("승무원 밤 행동 시작");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log("승무원 10초 동안 수리");
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
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(1.5f, 0.5f, 1.5f),
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
        yield return new WaitForSeconds(attackVisualDuration);
        isShowingAttackBox = false;

        Vector3 boxCenter = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapBox(boxCenter, new Vector3(1f, 0.5f, 1f), transform.rotation, targetLayer);

        foreach (var hit in hits)
        {
            Debug.Log($"{hit.name} 공격 범위 내");

            MarinerStatus marinerStatus = hit.GetComponent<MarinerStatus>();
            if (marinerStatus != null)
            {
                int damage = marinerStatus.attackPower;
                marinerStatus.currentHP -= damage;
                Debug.Log($"{hit.name}에게 {damage}의 데미지를 입혔습니다.");

                marinerStatus.UpdateStatus();
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
        Gizmos.DrawWireCube(transform.position, new Vector3(3f, 1f, 3f));
    }

}
