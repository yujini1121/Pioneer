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

        fov.Start();
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

    private void OnTimeStateChanged(bool isDaytime)
    {
        if (isDaytime)
        {
            CleanupNightCombat();
            TransitionToDaytime();
        }
        else
        {
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
        }

        if (isChasing)
        {
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

        CancelCurrentRepair();

        Debug.Log($"승무원 {marinerId}: 낮 행동 모드로 전환");
        StartRepair();
    }

    private void TransitionToNight()
    {
        if (isRepairing || isSecondPriorityStarted)
        {
            CancelCurrentRepair();

            isSecondPriorityStarted = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    private void HandleDaytimeBehavior()
    {
        if (IsDead) return;

        ValidateCurrentTarget();

        if (target == null)
        {
            TryFindNewTarget();
        }

        if (target != null)
        {
            if (isRepairing)
            {
                Debug.Log($"승무원 {marinerId}: 적 발견으로 수리 완전 취소");
                CancelCurrentRepair();
            }

            if (isSecondPriorityStarted)
            {
                Debug.Log($"승무원 {marinerId}: 적 발견으로 2순위 작업 중단");
                CancelSecondPriorityAction();
            }

            attackCooldown -= Time.deltaTime;

            if (isChasing && target != null)
            {
                HandleChasing();
            }
            else if (target != null)
            {
                EnterChasingState();
            }
        }
        else
        {
            if (!isRepairing && !isSecondPriorityStarted)
            {
                StartRepair();
            }
        }
    }

    private void CancelCurrentRepair()
    {
        if (isRepairing)
        {
            isRepairing = false;
            StopAllCoroutines();

            if (targetRepairObject != null)
            {
                MarinerManager.Instance.ReleaseRepairObject(targetRepairObject);
                targetRepairObject = null;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            Debug.Log($"승무원 {marinerId}: 수리 취소");
        }
    }

    private void CancelSecondPriorityAction()
    {
        if (isSecondPriorityStarted)
        {
            isSecondPriorityStarted = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            Debug.Log($"승무원 {marinerId}: 2순위 작업 취소");
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

            if (GameManager.Instance.IsDaytime)
            {
                Debug.Log($"승무원 {marinerId}: 타겟 없음, 수리 재개");
                StartRepair();
            }
            else
            {
                EnterWanderingState();
            }
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

                if (GameManager.Instance.IsDaytime)
                {
                    Debug.Log($"승무원 {marinerId}: 적 제거 완료, 수리 재개");
                    StartRepair();
                }
                else
                {
                    EnterWanderingState();
                }
            }
        }
        else
        {
            isChasing = false;

            if (GameManager.Instance.IsDaytime)
            {
                Debug.Log($"승무원 {marinerId}: 전투 종료, 수리 재개");
                StartRepair();
            }
            else
            {
                EnterWanderingState();
            }
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
        int chosenIndex = (marinerId - 1) % spawnPoints.Length;

        Debug.Log($"승무원 {marinerId}: 할당된 스포너 {chosenIndex}로 이동");

        UnityEngine.Transform targetSpawn = spawnPoints[chosenIndex].transform;
        MoveTo(targetSpawn.position);

        while (!IsArrived())
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log($"승무원 {marinerId}: 이동 중 작업 취소로 2순위 행동 중단");
                yield break;
            }
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (!isSecondPriorityStarted)
        {
            Debug.Log($"승무원 {marinerId}: 작업 취소로 2순위 행동 중단");
            yield break;
        }

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"승무원 {marinerId}: 밤이 가까워 수집 작업 중단");
            MarinerManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log($"승무원 {marinerId}: 10초 동안 자원 수집 시작");

        float collectTime = 0f;
        float totalCollectTime = 10f;

        while (collectTime < totalCollectTime)
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log($"승무원 {marinerId}: 자원 수집 중 작업 취소로 2순위 행동 중단");
                yield break;
            }

            yield return new WaitForSeconds(1f); 
            collectTime += 1f;
        }

        if (!isSecondPriorityStarted)
        {
            Debug.Log($"승무원 {marinerId}: 자원 수집 완료 후 작업 취소로 2순위 행동 중단");
            yield break;
        }

        GameManager.Instance.CollectResource("wood"); // 자원 수집
        Debug.Log($"승무원 {marinerId}: 자원 수집 완료");

        var needRepairList = MarinerManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)
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