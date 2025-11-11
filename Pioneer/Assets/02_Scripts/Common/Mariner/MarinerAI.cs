using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(FOVController))]
public class MarinerAI : MarinerBase, IBegin
{
    Quaternion initialRot;

    private Coroutine nightRoamRoutine;
    private bool isNightRoaming;
    public bool isCharmed = false;
    public NavMeshAgent Agent => agent;

    [System.Serializable]
    public struct ItemDrop
    {
        public int itemID;
        public float probability;
    }

    private static readonly ItemDrop[] FixedItemDrops = new ItemDrop[]
    {
        new ItemDrop { itemID = 30001, probability = 0.20f },
        new ItemDrop { itemID = 30002, probability = 0.15f },
        new ItemDrop { itemID = 30003, probability = 0.15f },
        new ItemDrop { itemID = 30004, probability = 0.10f },
        new ItemDrop { itemID = 30005, probability = 0.10f },
        new ItemDrop { itemID = 30006, probability = 0.075f },
        new ItemDrop { itemID = 30007, probability = 0.0525f },
        new ItemDrop { itemID = 30008, probability = 0.0525f },
        new ItemDrop { itemID = 30009, probability = 0.06f },
        new ItemDrop { itemID = 40009, probability = 0.06f }
    };

    public int marinerId;

    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;

    private bool isRegistered = false;
    private bool lastDaytimeState = false;
    private bool hasInitializedDaytimeState = false;

    private void Awake()
    {
        maxHp = 100;
        hp = 100;
        speed = 2f;
        attackDamage = 6;
        attackRange = 1.5f;
        attackDelayTime = 1.5f;

        fov = GetComponent<FOVController>();
        initialRot = transform.rotation;

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
            // 즉시 1순위 행동 시작
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
        if (nightRoamRoutine != null) { StopCoroutine(nightRoamRoutine); nightRoamRoutine = null; }
        isNightRoaming = false;
        transform.rotation = initialRot;
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

        if (GameManager.Instance.TimeUntilNight() <= 30f && !isNightRoaming)
        {
            OnNightApproaching();
            return;
        }

        // 전투 중이 아닐 때만 타겟 탐지
        if (!isChasing && target == null)
        {
            ValidateCurrentTarget();
            if (target == null)
            {
                TryFindNewTarget();
            }
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
                Debug.Log($"승무원 {marinerId}: 적 발견으로 파밍 중단");
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
            if (GameManager.Instance.TimeUntilNight() <= 30f)
            {
                OnNightApproaching();
            }
            else
            {
                StartRepair();
            }
        }
    }

    private void CancelCurrentRepair()
    {
        isSecondPriorityStarted = false;
        if (isRepairing)
        {
            isRepairing = false;
            
            if (targetRepairObject != null)
            {
                MarinerManager.Instance.ReleaseRepairObject(targetRepairObject);
                targetRepairObject = null;
            }

            StopAllCoroutines();

            ResetAgentPath();
            Debug.Log($"승무원 {marinerId}: 수리 취소");
        }
    }

    protected void CancelSecondPriorityAction()
    {
        if (!isSecondPriorityStarted) return;
        isSecondPriorityStarted = false;
        if (secondPriorityRoutine != null)
        {
            StopCoroutine(secondPriorityRoutine);
            secondPriorityRoutine = null;
        }
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: 2순위 작업 취소");
    }


    private void HandleNightCombat()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;

        // 전투 중이 아닐 때만 타겟 탐지
        if (!isChasing && target == null)
        {
            ValidateCurrentTarget();
            if (target == null)
            {
                TryFindNewTarget();
            }
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

        var animCtrl = GetComponentInChildren<MarinerAnimControll>(true);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.SamshSound);

        // 항상 정리 보장
        try
        {
            if (target == null)
            {
                attackRoutine = null;
                isChasing = false;
                yield break;
            }

            ResetAgentPath();
            LookAtTarget(); 

            // 타겟 바라보게 DirX/DirZ 스냅 + 공격 트리거
            animCtrl?.AimAtTarget(target.position, transform);
            animCtrl?.PlayAttackOnce();

            isShowingAttackBox = true;
            yield return new WaitForSeconds(attackDelayTime);
            isShowingAttackBox = false;

            // 실제 타격
            PerformMarinerAttack();
            attackCooldown = attackInterval;

            // 타겟 상태 확인 후 분기
            if (target != null)
            {
                CommonBase targetBase = target.GetComponent<CommonBase>();
                if (targetBase != null && !targetBase.IsDead)
                {
                    Debug.Log($"승무원 {marinerId}: 공격 완료, 추격 재개");
                    EnterChasingState();
                    yield break;
                }
                else
                {
                    Debug.Log($"승무원 {marinerId}: 적 처치 완료");
                    target = null;
                    isChasing = false;
                    HandlePostCombatAction();
                    yield break;
                }
            }
            else
            {
                Debug.Log($"승무원 {marinerId}: 적 소실");
                isChasing = false;
                HandlePostCombatAction();
                yield break;
            }
        }
        finally
        {
            // 어떤 경로로 끝나든 공격 상태 정리
            animCtrl?.EndAttack();   // IsAttacking=false → Idle 전이 허용
            animCtrl?.ClearAim();    // 이동 기반 Dir 업데이트 복귀
            attackRoutine = null;
        }
    }



    private void PerformMarinerAttack()
    {
        // 전방 박스 중심과 반경(반쪽 크기) 계산
        float half = Mathf.Max(0.5f, attackRange * 0.5f);
        Vector3 boxCenter = transform.position + transform.forward * half;
        Vector3 halfExtents = new Vector3(half, 1f, half);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            transform.rotation,
            targetLayer
        );

        foreach (var hit in hits)
        {
            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(attackDamage, this.gameObject);
                Debug.Log($"승무원이 {hit.name}에게 {attackDamage} 데미지");
            }
        }
    }


    public override IEnumerator StartSecondPriorityAction()
    {
        if (isSecondPriorityStarted) yield break;
        isSecondPriorityStarted = true;

        if (!GameManager.Instance.IsDaytime || GameManager.Instance.TimeUntilNight() <= 30f)
        {
            isSecondPriorityStarted = false;
            OnNightApproaching();
            yield break;
        }
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
                    else // 보관함 구현 후 삭제? or 에러처리? 
                    {
                        Debug.LogWarning("보관함에 InventoryBase가 없음 - 아이템 제거, 보관함 구현 후 삭제?");

                        List<SItemStack> itemsToRemove = new List<SItemStack>();
                        for (int i = 0; i < inventory.itemLists.Count; i++)
                        {
                            if (inventory.itemLists[i] != null)
                            {
                                itemsToRemove.Add(new SItemStack(inventory.itemLists[i].id, inventory.itemLists[i].amount));
                            }
                        }

                        if (itemsToRemove.Count > 0)
                        {
                            inventory.Remove(itemsToRemove.ToArray());
                        }
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
                Debug.LogWarning($"승무원 {marinerId}: 보관함을 찾을 수 없음 - 3초간 랜덤 이동 후 재시도");

                SetRandomDestination();

                yield return new WaitForSeconds(3f); // 움직이고 3초 대기

                isSecondPriorityStarted = false;
                StartRepair();
                yield break;
            }
        }
        else
        {
            Debug.Log($"승무원 {marinerId}: 개인 경계 탐색 및 파밍 시작");
            isSecondPriorityStarted = true;
            yield return StartCoroutine(MoveToMyEdgeAndFarm());

            var needRepairList = MarinerManager.Instance.GetNeedsRepair();
            isSecondPriorityStarted = false;

            if (needRepairList.Count > 0)
                StartRepair();

            yield break;
        }

    }

    protected override float GetRepairSuccessRate()
    {
        return 1.0f; // 100% 성공률
    }

    protected override void OnNightApproaching()
    {
        // 이미 돌고 있으면 재시작 금지
        if (nightRoamRoutine != null) return;
        nightRoamRoutine = StartCoroutine(NightApproachRoutine());
    }


    public override void WhenDestroy()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.Die);

        GameManager.Instance.MarinerDiedCount();
        PlayerCore.Instance.ReduceMentalOnMarinerDie();
        base.WhenDestroy();
    }

    protected override void OnPersonalFarmingCompleted()
    {
        int acquiredItemID = GetRandomItemIDByProbability(FixedItemDrops);

        MarinerInventory inventory = GetComponent<MarinerInventory>();

        if (inventory != null)
        {
            // 만약 acquiredItemID -> 보물상자가 아님 -> 그냥 받음
            // 만약 acquiredItemID -> 보물상자가 맞음 ->  TreasureBoxManager SItemStack GetReward()

            //int amount = 1; // 기본 1개

            if (acquiredItemID == 30009)
            {
                SItemStack treasure = TreasureBoxManager.instance.GetReward();
                acquiredItemID = treasure.id;
                //amount = treasure.amount;
            }

            bool result = inventory.AddItem(acquiredItemID, 1);

            // 바디이벤트 녹조로 얻는 추가 아이템 획득 
            if (OceanEventManager.instance.currentEvent is OceanEventWaterBloom)
            {
                OceanEventWaterBloom waterBloomEnvent = OceanEventManager.instance.currentEvent as OceanEventWaterBloom;

                SItemTypeSO bonusItem = waterBloomEnvent.GetMoreItem();

                if (bonusItem != null)
                {
                    inventory.AddItem(bonusItem.id, 1);
                }
            }

            Debug.Log($"AddItem 결과: {result}, 획득 아이템 ID: {acquiredItemID}");
        }
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.ItemGet);

        Debug.Log($"승무원 {marinerId}: 개인 경계에서 자원 수집 완료");
    }

    private int GetRandomItemIDByProbability(ItemDrop[] dropList)
    {
        float randomValue = Random.value; // 0.0에서 1.0 사이의 무작위 값
        float cumulativeProbability = 0f;

        foreach (var drop in dropList)
        {
            cumulativeProbability += drop.probability;

            if (randomValue <= cumulativeProbability)
            {
                return drop.itemID;
            }
        }

        Debug.LogError("확률 계산 오류: 아이템이 선택되지 않았습니다. 첫 번째 아이템 반환.");
        return dropList.Length > 0 ? dropList[0].itemID : 0;
    }

    public void RestartNormalAI()
    {
        if (!IsDead)
        {
            isCharmed = false;
            StopAllCoroutines();
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    private IEnumerator NightApproachRoutine()
    {
        if (isNightRoaming) yield break;
        isNightRoaming = true;
        Debug.Log($"승무원 {marinerId}: 야간 루틴 시작 - 수납 시도 후 랜덤 이동");

        if (agent != null && agent.isOnNavMesh) agent.ResetPath();

        // 즉시 모든 작업 중단
        isSecondPriorityStarted = false;
        isRepairing = false;
        isChasing = false;
        target = null;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            isShowingAttackBox = false;
        }

        // 수납 로직 (필요 시)
        var inventory = GetComponent<MarinerInventory>();
        if (inventory != null && inventory.GetAllItem() > 0)
        {
            var storage = GameObject.FindWithTag("Engine");
            if (storage != null && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(storage.transform.position);
                while (!IsArrived())
                {
                    if (target != null || isChasing)
                    {   // 적 만나면 전투로
                        isNightRoaming = false;
                        nightRoamRoutine = null;
                        yield break;
                    }
                    yield return null;
                }

                var storageInventory = storage.GetComponent<InventoryBase>();
                if (storageInventory != null)
                {
                    inventory.TransferAllItemsToStorage(storageInventory);
                    Debug.Log($"승무원 {marinerId}: 보관함 도착 및 수납 완료");
                }
                else
                {
                    // 보관함에 InventoryBase 없음 → 제거 후 이동
                    var itemsToRemove = new List<SItemStack>();
                    for (int i = 0; i < inventory.itemLists.Count; i++)
                        if (inventory.itemLists[i] != null)
                            itemsToRemove.Add(new SItemStack(inventory.itemLists[i].id, inventory.itemLists[i].amount));
                    if (itemsToRemove.Count > 0) inventory.Remove(itemsToRemove.ToArray());
                }
            }
            else
            {
                // 보관함 없거나 agent 불가 → 일단 이동 시작
                if (agent != null && agent.isOnNavMesh) SetRandomDestination();
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            // 아이템 없음 → 바로 이동 시작
            if (agent != null && agent.isOnNavMesh) SetRandomDestination();
            yield return new WaitForSeconds(0.5f);
        }

        //  낮의 마지막 30초 + 밤 동안 계속 배회
        while (GameManager.Instance != null &&
               (GameManager.Instance.TimeUntilNight() <= 30f || !GameManager.Instance.IsDaytime))
        {
            if (target != null || isChasing) break;    // 적 만나면 전투로 전환
            if (agent != null && agent.isOnNavMesh)
            {
                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                    SetRandomDestination();
            }
            yield return new WaitForSeconds(3f);
        }

        isNightRoaming = false;
        nightRoamRoutine = null;
        Debug.Log($"승무원 {marinerId}: 야간 루틴 종료");
    }

    protected override IEnumerator PerformPersonalEdgeFarming() // 애니메이션을 위한 오버라이드
    {
        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: [마리너] 파밍 시작");

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        var anim = GetComponentInChildren<MarinerAnimControll>(true);

        // 이동 정지
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Vector3 dir = Vector3.zero;
        if (agent != null)
            dir = agent.desiredVelocity.sqrMagnitude > 0.0001f ? agent.desiredVelocity : agent.velocity;

        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward; // 거의 정지면 바라보는 방향 사용
        dir.y = 0f;

        Vector3 sideDir = (dir.x >= 0f) ? transform.right : -transform.right;
        if (anim != null) anim.StartFishing(transform.position + sideDir, transform);

        float endTime = Time.time + 10f;

        try
        {
            while (Time.time < endTime)
            {
                if (!isSecondPriorityStarted) yield break;

                if (GameManager.Instance.TimeUntilNight() <= 30f)
                {
                    OnNightApproaching();
                    yield break;
                }

                yield return null; // 프레임 대기
            }

            OnPersonalFarmingCompleted();
            hasFoundPersonalEdge = false;
        }
        finally
        {
            // 정리
            anim?.StopFishing();
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.GetFishing);
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }
    }


}