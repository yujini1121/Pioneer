using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MastManager : MonoBehaviour
{
    public static MastManager Instance;

    [Header("갑판 관리")]
    public int currentDeckCount = 0;
    public LayerMask platformLayerMask; // 플랫폼 레이어마스크

    [Header("아이템 ID 설정")]
    public int woodItemID = 30001; // 통나무 아이템 ID
    public int clothItemID = 30003; // 천 아이템 ID

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateCurrentDeckCount();
    }

    public int GetItemCount(int itemID)
    {
        if (InventoryManager.Instance == null) return 0; // 인벤에서 계싼

        int count = 0;
        foreach (var item in InventoryManager.Instance.itemLists)
        {
            if (item != null && item.id == itemID)
            {
                count += item.amount;
            }
        }
        return count;
    }

    // 인벤토리에서 특정 아이템 소모 (돗대 강화용)
    public bool ConsumeItems(int itemID, int amount)
    {
        if (InventoryManager.Instance == null) return false;
        if (GetItemCount(itemID) < amount) return false;

        int remainingToConsume = amount;

        for (int i = 0; i < InventoryManager.Instance.itemLists.Count && remainingToConsume > 0; i++)
        {
            var item = InventoryManager.Instance.itemLists[i];
            if (item != null && item.id == itemID)
            {
                int consumeFromSlot = Mathf.Min(item.amount, remainingToConsume);
                item.amount -= consumeFromSlot;
                remainingToConsume -= consumeFromSlot;

                if (item.amount <= 0)
                {
                    InventoryManager.Instance.itemLists[i] = null;
                }
            }
        }

        return remainingToConsume == 0;
    }

    // 현재 갑판 개수 업데이트 (CreateObject에서 호출)
    public void UpdateCurrentDeckCount()
    {
        Collider[] platformColliders = Physics.OverlapSphere(Vector3.zero, 1000f, platformLayerMask);
        currentDeckCount = platformColliders.Length;

        Debug.Log($"=== 갑판 카운트 디버그 ===");
        Debug.Log($"레이어마스크 값: {platformLayerMask.value}");
        Debug.Log($"검색된 콜라이더 수: {platformColliders.Length}");
        Debug.Log($"현재 갑판 개수: {currentDeckCount}");

        // 각 갑판 정보 출력
        for (int i = 0; i < platformColliders.Length; i++)
        {
            Debug.Log($"갑판 {i}: {platformColliders[i].name} at {platformColliders[i].transform.position}");
        }
    }

    public void DecrementDeckCount(int destroyedCount)
    {
        currentDeckCount -= destroyedCount;

        if (currentDeckCount < 0)
        {
            currentDeckCount = 0;
        }

    }
    // 게임오버 처리
    public void GameOver()
    {
        Debug.Log("게임오버! 돛대가 파괴되었습니다.");
        Time.timeScale = 0f;
    }
}