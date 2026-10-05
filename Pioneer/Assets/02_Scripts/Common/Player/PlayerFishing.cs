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

    private static readonly Vector3 FishingEffectOffset = new Vector3(0f, 0f, 0.95f);
    private ParticleSystem fishingEffect;
    private Coroutine fishingLoopCoroutine;
    private Vector3 fishingDirection;

    private int fishingExp = 5;

    private void Awake()
    {
        instance = this;
    }

    public void BeginFishing(Vector3 dir)
    {
        if (PlayerCore.Instance == null) return;
        dir.y = 0f;
        fishingDirection = dir.normalized;
        int idx = (Mathf.Abs(dir.x) < 1e-6f) ? 1 : (dir.x >= 0f ? 1 : 0);

        // 안전장치: 리스트가 2개 미만이면 0으로 강제
        var controller = PlayerCore.Instance.GetComponent<PlayerController>();
        if (controller == null || controller.animSlots == null) return;
        var slots = controller.animSlots;
        int maxReady = (slots.fising != null) ? Mathf.Max(0, slots.fising.Count - 1) : 0;
        int maxHold = (slots.fisingHold != null) ? Mathf.Max(0, slots.fisingHold.Count - 1) : 0;
        idx = Mathf.Clamp(idx, 0, Mathf.Min(maxReady, maxHold));

        PlayerCore.Instance.FishingReady(new Vector3(idx == 1 ? 1f : -1f, 0, 0));
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
        StopFishingEffect();
        if (fishingLoopCoroutine != null)
        {
            StopCoroutine(fishingLoopCoroutine);
            fishingLoopCoroutine = null;
        }
        if (fishingEventUI != null) fishingEventUI.CloseUI();
        if (PlayerCore.Instance != null && !PlayerCore.Instance.IsDead)
            PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);
    }

    private void StopFishingEffect()
    {
        CreatureEffect.StopLoopingEffect(fishingEffect);
        fishingEffect = null;
    }

    public Vector3 GetFishingEffectPosition(Vector3 direction)
    {
        return GetFishingEffectPosition(transform.position, direction);
    }

    public Vector3 GetFishingEffectPosition(Vector3 origin, Vector3 direction)
    {
        direction.y = 0f;
        var controller = GetComponent<PlayerController>();
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = controller != null ? controller.lastMoveDirection : Vector3.right;
            direction.y = 0f;
        }
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.right;
        direction.Normalize();

        Vector3 sideways = Vector3.Cross(Vector3.up, direction);
        Vector3 position = origin + direction * FishingEffectOffset.z
            + sideways * FishingEffectOffset.x;
        int groundMask = controller != null ? controller.groundLayer.value : LayerMask.GetMask("Platform");
        int waterMask = controller != null ? controller.seaLayer.value : LayerMask.GetMask("Water");

        for (int step = 0; step < 12; step++)
        {
            if (!Physics.Raycast(position + Vector3.up * 3f, Vector3.down, 8f,
                groundMask, QueryTriggerInteraction.Ignore)) break;
            position += direction * 0.25f;
        }

        position.y = origin.y - 0.8f + FishingEffectOffset.y;
        if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit water,
            8f, waterMask, QueryTriggerInteraction.Collide))
            position.y = water.point.y + FishingEffectOffset.y;
        return position;
    }

    // 낚시로 아이템 추가하는 코드
    private IEnumerator FishingLoop()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeFishing);

        try
        {
            // 낚시 시작
            UtilityFunctions.Log("낚시 시작");

            if (CreatureEffect.Instance != null && fishingEffect == null)
            {
                ParticleSystem ps = CreatureEffect.Instance.GetEffect(8);
                fishingEffect = CreatureEffect.Instance.PlayLoopingEffect(ps,
                    GetFishingEffectPosition(fishingDirection), transform);
            }
            yield return new WaitForSeconds(Random.Range(3f, 5f));



            bool isSuccess = true;
            bool eventResult = false;
            if (fishingEventUI != null && (nonEventCount >= 5 || Random.value < eventChance))
            {
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

            StopFishingEffect();
            GetItemProcess(isSuccess && eventResult);

            UtilityFunctions.Log("낚시 끝");
            fishingLoopCoroutine = null;
            PlayerController completedController = GetComponent<PlayerController>();
            if (completedController != null) completedController.CancelFishing();
            else StopFishingLoop();
        }
        finally
        {
            StopFishingEffect();
            fishingLoopCoroutine = null;
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
        int count = caughtItem == treasureItem ? 1 : 2;
        int dropIndex = 0;
        if (isDoubleBonus) TreasureBoxManager.instance?.GetSpecialBox();
        SItemStack itemStack = new SItemStack(caughtItem.id, count);

        if (caughtItem != null)
        {
            if (caughtItem == treasureItem)
            {
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
            }

            if (Random.Range(0f, 1f) < chances.treasureChestChance)
            {
                if (treasureItem != null)
                {

                    TreasureBoxManager.instance?.GetBox();
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

        }
    }

    private void OnDisable()
    {
        StopFishingLoop();
    }

    private void OnDestroy()
    {
        StopFishingEffect();
        if (instance == this) instance = null;
    }
}
