using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerInventory : InventoryBase
{
    [SerializeField] private int maxInventorySize = 10;
    [SerializeField] private int storageThreshold = 7;
    [SerializeField] private float interactionRange = 4f;

    [Header("UI")]
    [SerializeField] private SpriteRenderer exclamationMarkSprite;

    private Transform playerTransform;

    void Start()
    {
        if (ThisIsPlayer.Player != null)
        {
            playerTransform = ThisIsPlayer.Player.transform;
        }
    }

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

        CheckForExclamationMark();

        // 플레이어 상호작용 체크
        if (Input.GetMouseButtonDown(0))
        {
            CheckPlayerInteraction();
        }
    }

    public void ShutdownUI()
    {
        if (exclamationMarkSprite != null)
            exclamationMarkSprite.enabled = false;
    }
    /// <summary>
    /// 인벤토리에 아이템이 있는지 확인하고 느낌표 이미지를 제어합니다.
    /// </summary>
    private void CheckForExclamationMark()
    {
        if (exclamationMarkSprite == null) return;

        int totalItems = GetAllItem();
        bool shouldShowExclamation = totalItems > 0;

        if (exclamationMarkSprite.enabled != shouldShowExclamation)
        {
            exclamationMarkSprite.enabled = shouldShowExclamation;
        }
    }

    /// <summary>
    /// 플레이어와의 상호작용 체크 (4,4,4 범위) 임시 -> 4,1,4원래
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
                       yDistance <= interactionRange);

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
        Debug.Log("플레이어에게 아이템 전달 함수 호출");
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

        Debug.Log($"마리너 인벤토리의 모든 아이템을 플레이어에게 전달했습니다. (총 {itemsToTransfer.Count}개 종류)");
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(int itemId, int amount)
    {
        return TryAdd(new SItemStack(itemId, amount));
    }

    /// <summary>
    /// 아이템 개수가 임계값 이상인지 체크 (7개 이상)
    /// </summary>
    public bool ShouldMoveToStorage()
    {
        return GetAllItem() >= storageThreshold;
    }

    /// <summary>
    /// 보관함에 모든 아이템 저장 (MarinerAI에서 호출)
    /// </summary>
    public void TransferAllItemsToStorage(InventoryBase storageInventory)
    {
        Debug.Log("모든 아이템 보관함에 저장 함수 호출");
        if (storageInventory == null) ; // 오류 검증을 위한 return 제거

        for (int i = 0; i < itemLists.Count; i++)
        {
            if (itemLists[i] != null)
            {
                if (storageInventory.TryAdd(itemLists[i]))
                {
                    Debug.Log($"보관함에 저장: {itemLists[i].id} x {itemLists[i].amount}");
                    itemLists[i] = null;
                }
                else
                {
                    Debug.LogWarning("보관함이 가득참");
                    break;
                }
            }
        }
        SafeClean();
        Debug.Log("모든 아이템을 보관함에 저장 완료");
    }
}