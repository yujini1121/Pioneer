using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(FOVController))]
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
        base.Start();  // 먼저 호출 (NavMeshAgent 설정)

        SetRandomDirection();
        stateTimer = moveDuration;
        fov.Start();
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

    private void ResetAgentPath()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    private void UpdateTargetDetection()
    {
        ValidateCurrentTarget();
        if (target == null)
        {
            TryFindNewTarget();
        }
    }

    private void HandlePostCombatAction()
    {
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

    private bool CheckSecondPriorityActionCancellation(string context)
    {
        if (!isSecondPriorityStarted)
        {
            Debug.Log($"승무원 {marinerId}: {context} 작업 취소로 2순위 행동 중단");
            return true;
        }
        return false;
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
            ResetAgentPath();
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
            ResetAgentPath();
        }
    }

    private void HandleDaytimeBehavior()
    {
        if (IsDead) return;

        UpdateTargetDetection();

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

            ResetAgentPath();
            Debug.Log($"승무원 {marinerId}: 수리 취소");
        }
    }

    private void CancelSecondPriorityAction()
    {
        if (isSecondPriorityStarted)
        {
            isSecondPriorityStarted = false;
            ResetAgentPath();
            Debug.Log($"승무원 {marinerId}: 2순위 작업 취소");
        }
    }

    private void HandleNightCombat()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;
        UpdateTargetDetection();

        if (isChasing && target != null)
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
            HandlePostCombatAction();
            yield break;
        }

        ResetAgentPath();
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
                HandlePostCombatAction();
            }
        }
        else
        {
            isChasing = false;
            HandlePostCombatAction();
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
                targetBase.TakeDamage(attackDamage, this.gameObject);
                Debug.Log($"승무원이 {hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }
    }

    public override IEnumerator StartSecondPriorityAction()
    {
        // 인벤토리 체크 - 7개 이상이면 보관함으로 이동
        MarinerInventory inventory = GetComponent<MarinerInventory>();
        if (inventory != null && inventory.ShouldMoveToStorage())
        {
            Debug.Log($"승무원 {marinerId}: 인벤토리가 가득함 ({inventory.GetAllItem()}개) - 보관함으로 이동");

            // 보관함 찾기
            GameObject storage = GameObject.FindWithTag("Engine");
            if (storage != null)
            {
                // NavMeshAgent로 보관함으로 이동
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(storage.transform.position);

                    // 보관함에 도착할 때까지 대기
                    while (!IsArrived())
                    {
                        if (!isSecondPriorityStarted)
                        {
                            yield break;
                        }

                        if (GameManager.Instance.TimeUntilNight() <= 30f)
                        {
                            OnNightApproaching();
                            yield break;
                        }
                        yield return null;
                    }

                    Debug.Log($"승무원 {marinerId}: 보관함에 도착 - 아이템 저장");

                    // 보관함에 아이템 저장
                    var storageInventory = storage.GetComponent<InventoryBase>();
                    if (storageInventory != null)
                    {
                        inventory.TransferAllItemsToStorage(storageInventory);
                    }

                    Debug.Log($"승무원 {marinerId}: 보관함 저장 완료 - 1순위 행동 재확인");

                    // 보관함 저장 후 1순위 행동(수리) 재확인
                    isSecondPriorityStarted = false;
                    StartRepair();
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"승무원 {marinerId}: 보관함을 찾을 수 없음");
            }
        }
        else
        {
            // 기존 파밍 시스템 그대로 유지
            Debug.Log($"승무원 {marinerId}: 개인 경계 탐색 및 파밍 시작");
            yield return StartCoroutine(MoveToMyEdgeAndFarm());

            var needRepairList = MarinerManager.Instance.GetNeedsRepair();
            if (needRepairList.Count > 0)
            {
                isSecondPriorityStarted = false;
                StartRepair();
            }
            else
            {
                StartCoroutine(StartSecondPriorityAction());
            }
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
    public override void WhenDestroy()
    {
        GameManager.Instance.MarinerDiedCount();
        base.WhenDestroy();
    }

    protected override void OnPersonalFarmingCompleted()
    {
        MarinerInventory inventory = GetComponent<MarinerInventory>();
        if (inventory != null)
        {
            Debug.Log($"AddItem 호출 전 - itemLists null 여부: {inventory.itemLists == null}");
            Debug.Log($"AddItem 호출 전 - itemLists 크기: {inventory.itemLists?.Count ?? 0}");

            bool result = inventory.AddItem(30001, 1);
            Debug.Log($"AddItem 결과: {result}");
        }
        Debug.Log($"승무원 {marinerId}: 개인 경계에서 자원 수집 완료");
    }
}