using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerAI : MarinerBase, IBegin
{
    public int marinerId;

    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;

    private void Awake()
    {
        maxHp = 100;  // Mariner HP
        speed = 2f;
        attackDamage = 6;
        attackRange = 4f;
        attackDelayTime = 1f;

        fov = GetComponent<FOVController>();

        gameObject.layer = LayerMask.NameToLayer("Mariner");
        targetLayer = LayerMask.GetMask("Enemy");
    }

    public override void Start()
    {
        SetRandomDirection();
        stateTimer = moveDuration;

        if (fov != null)
        {
            fov.Start();
        }

        Debug.Log($"Mariner {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}, 속도: {speed}, 공격범위: {attackRange}");

        base.Start();
    }

    private void Update()
    {
        if (GameManager.Instance == null || MarinerManager.Instance == null) return;

        MarinerManager.Instance.RegisterMariner(this);

        if (GameManager.Instance.IsDaytime)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
                isShowingAttackBox = false;
            }

            isChasing = false;
            target = null;

            if (!isRepairing)
                StartRepair();
        }
        else
        {
            HandleNightCombat();
        }
    }

    private void HandleNightCombat()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;

        ValidateCurrentTarget();

        if (target == null && !isChasing)
        {
            TryFindNewTarget();
        }

        if (target != null && isChasing)
        {
            HandleChasing(); 
        }
        else
        {
            HandleNormalBehavior(); 
        }
    }

    protected override float GetAttackCooldown()
    {
        return attackCooldown;
    }

    protected override IEnumerator GetAttackSequence()
    {
        return MarinerAttackSequence();
    }

    private IEnumerator MarinerAttackSequence()
    {
        currentState = CrewState.Attacking;

        if (target == null)
        {
            attackRoutine = null;
            isChasing = false;
            EnterWanderingState();
            yield break;
        }

        LookAtTarget();

        isShowingAttackBox = true;

        yield return new WaitForSeconds(attackDelayTime);

        isShowingAttackBox = false;

        PerformMarinerAttack();

        attackCooldown = attackInterval;

        if (target != null)
        {
            CommonBase targetBase = target.GetComponent<CommonBase>();
            if (targetBase != null && !targetBase.IsDead)
            {
                Debug.Log($"승무원 {marinerId}: 공격 완료, 추격 재개");
                currentState = CrewState.Wandering; // 추격을 위해 상태 변경
            }
            else
            {
                target = null;
                isChasing = false;
                EnterWanderingState();
            }
        }
        else
        {
            isChasing = false;
            EnterWanderingState();
        }

        attackRoutine = null;
    }
    private void PerformMarinerAttack()
    {
        Vector3 boxCenter = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapBox(boxCenter, new Vector3(1f, 0.5f, 1f), transform.rotation, targetLayer);

        foreach (var hit in hits)
        {
            Debug.Log($"승무원이 {hit.name} 공격 범위 내 감지");

            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(attackDamage);
                Debug.Log($"승무원이 {hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }
    }
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

            if (!MarinerManager.Instance.IsSpawnerOccupied(index)) // 비 점유 중
            {
                MarinerManager.Instance.OccupySpawner(index);
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

        while (!IsArrived())
        {
            yield return null;
        }

        agent.ResetPath(); // 초기화 코드

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("승무원 밤 행동 시작");
            MarinerManager.Instance.ReleaseSpawner(chosenIndex);
            MarinerManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log("승무원 10초 동안 파밍");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood"); // 출력만
        MarinerManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = MarinerManager.Instance.GetNeedsRepair();
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

    protected override float GetRepairSuccessRate()
    {
        return 1.0f; // 100% 성공률
    }

    protected override void OnNightApproaching()
    {
        MarinerManager.Instance.StoreItemsAndReturnToBase(this);
    }
}