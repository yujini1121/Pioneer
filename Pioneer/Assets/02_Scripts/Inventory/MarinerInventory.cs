using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerInventory : InventoryBase
{
    [Header("마리너 인벤토리 설정")]
    [SerializeField] private int maxInventorySize = 10;
    [SerializeField] private int storageThreshold = 7;

    [Header("상호작용 설정")]
    [SerializeField] private bool canInteract = false;
    [SerializeField] private float interactionRange = 4f;
    [SerializeField] private float interactionHeight = 1f;

    private Transform playerTransform;
    private bool hasStorage = false;
    private Transform storageTransform;
    private MarinerBase marinerBase;
    private bool isMovingToStorage = false;
    private UnityEngine.AI.NavMeshAgent agent; // 추가

    // 마리너 타입 체크용
    private MarinerAI normalMariner;
    private InfectedMarinerAI infectedMariner;

    void Update()
    {
        // 리스트 초기화 체크
        if (itemLists == null || itemLists.Count == 0)
        {
            itemLists = new List<SItemStack>();
            for (int i = 0; i < maxInventorySize; i++)
            {
                itemLists.Add(null);
            }
        }

        // 상호작용 상태일 때만 마우스 클릭 체크
        if (canInteract && Input.GetMouseButtonDown(0))
        {
            CheckPlayerInteraction();
        }
    }

    void Start()
    {
        // 컴포넌트 참조
        marinerBase = GetComponent<MarinerBase>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); // 추가
        normalMariner = GetComponent<MarinerAI>();
        infectedMariner = GetComponent<InfectedMarinerAI>();

        // 플레이어 참조
        if (ThisIsPlayer.Player != null)
        {
            playerTransform = ThisIsPlayer.Player.transform;
        }

        // 보관함 찾기
        if (MarinerManager.Instance != null && MarinerManager.Instance.HasStorage())
        {
            hasStorage = true;
            GameObject storage = GameObject.FindWithTag("Engine");
            if (storage != null)
            {
                storageTransform = storage.transform;
            }
            else
            {
                hasStorage = false;
            }
        }
    }

    /// <summary>
    /// 현재 마리너가 감염된 상태인지 확인
    /// </summary>
    private bool IsInfectedMariner()
    {
        return infectedMariner != null && infectedMariner.enabled;
    }

    /// <summary>
    /// 아이템 개수가 임계값을 넘었는지 체크
    /// </summary>
    public bool ShouldMoveToStorage => !isMovingToStorage && GetAllItem() >= storageThreshold;

    /// <summary>
    /// 보관함으로 이동하여 아이템 저장/버리기
    /// </summary>
    public void StartMoveToStorage()
    {
        if (!ShouldMoveToStorage) return;

        isMovingToStorage = true;

        if (IsInfectedMariner())
        {
            HandleInfectedMarinerStorage();
        }
        else
        {
            HandleNormalMarinerStorage();
        }
    }

    /// <summary>
    /// 일반 승무원의 보관함 처리
    /// </summary>
    private void HandleNormalMarinerStorage()
    {
        if (hasStorage && storageTransform != null)
        {
            StartCoroutine(MoveToStorageCoroutine(false));
        }
        else
        {
            StartWandering();
            isMovingToStorage = false;
        }
    }

    /// <summary>
    /// 감염된 승무원의 보관함 처리
    /// </summary>
    private void HandleInfectedMarinerStorage()
    {
        if (hasStorage && storageTransform != null)
        {
            StartCoroutine(MoveToStorageCoroutine(true));
        }
        else
        {
            DiscardAllItems();
            StartWandering();
            isMovingToStorage = false;
        }
    }

    private IEnumerator MoveToStorageCoroutine(bool shouldDiscard)
    {
        Debug.Log($"보관함으로 이동 시작: 목표 위치 {storageTransform.position}");

        // NavMeshAgent를 사용해서 정확한 위치로 이동 (수리 방식과 동일)
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(storageTransform.position);

            while (!IsArrived())
            {
                yield return null;
            }

            Debug.Log("보관함에 도착했습니다.");
        }

        if (shouldDiscard)
        {
            DiscardAllItems();
        }
        else
        {
            DepositAllItemsToStorage();
        }

        StartWandering();
        isMovingToStorage = false;
    }

    /// <summary>
    /// MarinerBase의 IsArrived 메서드 사용
    /// </summary>
    private bool IsArrived()
    {
        if (marinerBase != null)
        {
            return marinerBase.IsArrived();
        }

        // fallback
        if (agent == null || !agent.isOnNavMesh) return true;
        return !agent.pathPending && agent.remainingDistance <= (agent.stoppingDistance + 0.5f);
    }

    /// <summary>
    /// 보관함에 모든 아이템 저장
    /// </summary>
    private void DepositAllItemsToStorage()
    {
        // 보관함 컴포넌트의 itemLists에 직접 접근해서 저장
        if (storageTransform != null)
        {
            var storageInventory = storageTransform.GetComponent<InventoryBase>();
            if (storageInventory != null)
            {
                // 보관함의 itemLists에 직접 추가
                for (int i = 0; i < itemLists.Count; i++)
                {
                    if (itemLists[i] != null)
                    {
                        Debug.Log($"보관함에 저장: {itemLists[i].id} x {itemLists[i].amount}");

                        // 보관함의 빈 슬롯 찾기
                        bool stored = false;
                        for (int j = 0; j < storageInventory.itemLists.Count; j++)
                        {
                            if (storageInventory.itemLists[j] == null)
                            {
                                storageInventory.itemLists[j] = new SItemStack(itemLists[i].id, itemLists[i].amount);
                                stored = true;
                                break;
                            }
                            // 같은 아이템이면 합치기
                            else if (storageInventory.itemLists[j].id == itemLists[i].id)
                            {
                                storageInventory.itemLists[j].amount += itemLists[i].amount;
                                stored = true;
                                break;
                            }
                        }

                        if (!stored)
                        {
                            Debug.LogWarning("보관함이 가득참!");
                        }

                        itemLists[i] = null;
                    }
                }
            }
            else
            {
                Debug.LogWarning("보관함에 InventoryBase 컴포넌트가 없습니다!");
                // 컴포넌트가 없으면 그냥 삭제
                for (int i = 0; i < itemLists.Count; i++)
                {
                    if (itemLists[i] != null)
                    {
                        itemLists[i] = null;
                    }
                }
            }
        }

        SafeClean();
        Debug.Log("일반 승무원이 모든 아이템을 보관함에 저장했습니다.");
    }

    /// <summary>
    /// 모든 아이템을 버리는 함수
    /// </summary>
    private void DiscardAllItems()
    {
        for (int i = 0; i < itemLists.Count; i++)
        {
            if (itemLists[i] != null)
            {
                itemLists[i] = null;
            }
        }
        SafeClean();
    }

    /// <summary>
    /// 배회 시작
    /// </summary>
    private void StartWandering()
    {
        // 기존 마리너 시스템의 배회 로직과 연동
    }

    /// <summary>
    /// 상호작용 가능 상태 설정
    /// </summary>
    public void SetInteractable(bool canInteract)
    {
        this.canInteract = canInteract;
    }

    /// <summary>
    /// 플레이어와의 상호작용 체크
    /// </summary>
    private void CheckPlayerInteraction()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 marinerPos = transform.position;

        float xDistance = Mathf.Abs(playerPos.x - marinerPos.x);
        float zDistance = Mathf.Abs(playerPos.z - marinerPos.z);
        float yDistance = Mathf.Abs(playerPos.y - marinerPos.y);

        bool inRange = (xDistance <= interactionRange &&
                       zDistance <= interactionRange &&
                       yDistance <= interactionHeight);

        if (inRange)
        {
            TransferAllItemsToPlayer();
        }
    }

    /// <summary>
    /// 모든 아이템을 플레이어에게 전달
    /// </summary>
    private void TransferAllItemsToPlayer()
    {
        if (InventoryManager.Instance == null) return;

        List<SItemStack> itemsToTransfer = new List<SItemStack>();

        for (int i = 0; i < itemLists.Count; i++)
        {
            if (itemLists[i] != null && itemLists[i].amount > 0)
            {
                itemsToTransfer.Add(new SItemStack(itemLists[i].id, itemLists[i].amount));
                itemLists[i] = null;
            }
        }

        foreach (var item in itemsToTransfer)
        {
            InventoryManager.Instance.Add(item);
        }

        SetInteractable(false);
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(SItemStack item)
    {
        return TryAddMariner(item);
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(int itemId, int amount)
    {
        return TryAddMariner(new SItemStack(itemId, amount));
    }

    /// <summary>
    /// 마리너 전용 TryAdd
    /// </summary>
    private bool TryAddMariner(SItemStack itemStack)
    {
        // 리스트 초기화 체크
        if (itemLists == null || itemLists.Count == 0)
        {
            itemLists = new List<SItemStack>();
            for (int i = 0; i < maxInventorySize; i++)
            {
                itemLists.Add(null);
            }
        }

        SafeClean();

        if (itemStack.amount < 1) return true;

        int firstEmpty = -1;
        for (int inventoryIndex = 0; inventoryIndex < itemLists.Count; ++inventoryIndex)
        {
            if (itemLists[inventoryIndex] == null)
            {
                if (firstEmpty == -1)
                {
                    firstEmpty = inventoryIndex;
                }
                continue;
            }
            if (itemLists[inventoryIndex].id == itemStack.id)
            {
                itemLists[inventoryIndex].amount += itemStack.amount;
                return true;
            }
        }
        if (firstEmpty != -1)
        {
            itemLists[firstEmpty] = new SItemStack(itemStack.id, itemStack.amount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 현재 보관함으로 이동 중인지 확인
    /// </summary>
    public bool IsMovingToStorage()
    {
        return isMovingToStorage;
    }

    /// <summary>
    /// 마리너 전용 SafeClean (amount 0인 아이템 제거)
    /// </summary>
    protected override void SafeClean()
    {
        for (int index = 0; index < itemLists.Count; ++index)
        {
            if (itemLists[index] == null)
            {
                continue;
            }
            if (itemLists[index].amount < 1 || itemLists[index].id == 0)
            {
                itemLists[index] = null;
            }
        }
    }
}