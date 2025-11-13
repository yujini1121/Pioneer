using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfectedMarinerAI : MarinerBase, IBegin
{
    [System.Serializable]
    public struct ItemDrop
    {
        public int itemID;
        public float probability;
    }

    // 고정 드랍 테이블 (확률 합은 1.0 근처 권장)
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

    public Animator animator;
    Quaternion initialRot;


    // 감염된 승무원 고유 설정
    public int marinerId;
    private bool hasTransformedToZombie = false; // 중복 전환 가드

    // 밤 혼란 관련
    private float nightConfusionTime; // 혼란 유지 시간(초)
    private bool isNight = false;
    private bool isConfused = false;
    private bool isNightBehaviorStarted = false;

    // 프리-나이트(밤 30초 전) 관리
    private Coroutine nightRoamRoutine = null;
    private bool isNightRoaming = false;

    // 2순위 로직 중복 방지
    private Coroutine secondPriorityRoutine = null;

    // 유틸 가드
    private bool _shuttingDown;
    private bool IsPreNightActive =>
        GameManager.Instance != null &&
        GameManager.Instance.IsDaytime &&
        GameManager.Instance.TimeUntilNight() <= 30f;

    private bool IsNightPhaseActive =>
        GameManager.Instance != null &&
        !GameManager.Instance.IsDaytime;

    private void EnsureAnimator()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        maxHp = 40;
        hp = 40;
        speed = 1f;
        attackDamage = 6;
        attackRange = 1.5f;
        attackDelayTime = 1f;

        fov = GetComponent<FOVController>();
        initialRot = transform.rotation;

        gameObject.layer = LayerMask.NameToLayer("Mariner");
    }

    public override void Start()
    {
        base.Start(); 
        if (fov != null) fov.Start();

        nightConfusionTime = 10f; //혼란 유지 시간

        if (GameManager.Instance != null && !GameManager.Instance.IsDaytime && !isNightBehaviorStarted && !isConfused)
        {
            isNight = true;
            StartCoroutine(NightBehaviorRoutine());
        }
    }

    private void Update()
    {
        if (IsDead) return;
        if (_shuttingDown || IsDead) return;

        // 프리-나이트: 다른 낮 로직을 모두 막고, 프리-나이트 루틴만 돌림
        if (IsPreNightActive)
        {
            if (!isNightRoaming && !isNightBehaviorStarted && !isConfused)
            {
                OnNightApproaching(); // 프리-나이트 루틴 시작
            }
            return; // 낮 로직(수리/파밍 재기동) 차단
        }

        // 밤 시작 감지 및 혼란→좀비 전환 루틴 트리거
        if (GameManager.Instance != null && GameManager.Instance.IsDaytime && !isNightBehaviorStarted)
        {

            isNight = false;

            // 낮이고 프리-나이트도 아니면: 평상시 루틴
            if (!isRepairing && !IsPreNightActive)
            {
                StartRepair();
            }
        }
        else if (!isNight) // 낮→밤 전환 시점
        {
            isNight = true;
            StartCoroutine(NightBehaviorRoutine());
        }
    }
    
    /// <summary>
    /// 감염된 승무원 2순위 로직
    /// </summary>
    /// <returns></returns>
    public override IEnumerator StartSecondPriorityAction()
    {
        if (IsPreNightActive || IsNightPhaseActive || isNightRoaming || isNightBehaviorStarted || isConfused)
        {
            Debug.Log($"감염승무원 {marinerId}: 파밍 시작 거부 (프리-나이트/야간 상태)");
            yield break;
        }

        secondPriorityRoutine = StartCoroutine(SecondPriorityBody());
        yield return secondPriorityRoutine;
        secondPriorityRoutine = null;
    }

    /// <summary>
    /// 감염된 승무원 아이템 가득 찼을 시 아이템 정리 로직
    /// </summary>
    /// <returns></returns>
    private IEnumerator SecondPriorityBody()
    {
        // 인벤토리 가득 시: 보관함으로 이동(감염자는 결국 버림)
        MarinerInventory inventory = GetComponent<MarinerInventory>();
        if (inventory != null && inventory.ShouldMoveToStorage())
        {
            Debug.Log($"감염된 승무원 {marinerId}: 인벤토리가 가득함 ({inventory.GetAllItem()}개) - 아이템 처리");

            GameObject storage = GameObject.FindWithTag("Engine");
            if (storage != null)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(storage.transform.position);

                    while (!IsArrived())
                    {
                        // 다른 곳에서 중단되면 즉시 종료
                        if (!isSecondPriorityStarted) yield break;

                        //프리-나이트 진입 시 즉시 이탈
                        if (IsPreNightActive)
                        {
                            Debug.Log("감염된 승무원 밤되기 30초전 작동 → 2순위 즉시 중단");
                            OnNightApproaching();
                            yield break;
                        }
                        yield return null;
                    }

                    Debug.Log($"감염된 승무원 {marinerId}: 보관함 도착 - 아이템 버림");

                    // 감염자는 저장하지 않고 버림
                    List<SItemStack> itemsToRemove = new List<SItemStack>();
                    for (int i = 0; i < inventory.itemLists.Count; i++)
                        if (inventory.itemLists[i] != null)
                            itemsToRemove.Add(new SItemStack(inventory.itemLists[i].id, inventory.itemLists[i].amount));

                    if (itemsToRemove.Count > 0)
                        inventory.Remove(itemsToRemove.ToArray());

                    Debug.Log($"감염된 승무원 {marinerId}: 아이템 버림 완료 - 1순위 행동 재확인");

                    isSecondPriorityStarted = false;
                    StartRepair();
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"감염된 승무원 {marinerId}: 보관함을 찾을 수 없음 - 3초간 랜덤 이동 후 재시도");

                if (agent != null && agent.isOnNavMesh)
                    SetRandomDestination();
                yield return new WaitForSeconds(3f);

                isSecondPriorityStarted = false;
                StartRepair();
                yield break;
            }
        }
        else
        {
            Debug.Log($"감염된 승무원 {marinerId}: 개인 경계에서 가짜 파밍");

            yield return StartCoroutine(MoveToMyEdgeAndFarm());

            if (IsPreNightActive || IsNightPhaseActive || isNightRoaming || isNightBehaviorStarted || isConfused)
                yield break;

            var needRepairList = MarinerManager.Instance.GetNeedsRepair();
            if (needRepairList.Count > 0)
            {
                isSecondPriorityStarted = false;
                StartRepair();
            }
            else // 재시작 가드
            {
                if (!(IsPreNightActive || IsNightPhaseActive || isNightRoaming || isNightBehaviorStarted || isConfused))
                    StartCoroutine(StartSecondPriorityAction());
            }
        }
    }

    /// <summary>
    /// 밤 30초전 작동하는 로직 
    /// </summary>
    protected override void OnNightApproaching()
    {
        // 좀비 변신 루틴이 이미 시작되었거나 혼란 중이면 프리-나이트 불필요
        if (isNightBehaviorStarted || isConfused) return;

        if (nightRoamRoutine == null)
            nightRoamRoutine = StartCoroutine(NightApproachRoutine_Infected());
    }

    private IEnumerator NightApproachRoutine_Infected()
    {
        if (isNightRoaming) yield break;
        isNightRoaming = true;
        Debug.Log($"감염 승무원 {marinerId}: 프리-나이트 루틴 시작 (공격 금지, 수납 후 랜덤 이동)");

        // 경로 초기화
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();

        isSecondPriorityStarted = false;
        isRepairing = false;
        isChasing = false;
        target = null;

        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        isShowingAttackBox = false;

        if (secondPriorityRoutine != null) { StopCoroutine(secondPriorityRoutine); secondPriorityRoutine = null; }

        // 아이템 수납 시도 (보관함 없거나 접근 불가면 전량 버림)
        var inventory = GetComponent<MarinerInventory>();
        if (inventory != null && inventory.GetAllItem() > 0)
        {
            var storage = GameObject.FindWithTag("Engine");
            if (storage != null && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(storage.transform.position);

                while (GameManager.Instance != null &&
                       GameManager.Instance.IsDaytime &&
                       GameManager.Instance.TimeUntilNight() > 0f &&
                       !IsArrived())
                {
                    target = null;   
                    isChasing = false;
                    yield return null;
                }

                // 밤 시작되면 프리-나이트 종료
                if (GameManager.Instance != null && !GameManager.Instance.IsDaytime) goto END;

                var storageInventory = storage.GetComponent<InventoryBase>();
                if (storageInventory != null)
                {
                    inventory.TransferAllItemsToStorage(storageInventory);
                    Debug.Log($"감염 승무원 {marinerId}: 보관함 수납 완료");
                }
                else
                {
                    var itemsToRemove = new List<SItemStack>();
                    for (int i = 0; i < inventory.itemLists.Count; i++)
                        if (inventory.itemLists[i] != null)
                            itemsToRemove.Add(new SItemStack(inventory.itemLists[i].id, inventory.itemLists[i].amount));
                    if (itemsToRemove.Count > 0) inventory.Remove(itemsToRemove.ToArray());
                    Debug.Log($"감염 승무원 {marinerId}: 보관함 인벤토리 없음 → 아이템 버림");
                }
            }
            else
            {
                var itemsToRemove = new List<SItemStack>();
                for (int i = 0; i < inventory.itemLists.Count; i++)
                    if (inventory.itemLists[i] != null)
                        itemsToRemove.Add(new SItemStack(inventory.itemLists[i].id, inventory.itemLists[i].amount));
                if (itemsToRemove.Count > 0) inventory.Remove(itemsToRemove.ToArray());
                Debug.Log($"감염 승무원 {marinerId}: 보관함 접근 불가 → 아이템 버림");
            }
        }

        if (agent != null && agent.isOnNavMesh)
        {
            SetRandomDestination();
            yield return new WaitForSeconds(0.25f);

            while (GameManager.Instance != null &&
                   GameManager.Instance.IsDaytime &&
                   GameManager.Instance.TimeUntilNight() > 0f)
            {
                target = null; // 교전 금지 유지
                isChasing = false;

                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                    SetRandomDestination();

                yield return new WaitForSeconds(2.5f);
            }
        }

    END:
        isNightRoaming = false;
        nightRoamRoutine = null;
        Debug.Log($"감염 승무원 {marinerId}: 프리-나이트 루틴 종료 (밤 시작 또는 완료)");

        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        ChangeToZombieAI();
    }

    /// <summary>
    /// 밤이 시작하면 혼란로직이 발동하고 혼란 종료 후 좀비로 변경
    /// </summary>
    /// <returns></returns>
    private IEnumerator NightBehaviorRoutine()
    {
        isNightBehaviorStarted = true;
        isConfused = true;
        Debug.Log("혼란 상태 시작 - NavMesh로 이동");

        float escapedTime = 0f;

        float angle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            while (escapedTime < nightConfusionTime)
            {
                escapedTime += Time.deltaTime;
                Vector3 targetPosition = transform.position + direction * 10f;
                agent.SetDestination(targetPosition);
                yield return new WaitForSeconds(0.1f);
            }

            agent.isStopped = true;
        }
        else
        {
            while (escapedTime < nightConfusionTime)
            {
                escapedTime += Time.deltaTime;
                transform.position += direction * speed * Time.deltaTime;
                yield return null;
            }
        }

        isConfused = false;
        Debug.Log("혼란 종료 후 좀비 AI로 변경");

        if (agent != null) agent.ResetPath();

        ChangeToZombieAI();
    }

    private void ChangeToZombieAI()
    {
        if (hasTransformedToZombie) return;
        hasTransformedToZombie = true;

        Debug.Log("좀비 변신 전 랜덤 이동 시작");

        SetRandomDestination();

        StartCoroutine(TransformAfterDelay());
    }

    private IEnumerator TransformAfterDelay()
    {
        yield return new WaitForSeconds(5f);

        _shuttingDown = true;

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
        if (secondPriorityRoutine != null) { StopCoroutine(secondPriorityRoutine); secondPriorityRoutine = null; }
        if (nightRoamRoutine != null) { StopCoroutine(nightRoamRoutine); nightRoamRoutine = null; }
        isSecondPriorityStarted = false;
        isRepairing = false;
        isChasing = false;
        target = null;

        EnsureAnimator();
        var animCtrl = GetComponentInChildren<MarinerAnimControll>(true);
        if (animCtrl != null)
        {
            animCtrl.EndAttack();      // IsAttacking=false
            animCtrl.StopFishing();    // IsFishing=false
            animCtrl.ClearAim();

            animCtrl.SetZombieModeTrigger();  

        }
        else
        {
            Debug.Log("setzombiemode불가");
        }

        var zombieAI = GetComponent<ZombieMarinerAI>();
        if (zombieAI == null)
            zombieAI = gameObject.AddComponent<ZombieMarinerAI>();

        zombieAI.marinerId = marinerId;
        zombieAI.targetLayer = LayerMask.GetMask("Mariner", "Player");
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        var inv = GetComponent<MarinerInventory>();
        if (inv == null) inv = GetComponentInChildren<MarinerInventory>(true);
        if (inv != null)
        {
            inv.ShutdownUI();
            inv.enabled = false;
        }

        // 자신(감염 전 AI) 정리
        var oldAI = this;
        oldAI.enabled = false;
        Destroy(oldAI);
    }


    /// <summary>
    /// 파밍 완료시
    /// </summary>
    protected override void OnPersonalFarmingCompleted()
    {
        if (IsPreNightActive || IsNightPhaseActive || isNightRoaming || isNightBehaviorStarted || isConfused)
            return;

        int acquiredItemID = GetRandomItemIDByProbability(FixedItemDrops);
        MarinerInventory inventory = GetComponent<MarinerInventory>();
        if (inventory != null)
        {
            bool result = inventory.AddItem(acquiredItemID, 1);
            Debug.Log($"AddItem 결과: {result}, 획득 아이템 ID: {acquiredItemID}");
        }
        Debug.Log($"감염된 승무원 {marinerId}: 개인 경계에서 가짜 파밍 완료");
    }

    private int GetRandomItemIDByProbability(ItemDrop[] dropList)
    {
        float randomValue = Random.value; // [0,1)
        float cumulativeProbability = 0f;

        foreach (var drop in dropList)
        {
            cumulativeProbability += drop.probability;
            if (randomValue <= cumulativeProbability)
                return drop.itemID;
        }

        Debug.LogError("확률 합 불일치: 첫 번째 아이템 반환.");
        return dropList.Length > 0 ? dropList[0].itemID : 0;
    }

    protected override IEnumerator PerformPersonalEdgeFarming()
    {
        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()}: [감염] 가짜 파밍(낚시) 시작");

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        var anim = GetComponentInChildren<MarinerAnimControll>(true);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Vector3 dir = agent != null && agent.desiredVelocity.sqrMagnitude > 0.1f
            ? agent.desiredVelocity
            : transform.forward;
        dir.y = 0f;

        Vector3 sideDir = (dir.x >= 0f) ? transform.right : -transform.right;
        if (anim != null) anim.StartFishing(transform.position + sideDir, transform);

        float endTime = Time.time + 10f;
        try
        {
            while (Time.time < endTime)
            {
                if (!isSecondPriorityStarted) yield break;
                if (IsPreNightActive || IsNightPhaseActive || isNightRoaming || isNightBehaviorStarted || isConfused) yield break;
                yield return null;
            }

            OnPersonalFarmingCompleted();
            hasFoundPersonalEdge = false;
        }
        finally
        {
            anim?.StopFishing();
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.GetFishing);
    }



    /// <summary>
    /// 기타
    /// </summary>
    /// <returns></returns>

    // 감염된 승무원은 30% 수리 성공
    protected override float GetRepairSuccessRate() => 0.3f;

    protected override int GetMarinerId() => marinerId;

    protected override string GetCrewTypeName() => "감염승무원";

    
}
