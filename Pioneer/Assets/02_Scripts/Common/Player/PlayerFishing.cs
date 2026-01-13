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

    // PlayerFishing.cs
    public void BeginFishing(Vector3 dir)
    {
        // 좌/우만 사용: x>=0 → 1(오른쪽), x<0 → 0(왼쪽). 정지면 마지막 값 유지되므로 1로 처리
        int idx = (Mathf.Abs(dir.x) < 1e-6f) ? 1 : (dir.x >= 0f ? 1 : 0);

        // 안전장치: 리스트가 2개 미만이면 0으로 강제
        var slots = PlayerCore.Instance.GetComponent<PlayerController>().animSlots;
        int maxReady = (slots.fising != null) ? Mathf.Max(0, slots.fising.Count - 1) : 0;
        int maxHold = (slots.fisingHold != null) ? Mathf.Max(0, slots.fisingHold.Count - 1) : 0;
        idx = Mathf.Clamp(idx, 0, Mathf.Min(maxReady, maxHold));

        //PlayerCore.Instance.SetState(PlayerCore.PlayerState.ActionFishing);
        PlayerCore.Instance.FishingReady(new Vector3(idx == 1 ? 1f : -1f, 0, 0));
        //PlayerCore.Instance.FishingHold(new Vector3(idx == 1 ? 1f : -1f, 0, 0));
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
            //creatureEffect.Effects[5].Stop();
            //creatureEffect.Effects[3].Stop();
            StopCoroutine(fishingLoopCoroutine);
            fishingLoopCoroutine = null;
        }
        PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);
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

            if (CreatureEffect.Instance != null)
            {
                ParticleSystem ps = CreatureEffect.Instance.Effects[8];
                CreatureEffect.Instance.PlayEffect(ps, PlayerCore.Instance.transform.position + new Vector3(0f, -0.8f, 0.3f));
            }
            yield return new WaitForSeconds(2f);

            // 아이템 획득
            SItemTypeSO caughtItem = GetItem();
            if(caughtItem != null)
            {                
                SItemStack itemStack = new SItemStack(caughtItem.id, 1);

                if(caughtItem == treasureItem)
                {
                    TreasureBoxManager.instance.GetBox();
                    fishingExp = 4;
                }
                else
                {
                    fishingExp = 2;
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

                //creatureEffect.Effects[3].Play();
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