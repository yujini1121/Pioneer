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
    private int getMoreProbability = 80;

    List<PlayerFishing.FishingDropItem> getMoreDropItems;

    public OceanEventWaterBloom()
    {
        EventName = "녹조";
    }

    public override void EventRun()
    {
        base.EventRun();
        Debug.Log("[OceanEventWaterBloom][녹조 이벤트 시작]");
    }

    public override void EventEnd()
    {
        base.EventEnd();
        Debug.Log("[OceanEventWaterBloom][녹조 이벤트 종료]");
    }

    public SItemTypeSO GetMoreItem()
    {
        getMoreDropItems = PlayerFishing.instance.dropItemTable;

        if (Random.Range(0, 100) < getMoreProbability)
        {
            int randomIndex = Random.Range(0, getMoreDropItems.Count);

            PlayerFishing.FishingDropItem bonusItem = getMoreDropItems[randomIndex];

            Debug.Log($"[OceanEventWaterBloom][녹조 추가 아이템 획득 : {bonusItem.itemData.name}]");
            return bonusItem.itemData;
        }

        return null;
    }
}