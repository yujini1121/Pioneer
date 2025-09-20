using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFishing : MonoBehaviour
{
    [Header("Item List")]
    public List<SItemStack> fishingItemList = new List<SItemStack>();

    private Coroutine fishingLoopCoroutine;

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
            yield return new WaitForSeconds(4f);
            // PlayerStatsLevel.Instance.AddExp(); => 아이템 결정되면 해당 아이템에따라 경험치 부여?
            Debug.Log("낚시 끝");
        }
    }

    private void GetItem()
    {
        // 으음...
    }
}
