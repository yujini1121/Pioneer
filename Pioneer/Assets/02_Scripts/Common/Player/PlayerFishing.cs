using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFishing : MonoBehaviour
{
    [System.Serializable]
    public struct FishingDropItem
    {
        public SItemStack itemStack;
        public float dropProbability;
    }

    [Header("낚시 아이템 드랍 테이블")]
    public List<FishingDropItem> dropItemTable;

    [Header("낚시 아이템 경험치")]
    private int fishingExp = 5;
    private int treasureChestExp = 10;

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
            GetItem();
            // PlayerStatsLevel.Instance.AddExp(); => 아이템 결정되면 해당 아이템에따라 경험치 부여?
            Debug.Log("낚시 끝");
        }
    }

    private void GetItem()
    {
        float totalProbability = 0f;
        // 1. 전체 가중치 합 계산
        for (int i = 0; i < dropItemTable.Count; i++)
        {
            totalProbability += dropItemTable[i].dropProbability;
        }
        // 2. 0 ~ 전체 가중치사이 랜덤 숫자 뽑기
        float randomNum = Random.Range(0f, totalProbability);
        // 3. 랜덤 숫자가 현재 아이템의 가중치 보다 작으면 당첨
        foreach(var item in dropItemTable)
        {
            if(randomNum <= item.dropProbability)
            {
                // 아이템 획득 로직
                Debug.Log("아이템 획득");
                return;
            }
            // 4. 당첨되지않았으면 현재 아이템 가중치를 빼고 다음 아이템으로 넘어감
            randomNum -= item.dropProbability;
        }
    }
}