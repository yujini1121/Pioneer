using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventThunder : OceanEventBase
{
    private GameObject rainEffect;
    private GameObject thunderEffectPrefab;
    private float thunderInterval;
    private float warningDuration;
    private float thunderRadius;
    private float stunDuration;


    // 2번 선택되는 것 방지용
    private bool isThunderLoopRunning = false;

    public OceanEventThunder(GameObject thunderEffectPrefab,
                             GameObject rainEffect,
                             float thunderInterval,
                             float warningDuration,
                             float thunderRadius,
                             float stunDuration)
    {
        EventName = "뇌우";

        this.thunderEffectPrefab = thunderEffectPrefab;
        this.rainEffect = rainEffect;
        this.thunderInterval = thunderInterval;
        this.warningDuration = warningDuration;
        this.thunderRadius = thunderRadius;
        this.stunDuration = stunDuration;
    }

    public override void EventRun()
    {
        if (isThunderLoopRunning)
            return;

        base.EventRun();

        if (rainEffect != null) 
            rainEffect.SetActive(true);

        isThunderLoopRunning = true;

        if (PlayerCore.Instance != null)
        {
            PlayerCore.Instance.ApplyThunderSpeedModifier(0.8f);
        }

        CrawlerAI[] crawlers = GameObject.FindObjectsOfType<CrawlerAI>();
        for (int i = 0; i < crawlers.Length; i++)
        {
            if (crawlers[i] == null) continue;
            crawlers[i].ApplyThunderSpeedModifier(0.8f);
        }

        TitanAI[] titans = GameObject.FindObjectsOfType<TitanAI>();
        for (int i = 0; i < titans.Length; i++)
        {
            if (titans[i] == null) continue;
            titans[i].ApplyThunderSpeedModifier(0.8f);
        }

        MinionAI[] minions = GameObject.FindObjectsOfType<MinionAI>();
        for (int i = 0; i < minions.Length; i++)
        {
            if (minions[i] == null) continue;
            minions[i].ApplyThunderSpeedModifier(0.8f);
        }

        MarinerBase[] mariners = GameObject.FindObjectsOfType<MarinerBase>();
        for (int i = 0; i < mariners.Length; i++)
        {
            if (mariners[i] == null) continue;
            mariners[i].ApplyThunderSpeedModifier(0.8f);
        }

        OceanEventManager.instance.BeginCoroutine(ThunderLoop());

        rainEffect.SetActive(true);
    }   

    public override void EventEnd()
    {
        base.EventEnd();

        if (PlayerCore.Instance != null)
            PlayerCore.Instance.ResetThunderSpeedModifier();

        if (rainEffect != null)
            rainEffect.SetActive(false);

        isThunderLoopRunning = false;

        CrawlerAI[] crawlers = GameObject.FindObjectsOfType<CrawlerAI>();
        for (int i = 0; i < crawlers.Length; i++)
        {
            if (crawlers[i] == null) continue;
            crawlers[i].ResetThunderSpeedModifier();
        }

        TitanAI[] titans = GameObject.FindObjectsOfType<TitanAI>();
        for (int i = 0; i < titans.Length; i++)
        {
            if (titans[i] == null) continue;
            titans[i].ResetThunderSpeedModifier();
        }

        MinionAI[] minions = GameObject.FindObjectsOfType<MinionAI>();
        for (int i = 0; i < minions.Length; i++)
        {
            if (minions[i] == null) continue;
            minions[i].ResetThunderSpeedModifier();
        }

        MarinerBase[] mariners = GameObject.FindObjectsOfType<MarinerBase>();
        for (int i = 0; i < mariners.Length; i++)
        {
            if (mariners[i] == null) continue;
            mariners[i].ResetThunderSpeedModifier();
        }

        ItemDeck[] decks = GameObject.FindObjectsOfType<ItemDeck>();
        for (int i = 0; i < decks.Length; i++)
        {
            if (decks[i] == null) continue;
            decks[i].EndThunderWarning();
        }

        rainEffect.SetActive(false);
    }

    private IEnumerator ThunderLoop()
    {
        while (IsRunning)
        {
            yield return new WaitForSeconds(thunderInterval);

            if (!IsRunning) yield break;

            ItemDeck targetDeck = GetRandomDeck();
            if (targetDeck == null) continue;

            // 바다이벤트 : 갑판 색상 변경으로 낙뢰 예고
            targetDeck.BeginThunderWarning(warningDuration);

            yield return new WaitForSeconds(warningDuration);

            if (!IsRunning)
            {
                if (targetDeck != null)
                    targetDeck.EndThunderWarning();

                yield break;
            }

            Vector3 strikePosition = targetDeck.transform.position;

            // 바다이벤트 : 실제 뇌우 이펙트 생성
            if (thunderEffectPrefab != null)
            {
                GameObject.Instantiate(thunderEffectPrefab, strikePosition, Quaternion.identity);
            }

            ApplyThunderDamage(strikePosition, targetDeck);

            if (targetDeck != null)
                targetDeck.EndThunderWarning();
        }
    }

    private ItemDeck GetRandomDeck()
    {
        ItemDeck[] decks = GameObject.FindObjectsOfType<ItemDeck>();

        List<ItemDeck> validDecks = new List<ItemDeck>();
        for (int i = 0; i < decks.Length; i++)
        {
            if (decks[i] == null) continue;
            if (decks[i].IsDead) continue;

            validDecks.Add(decks[i]);
        }

        if (validDecks.Count == 0)
            return null;

        int randomIndex = Random.Range(0, validDecks.Count);
        return validDecks[randomIndex];
    }

    private void ApplyThunderDamage(Vector3 center, ItemDeck targetDeck)
    {
        if (targetDeck != null)
        {
            targetDeck.DestroyByThunder();
        }

        Collider[] hits = Physics.OverlapSphere(center, thunderRadius);
        HashSet<CommonBase> processedTargets = new HashSet<CommonBase>();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            CommonBase commonBase = hits[i].GetComponentInParent<CommonBase>();
            if (commonBase == null) continue;
            if (processedTargets.Contains(commonBase)) continue;
            if (commonBase.IsDead) continue;

            processedTargets.Add(commonBase);

            // 갑판은 범위 피해 대상에서 제외
            ItemDeck deck = commonBase as ItemDeck;
            if (deck != null)
                continue;

            // 설치형 오브젝트 : 최대 체력의 10% 감소
            StructureBase structure = commonBase as StructureBase;
            if (structure != null)
            {
                int damage = Mathf.Max(1, Mathf.RoundToInt(structure.maxHp * 0.1f));
                structure.TakeDamage(damage, null);
                continue;
            }

            // 생명체 : 최대 체력의 30% 감소 + 2초 경직
            CreatureBase creature = commonBase as CreatureBase;
            if (creature != null)
            {
                int damage = Mathf.Max(1, Mathf.RoundToInt(creature.maxHp * 0.3f));
                creature.TakeDamage(damage, null);

                StunHandler stunHandler = creature.GetComponent<StunHandler>();
                if (stunHandler != null)
                {
                    stunHandler.ApplyStun(stunDuration);
                }
            }
        }
    }
}
