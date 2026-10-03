using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
[ 바다이벤트 - 녹조 ]
- 하루종일 적용됨

- 바다에서 파밍시 80% 확률로 아이템 추가 획득
- 균등 랜덤 확률로 바다 파밍으로 얻을 수 있는 모든 아이템 중 1개 추가 획득
*/

public class OceanEventWaterBloom : OceanEventBase
{
    private int getMoreProbability = 50;

    List<PlayerFishing.FishingDropItem> getMoreDropItems;

    public OceanEventWaterBloom()
    {
        EventName = "녹조";
    }

    public override void EventRun()
    {
        base.EventRun();
        UtilityFunctions.Log("[OceanEventWaterBloom][녹조 이벤트 시작]");
    }

    public override void EventEnd()
    {
        base.EventEnd();
        UtilityFunctions.Log("[OceanEventWaterBloom][녹조 이벤트 종료]");
    }

    public SItemTypeSO GetMoreItem()
    {
        if (!IsRunning || PlayerFishing.instance == null) return null;
        getMoreDropItems = PlayerFishing.instance.dropItemTable;
        if (getMoreDropItems == null || getMoreDropItems.Count == 0) return null;

        if (Random.Range(0, 100) < getMoreProbability)
        {
            float totalWeight = 0f;
            foreach (var entry in getMoreDropItems)
                if (entry.itemData != null) totalWeight += Mathf.Max(0f, entry.dropProbability);
            if (totalWeight <= 0f) return null;
            float roll = Random.value * totalWeight;
            PlayerFishing.FishingDropItem bonusItem = default;
            foreach (var entry in getMoreDropItems)
            {
                if (entry.itemData == null || entry.dropProbability <= 0f) continue;
                bonusItem = entry;
                roll -= entry.dropProbability;
                if (roll <= 0f) break;
            }
            if (bonusItem.itemData == null) return null;

            UtilityFunctions.Log($"[OceanEventWaterBloom][녹조 추가 아이템 획득 : {bonusItem.itemData.name}]");
            return bonusItem.itemData;
        }

        return null;
    }
}
