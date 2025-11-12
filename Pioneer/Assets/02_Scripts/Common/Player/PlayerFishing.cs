using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFishing : MonoBehaviour
{
    public static PlayerFishing instance;

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

    private int fishingExp = 5;

    private CreatureEffect creatureEffect;
    private void Awake()
    {
        instance = this;
        creatureEffect = PlayerCore.Instance.GetComponent<CreatureEffect>();
    }

    // 현재 낚시 중인지 확인
    public void StartFishingLoop()
    {
        if(fishingLoopCoroutine == null)
        {
            // 낚시 중이 아니면 낚시 시작
            fishingLoopCoroutine = StartCoroutine(FishingLoop());
        }
    }

    public void StopFishingLoop()
    {
        if (fishingLoopCoroutine != null)
        {
            creatureEffect.Effects[5].Stop();
            creatureEffect.Effects[3].Stop();
            StopCoroutine(fishingLoopCoroutine);
            fishingLoopCoroutine = null;
            Debug.Log("낚시 중단");
        }
    }

    // 낚시로 아이템 추가하는 코드
    private IEnumerator FishingLoop()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        while (true)
        {
            // 낚시 시작
            Debug.Log("낚시 시작");
            creatureEffect.Effects[5].Play();
            yield return new WaitForSeconds(2f);

            // 아이템 획득
            SItemTypeSO caughtItem = GetItem();
            if(caughtItem != null)
            {                
                SItemStack itemStack = new SItemStack(caughtItem.id, 1);

                if(caughtItem == treasureItem)
                {
                    TreasureBoxManager.instance.GetBox();
                    fishingExp = 10;
                }
                else
                {
                    fishingExp = 5;
                    InventoryManager.Instance.Add(itemStack);
                }

                PlayerStatsLevel.Instance.AddExp(GrowStatType.Fishing, fishingExp);
                Debug.Log($">> PlayerFishing.FishingLoop() 아이템 획득: 숫자 {caughtItem.id}, 이름 {caughtItem.typeName}, 경험치 +{fishingExp}");

                (float extraItemChance, float treasureChestChance) chances = PlayerStatsLevel.Instance.FishingChance();

                if(Random.Range(0f, 1f) < chances.extraItemChance)
                {
                    if (caughtItem == treasureItem)
                    {
                        TreasureBoxManager.instance.GetBox();
                    }
                    else
                    {
                        InventoryManager.Instance.Add(itemStack);
                    }
                    Debug.Log($"<color=cyan>[낚시 레벨 보너스!]</color> {caughtItem.typeName}을(를) 추가로 획득했습니다! (확률: {chances.extraItemChance * 100:F2}%)");
                }

                if(Random.Range(0f, 1f) < chances.treasureChestChance)
                {
                    if(treasureItem != null)
                    {
                        //SItemStack treasureItemStack = new SItemStack(treasureItem.id, 1);
                        //InventoryManager.Instance.Add(treasureItemStack);

                        TreasureBoxManager.instance.GetBox();
                        Debug.Log($"<color=yellow>[낚시 레벨 보너스!]</color> 보물상자를 추가로 획득했습니다! (확률: {chances.treasureChestChance * 100:F2}%)");
                    }
                }

                // 바디이벤트 녹조로 얻는 추가 아이템 획득 
                if(OceanEventManager.instance.currentEvent is OceanEventWaterBloom)
                {
                    OceanEventWaterBloom waterBloomEnvent = OceanEventManager.instance.currentEvent as OceanEventWaterBloom;

                    SItemTypeSO bonusItem = waterBloomEnvent.GetMoreItem();

                    if(bonusItem != null)
                    {
                        SItemStack waterBloombonusItemStack = new SItemStack(bonusItem.id, 1);
                        InventoryManager.Instance.Add(waterBloombonusItemStack);
                    }
                }

                creatureEffect.Effects[3].Play();
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
        // 2. 0 ~ 전체 가중치사이 랜덤 숫자 뽑기
        float randomNum = Random.Range(0f, totalProbability);
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