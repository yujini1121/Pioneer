using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFishing : MonoBehaviour
{
    [System.Serializable]
    public struct FishingDropItem
    {
        public SItemTypeSO itemData;
        public float dropProbability;
    }

    [Header("낚시 아이템 드랍 테이블")]
    public List<FishingDropItem> dropItemTable;

    [Header("보물 아이템")]
    public SItemTypeSO treasureItem;

    private Coroutine fishingLoopCoroutine;

    private void Awake()
    {

    }

    public void StartFishingLoop()
    {
        if(fishingLoopCoroutine == null)
        {
            fishingLoopCoroutine = StartCoroutine(FishingLoop());
        }
    }

    public void StopFishingLoop()
    {
        if (fishingLoopCoroutine != null)
        {
            StopCoroutine(fishingLoopCoroutine);
            fishingLoopCoroutine = null;
            Debug.Log("낚시 중단");
        }
    }

    private IEnumerator FishingLoop()
    {        
        while (true)
        {
            Debug.Log("낚시 시작");
            yield return new WaitForSeconds(2f);
            SItemTypeSO caughtItem = GetItem();
            if(caughtItem != null)
            {                
                SItemStack itemStack = new SItemStack(caughtItem.id, 1);
                InventoryManager.Instance.Add(itemStack);
                PlayerStatsLevel.Instance.AddExp(GrowStatType.Fishing, 5);
                Debug.Log($"아이템 획득: {caughtItem.typeName}, 경험치 +{5}");
            }
            else
            {
                Debug.LogError("아이템 획득에 실패했습니다. 드랍 테이블을 확인해주세요.");
            }

            Debug.Log("낚시 끝");
        }
    }

    private SItemTypeSO GetItem()
    {
        Debug.Log("아이템 얻기 시작");
        float totalProbability = 0f;
        // 1. 전체 가중치 합 계산
        for (int i = 0; i < dropItemTable.Count; i++)
        {
            totalProbability += dropItemTable[i].dropProbability;
        }

        if (totalProbability <= 0)
        {
            return dropItemTable[0].itemData;
        }
        Debug.Log($"가중치 계산 끝 : {totalProbability}");
        // 2. 0 ~ 전체 가중치사이 랜덤 숫자 뽑기
        float randomNum = Random.Range(0f, totalProbability);
        Debug.Log("랜덤 수 걸림");
        Debug.Log($"randomNum : {randomNum}");
        // 3. 랜덤 숫자가 현재 아이템의 가중치 보다 작으면 당첨
        foreach (var item in dropItemTable)
        {
            if(randomNum <= item.dropProbability)
            {
                return item.itemData;
            }
            // 4. 당첨되지않았으면 현재 아이템 가중치를 빼고 다음 아이템으로 넘어감
            randomNum -= item.dropProbability;
        }
        
        return dropItemTable[dropItemTable.Count - 1].itemData;
    }
}