using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerAI : MarinerBase, IBegin
{
    public int marinerId;

    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;

    private bool isRegistered = false;
    private bool lastDaytimeState = false;
    private bool hasInitializedDaytimeState = false;

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

        if (!isRegistered)
        {
            MarinerManager.Instance.RegisterMariner(this);
            isRegistered = true;
        }

        bool currentDaytimeState = GameManager.Instance.IsDaytime;
        
        if (!hasInitializedDaytimeState)
        {
            lastDaytimeState = currentDaytimeState;
            hasInitializedDaytimeState = true;
        }
        
        if (currentDaytimeState != lastDaytimeState)
        {
            OnTimeStateChanged(currentDaytimeState);
            lastDaytimeState = currentDaytimeState;
        }

        if (currentDaytimeState)
        {
            HandleDaytimeBehavior();
        }
        else
        {
            HandleNightCombat();
        }
    }

    // 시간대 변화 처리 (새로운 메서드)
    private void OnTimeStateChanged(bool isDaytime)
    {
        if (isDaytime)
        {
            Debug.Log($"승무원 {marinerId}: 낮으로 전환");
            CleanupNightCombat();
            TransitionToDaytime();
        }
        else
        {
            Debug.Log($"승무원 {marinerId}: 밤으로 전환");
            TransitionToNight();
        }
    }

    private void CleanupNightCombat()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            isShowingAttackBox = false;
            Debug.Log($"승무원 {marinerId}: 낮 전환으로 공격 루틴 중단");
        }

        if (isChasing)
        {
            Debug.Log($"승무원 {marinerId}: 낮 전환으로 추격 모드 해제");
            isChasing = false;
            target = null;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    private void TransitionToDaytime()
    {
        isSecondPriorityStarted = false;
        isRepairing = false;
        
        Debug.Log($"승무원 {marinerId}: 낮 행동 모드로 전환");
        StartRepair();
    }

    private void TransitionToNight()
    {
        if (isRepairing || isSecondPriorityStarted)
        {
            isRepairing = false;
            isSecondPriorityStarted = false;
            
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    private void HandleDaytimeBehavior()
    {
        if (!isRepairing && !isSecondPriorityStarted)
        {
            StartRepair(); 
        }
    }

    private void HandleNightCombat()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;

        ValidateCurrentTarget();

        if (target == null)
        {
            TryFindNewTarget();
        }

        if (isChasing && target != null)
        {
            HandleChasing();
        }
        else
        {
            HandleNormalBehavior();
        }
    }

    protected override int GetMarinerId()
    {
        return marinerId;
    }

    protected override string GetCrewTypeName()
    {
        return "승무원";
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

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
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
                EnterChasingState();
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

        int chosenIndex = (marinerId - 1) % spawnPoints.Length; //(임시) 스포너 개수보다 승무원이 많을 경우 대비

        Debug.Log($"승무원 {marinerId}: 할당된 스포너 {chosenIndex}로 이동");

        UnityEngine.Transform targetSpawn = spawnPoints[chosenIndex].transform;
        MoveTo(targetSpawn.position);

        while (!IsArrived())
        {
            yield return null;
        }

        // NavMesh 경로 초기화
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"승무원 {marinerId}: 밤이 가까워 수집 작업 중단");
            MarinerManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log($"승무원 {marinerId}: 10초 동안 자원 수집");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood"); // 자원 수집

        var needRepairList = MarinerManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log($"승무원 {marinerId}: 수리 대상 발견으로 1순위 행동 실행");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log($"승무원 {marinerId}: 수리 대상 미발견으로 2순위 행동 계속");
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