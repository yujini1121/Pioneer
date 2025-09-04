using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 돗대 시스템 관리자 (싱글톤)
public class MastManager : MonoBehaviour
{
    public static MastManager Instance;

    [Header("갑판 관리")]
    public int currentDeckCount = 0;
    public GameObject deckPrefab; // PF_ItemDeck 프리팹

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
        // 시작시 현재 갑판 개수 계산
        UpdateCurrentDeckCount();
    }

    // 인벤토리에서 특정 아이템 개수 확인
    public int GetItemCount(int itemID)
    {
        if (InventoryManager.Instance == null) return 0;

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

    // 인벤토리에서 특정 아이템 소모
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

    // 현재 갑판 개수 업데이트 (설치/제거시에만 호출)
    public void UpdateCurrentDeckCount()
    {
        GameObject[] decks = GameObject.FindGameObjectsWithTag("Deck");
        currentDeckCount = decks.Length;
        Debug.Log($"현재 갑판 개수: {currentDeckCount}");
    }

    // 갑판 설치 가능한지 확인
    public bool CanBuildDeck(MastSystem mast)
    {
        int maxDecks = mast.GetMaxDeckCount();
        return currentDeckCount < maxDecks && GetItemCount(woodItemID) >= 30 && GetItemCount(clothItemID) >= 15;
    }

    // 갑판 건설
    public bool BuildDeck(MastSystem mast, Vector3 position)
    {
        if (!CanBuildDeck(mast))
        {
            if (currentDeckCount >= mast.GetMaxDeckCount())
            {
                mast.ShowMessage("갑판을 더이상 설치할 수 없다.", 4f);
            }
            else
            {
                mast.ShowMessage($"재료가 부족합니다.", 3f);
            }
            return false;
        }

        // 자원 소모
        if (!ConsumeItems(woodItemID, 30) || !ConsumeItems(clothItemID, 15))
        {
            mast.ShowMessage("아이템 소모에 실패했습니다.", 3f);
            return false;
        }

        // 갑판 생성
        GameObject newDeck = Instantiate(deckPrefab, position, Quaternion.identity);
        newDeck.layer = LayerMask.NameToLayer("Platform"); // 레이어로 변경 (실제 레이어명에 맞게 수정)

        // 갑판 수 업데이트
        UpdateCurrentDeckCount();

        Debug.Log("갑판 건설 완료!");

        // 인벤토리 UI 새로고침
        if (InventoryUiMain.instance != null)
        {
            InventoryUiMain.instance.IconRefresh();
        }

        return true;
    }

    // 게임오버 처리
    public void GameOver()
    {
        Debug.Log("게임오버! 돛대가 파괴되었습니다.");
        Time.timeScale = 0f;
    }
}