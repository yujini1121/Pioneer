using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerAI : MonoBehaviour
{
    public enum MarinerState { Wandering, Idle, Attacking }

    // --- 상태 ---
    private MarinerState currentState = MarinerState.Wandering;

    // --- 공통 변수 ---
    public LayerMask targetLayer;
    public float detectionRange = 3f;
    public float attackInterval = 0.5f;
    private float attackCooldown = 0f;
    private Transform target;

    // --- 낮 행동 변수 ---
    public int marinerId;
    private bool isRepairing = false;
    private DefenseObject targetRepairObject;
    private int repairAmount = 30;
    private bool isSecondPriorityStarted = false;

    // --- 밤 행동 변수 ---
    private float speed = 1f;
    private float moveDuration = 2f;
    private float idleDuration = 4f;
    private float stateTimer = 0f;
    private Vector3 moveDirection;

    private bool isShowingAttackBox = false;
    private float attackVisualDuration = 1f;
    private Coroutine attackRoutine;

    private void Start()
    {
        SetRandomDirection();
        stateTimer = moveDuration;
        Debug.Log("Mariner AI 작동 시작");
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

            // 낮엔 걷기/대기 상태는 쓸 일이 없음 (이동 없음)
        }
        else
        {
            // 밤: Mariner AI 
            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0f)
            {
                if (DetectTarget())
                {
                    LookAtTarget();

                    if (attackRoutine == null)
                        attackRoutine = StartCoroutine(AttackSequence());
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
                    // 공격 시퀀스에서 처리
                    break;
            }
        }
    }

    // --- 낮 행동 함수들 ---

    private void StartRepair()
    {
        List<DefenseObject> needRepairList = GameManager.Instance.GetNeedsRepair();

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0];

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log("승무원 수리 중");
                isRepairing = true;
                StartCoroutine(RepairProcess());
            }
            Debug.Log($"Mariner {marinerId} 수리 대상: {targetRepairObject.name}, HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log("수리 대상 없음, 2순위 행동 시작");
                isSecondPriorityStarted = true;
                StartCoroutine(StartSecondPriorityAction());
            }
        }
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

        Debug.Log($"Mariner {marinerId} 수리 완료: {targetRepairObject.name}, 수리량: {repairAmount}");
        targetRepairObject.Repair(repairAmount);

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("밤 시간 임박, 일시 행동 종료");
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        StartRepair();
    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("일반 승무원 2순위 낮 행동 시작");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : 1;
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? fallbackIndex : Random.Range(0, spawnPoints.Length);

            if (triedIndexes.Contains(index)) continue;

            if (!GameManager.Instance.IsSpawnerOccupied(index))
            {
                GameManager.Instance.OccupySpawner(index);
                chosenIndex = index;
                Debug.Log("스포너 점유 성공");
                break;
            }
            else
            {
                triedIndexes.Add(index);
                float waitTime = Random.Range(0f, 1f);
                Debug.Log("스포너 점유중, 대기 후 재탐색");
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (chosenIndex == -1)
        {
            Debug.LogWarning("스포너 모두 점유, 기본 위치로 이동");
            chosenIndex = fallbackIndex;
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        yield return StartCoroutine(MoveToTarget(targetSpawn.position));

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("밤 시간 임박, 2순위 행동 중단");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log("10초간 수리 대기");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood");
        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)
        {
            Debug.Log("수리 대상 발견, 1순위 행동 재개");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log("수리 대상 없음, 2순위 행동 반복");
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    public IEnumerator MoveToTarget(Vector3 destination, float stoppingDistance = 2f)
    {
        float moveSpeed = 2f;
        while (Vector3.Distance(transform.position, destination) > stoppingDistance)
        {
            Vector3 direction = (destination - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            yield return null;
        }
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
