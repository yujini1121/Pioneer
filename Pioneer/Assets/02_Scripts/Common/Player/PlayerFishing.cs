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

    [Header("낚시 돌발 이벤트 설정")]
    public FishingEventUI fishingEventUI;
    [SerializeField] private float eventChance = 0.3f;
    private int nonEventCount = 0;


    private Coroutine fishingLoopCoroutine;
    private Vector3 fishingDirection;

    private int fishingExp = 5;

    private CreatureEffect creatureEffect;
    private void Awake()
    {
        instance = this;
        creatureEffect = GetComponent<CreatureEffect>();
    }

    // PlayerFishing.cs
    public void BeginFishing(Vector3 dir)
    {
        if (PlayerCore.Instance == null) return;
        dir.y = 0f;
        fishingDirection = dir.normalized;
        // 좌/우만 사용: x>=0 → 1(오른쪽), x<0 → 0(왼쪽). 정지면 마지막 값 유지되므로 1로 처리
        int idx = (Mathf.Abs(dir.x) < 1e-6f) ? 1 : (dir.x >= 0f ? 1 : 0);

        // 안전장치: 리스트가 2개 미만이면 0으로 강제
        var controller = PlayerCore.Instance.GetComponent<PlayerController>();
        if (controller == null || controller.animSlots == null) return;
        var slots = controller.animSlots;
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
        if(fishingLoopCoroutine == null && isActiveAndEnabled && PlayerCore.Instance != null
            && !PlayerCore.Instance.IsDead)
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
        if (fishingEventUI != null) fishingEventUI.CloseUI();
        if (PlayerCore.Instance != null && !PlayerCore.Instance.IsDead)
            PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);
    }

    // 낚시로 아이템 추가하는 코드
    private IEnumerator FishingLoop()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        // One cast now resolves exactly one attempt.
        {
            // 낚시 시작
            UtilityFunctions.Log("낚시 시작");

            if (CreatureEffect.Instance != null)
            {
                ParticleSystem ps = CreatureEffect.Instance.GetEffect(8);
                CreatureEffect.Instance.PlayEffect(ps, PlayerCore.Instance.transform.position + new Vector3(0f, -0.8f, 0.3f));
            }
            yield return new WaitForSeconds(Random.Range(3f, 5f));


            /* 낚시 이벤트
             * 아이템 획득 전 30% 확률로 발생하는 슬라이드 바 타이밍 맞추기 이벤트
             * 4초마다 30% 확률로 이벤트 발생
             * 이벤트 연속 발생 횟수가 5번 이상일 경우 다음 낚시 돌발 이벤트 반드시 발생
             * 플레이어 머리 위에 낚시 이벤트 UI 발생
             */

            bool isSuccess = true;
            bool eventResult = false;
            if (fishingEventUI != null && (nonEventCount >= 5 || Random.value < eventChance))
            {
                // isSuccess = true;
                UtilityFunctions.Log("<color=orange>돌발 이벤트 발생!</color>");
                nonEventCount = 0;


                InGameUI.instance?.ShowActionFeedback("강한 입질! 잠시 후 표시 구간에 맞춰 Space!", 1);
                AudioManager.instance?.PlaySfx(AudioManager.SFX.FishingAlert);
                yield return new WaitForSeconds(0.8f);
                yield return fishingEventUI.StartQTE(res => eventResult = res);

                if(!eventResult)
                {
                    UtilityFunctions.Log("낚시 돌발 이벤트에 실패했습니다.");
                    // 낚시 이벤트 실패 사운드 재생
                    AudioManager.instance?.PlaySfx(AudioManager.SFX.RemoveItem);
                    InGameUI.instance?.ShowActionFeedback("놓쳤습니다. Q를 길게 눌러 다시 낚시하세요.", 1);
                    fishingLoopCoroutine = null;
                    PlayerController controller = GetComponent<PlayerController>();
                    if (controller != null) controller.CancelFishing();
                    else StopFishingLoop();
                    yield break;
                }
            }
            else
            {
                nonEventCount++;

                UtilityFunctions.Log($"돌발 이벤트 미발생 (누적: {nonEventCount})");
            }

            GetItemProcess(isSuccess && eventResult);

            UtilityFunctions.Log("낚시 끝");
            fishingLoopCoroutine = null;
            PlayerController completedController = GetComponent<PlayerController>();
            if (completedController != null) completedController.CancelFishing();
            else StopFishingLoop();
        }
    }

    private SItemTypeSO GetItem()
    {
        if (dropItemTable == null || dropItemTable.Count == 0) return null;
        UtilityFunctions.Log("아이템 얻기 시작");
        float totalProbability = 0f;
        // 1. 전체 가중치 합 계산
        for (int i = 0; i < dropItemTable.Count; i++)
        {
            if (dropItemTable[i].itemData != null) totalProbability += Mathf.Max(0f, dropItemTable[i].dropProbability);
        }

        if (totalProbability <= 0)
        {
            return null;
        }
        // 2. 0 ~ 전체 가중치사이 랜덤 숫자 뽑기
        float randomNum = Random.Range(0f, totalProbability);
        // 3. 랜덤 숫자가 현재 아이템의 가중치 보다 작으면 당첨
        foreach (var item in dropItemTable)
        {
            if (item.itemData == null || item.dropProbability <= 0f) continue;
            if(randomNum <= item.dropProbability)
            {
                return item.itemData;
            }
            // 4. 당첨되지않았으면 현재 아이템 가중치를 빼고 다음 아이템으로 넘어감
            randomNum -= item.dropProbability;
        }
        
        return dropItemTable[dropItemTable.Count - 1].itemData;
    }

    private void GetItemProcess(bool isDoubleBonus)
    {
        SItemTypeSO caughtItem = GetItem();
        if (caughtItem == null) return;

        AudioManager.instance?.PlaySfx(caughtItem == treasureItem ? AudioManager.SFX.OpenBox : AudioManager.SFX.GetFishing);
        if (caughtItem == treasureItem) InGameUI.instance?.ShowActionFeedback("보물상자를 낚았습니다!");
        else InGameUI.instance?.ShowActionFeedback(isDoubleBonus
            ? "인양 성공! 자원과 특별 보상을 획득했습니다."
            : "인양 완료! Q를 길게 눌러 다시 던지세요.", 1);
        // Longer, manual casts return a small bundle; the normal material pool stays intact.
        int count = caughtItem == treasureItem ? 1 : 2;
        int dropIndex = 0;
        if (isDoubleBonus) TreasureBoxManager.instance?.GetSpecialBox();
        SItemStack itemStack = new SItemStack(caughtItem.id, count);

        if (caughtItem != null)
        {
            if (caughtItem == treasureItem)
            {
                // TreasureBoxManager.instance.GetBox();
                for (int i = 0; i < count; i++) TreasureBoxManager.instance?.GetBox();
                fishingExp = 10;
            }
            else
            {
                if (ItemDropManager.instance != null)
                    dropIndex += ItemDropManager.instance.CatchFishing(itemStack, transform, fishingDirection, dropIndex);
                fishingExp = isDoubleBonus ? 10 : 5;
            }

            if (PlayerStatsLevel.Instance == null) return;
            PlayerStatsLevel.Instance.AddExp(GrowStatType.Fishing, fishingExp);
            UtilityFunctions.Log($">> PlayerFishing.FishingLoop() 아이템 획득: 숫자 {caughtItem.id}, 이름 {caughtItem.typeName}, 경험치 +{fishingExp}");

            (float extraItemChance, float treasureChestChance) chances = PlayerStatsLevel.Instance.FishingChance();

            if (Random.Range(0f, 1f) < chances.extraItemChance)
            {
                if (caughtItem == treasureItem)
                {
                    TreasureBoxManager.instance?.GetBox();
                }
                else
                {
                    if (ItemDropManager.instance != null)
                        dropIndex += ItemDropManager.instance.CatchFishing(new SItemStack(caughtItem.id, 1),
                            transform, fishingDirection, dropIndex);
                }
                UtilityFunctions.Log($"<color=cyan>[낚시 레벨 보너스!]</color> {caughtItem.typeName}을(를) 추가로 획득했습니다! (확률: {chances.extraItemChance * 100:F2}%)");
            }

            if (Random.Range(0f, 1f) < chances.treasureChestChance)
            {
                if (treasureItem != null)
                {
                    //SItemStack treasureItemStack = new SItemStack(treasureItem.id, 1);
                    //InventoryManager.Instance.Add(treasureItemStack);

                    TreasureBoxManager.instance?.GetBox();
                    UtilityFunctions.Log($"<color=yellow>[낚시 레벨 보너스!]</color> 보물상자를 추가로 획득했습니다! (확률: {chances.treasureChestChance * 100:F2}%)");
                }
            }

            // 바디이벤트 녹조로 얻는 추가 아이템 획득
            if (OceanEventManager.instance != null && OceanEventManager.instance.currentEvent is OceanEventWaterBloom)
            {
                OceanEventWaterBloom waterBloomEnvent = OceanEventManager.instance.currentEvent as OceanEventWaterBloom;

                SItemTypeSO bonusItem = waterBloomEnvent.GetMoreItem();

                if (bonusItem != null)
                {
                    SItemStack waterBloombonusItemStack = new SItemStack(bonusItem.id, 1);
                    if (bonusItem == treasureItem) TreasureBoxManager.instance?.GetBox();
                    else if (ItemDropManager.instance != null)
                        ItemDropManager.instance.CatchFishing(waterBloombonusItemStack, transform, fishingDirection, dropIndex);
                    InGameUI.instance?.ShowActionFeedback("녹조 보너스! 추가 자원을 획득했습니다.");
                }
            }

            //creatureEffect.Effects[3].Play();
        }
    }

    private void OnDisable()
    {
        StopFishingLoop();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
