using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerAI : MarinerBase, IBegin
{
    // 승무원 고유 설정
    public int marinerId;

    // 공격 설정
    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;

    private void Awake()
    {
        maxHp = 100;  // Mariner HP
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        fov = GetComponent<FOVController>();

        gameObject.layer = LayerMask.NameToLayer("Mariner");
        targetLayer = LayerMask.GetMask("Enemy");
    }

    public override void Init()
    {
        SetRandomDirection();
        stateTimer = moveDuration;

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
                case CrewState.Wandering:
                    Wander();
                    break;
                case CrewState.Idle:
                    Idle();
                    break;
                case CrewState.Attacking:
                    break;
            }
        }
    }

    /// <summary>
    /// 2순위 행동 구현 (자원 수집)
    /// </summary>
    public override IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("일반 승무원 2순위 낮 행동 시작");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : spawnPoints.Length - 1; // 짝수는 0, 홀수는 마지막 인덱스
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? (marinerId % 2 == 0 ? fallbackIndex + marinerId : spawnPoints.Length - 1 - marinerId) : Random.Range(0, spawnPoints.Length);

            if (triedIndexes.Contains(index)) continue; // 이미 시도한 스포너는 건뛰

            if (!GameManager.Instance.IsSpawnerOccupied(index)) // 비 점유 중
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

        if (chosenIndex == -1) // 예외 처리
        {
            Debug.LogWarning("모든 승무원이 사용중 임으로 처음 위치로 이동함.");
            chosenIndex = fallbackIndex; // 첫 위치로 이동
        }

        UnityEngine.Transform targetSpawn = spawnPoints[chosenIndex].transform;
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

    /// <summary>
    /// 공격 시퀀스 
    /// </summary>
    private IEnumerator AttackSequence()
    {
        currentState = CrewState.Attacking;

        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        while (Vector3.Distance(transform.position, attackPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, speed * Time.deltaTime);
            yield return null;
        }

        isShowingAttackBox = true;
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
                targetBase.TakeDamage(attackDamage);
                Debug.Log($"{hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }

        currentState = CrewState.Wandering;
        stateTimer = moveDuration;
        SetRandomDirection();
        attackRoutine = null;
    }

    /// <summary>
    /// 일반 승무원은 100% 수리 성공
    /// </summary>
    protected override float GetRepairSuccessRate()
    {
        return 1.0f; // 100% 성공률
    }

    /// <summary>
    /// 밤이 다가올 때 처리
    /// </summary>
    protected override void OnNightApproaching()
    {
        GameManager.Instance.StoreItemsAndReturnToBase(this);
    }
}