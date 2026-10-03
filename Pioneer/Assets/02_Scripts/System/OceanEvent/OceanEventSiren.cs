using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OceanEventSiren : OceanEventBase
{
    private readonly List<MarinerAI> charmedMariners = new List<MarinerAI>();

    private readonly GameObject sirenDebuffEffectPrefab;
    private readonly GameObject sirenAppearLeftEffectPrefab;
    private readonly GameObject sirenAppearRightEffectPrefab;
    private readonly Camera mainCamera;
    private readonly float checkInterval;
    private readonly float charmDuration;
    private readonly float procChance;

    private readonly Dictionary<MarinerAI, GameObject> debuffEffects = new Dictionary<MarinerAI, GameObject>();
    private readonly List<GameObject> appearEffects = new List<GameObject>();

    public OceanEventSiren(GameObject sirenDebuffEffectPrefab,
                           GameObject sirenAppearLeftEffectPrefab,
                           GameObject sirenAppearRightEffectPrefab,
                           Camera mainCamera,
                           float checkInterval,
                           float charmDuration,
                           float procChance)
    {
        EventName = "세이렌";

        this.sirenDebuffEffectPrefab = sirenDebuffEffectPrefab;
        this.sirenAppearLeftEffectPrefab = sirenAppearLeftEffectPrefab;
        this.sirenAppearRightEffectPrefab = sirenAppearRightEffectPrefab;
        this.mainCamera = mainCamera;

        this.checkInterval = checkInterval;
        this.charmDuration = charmDuration;
        this.procChance = procChance;
    }

    public override void EventRun()
    {
        if (IsRunning || OceanEventManager.instance == null) return;
        base.EventRun();
        OceanEventManager.instance.BeginCoroutine(CharmLoop());
    }

    public override void EventEnd()
    {
        base.EventEnd();

        for (int i = 0; i < charmedMariners.Count; i++)
        {
            if (charmedMariners[i] == null) continue;

            RemoveDebuffEffect(charmedMariners[i]);
            charmedMariners[i].isCharmed = false;
            charmedMariners[i].RestartNormalAI();
        }

        charmedMariners.Clear();

        foreach (var pair in debuffEffects)
        {
            if (pair.Value != null)
                GameObject.Destroy(pair.Value);
        }
        debuffEffects.Clear();
        foreach (GameObject effect in appearEffects)
            if (effect != null) GameObject.Destroy(effect);
        appearEffects.Clear();
    }

    private IEnumerator CharmLoop()
    {
        if (GameManager.Instance == null) yield break;
        float totalDuration = GameManager.Instance.dayDuration + GameManager.Instance.nightDuration;
        float elapsed = 0f; // 하루 전체 체크용
        int charmCount = 0;

        while (elapsed < totalDuration && IsRunning)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, checkInterval));
            elapsed += Mathf.Max(0.1f, checkInterval);

            if (!IsRunning || elapsed >= totalDuration || charmCount >= 2) yield break;
            charmedMariners.RemoveAll(mariner => mariner == null || mariner.IsDead || !mariner.isCharmed);
            if (charmedMariners.Count > 0) continue;

            if (Random.value <= procChance)
            {
                MarinerAI[] mariners = GameObject.FindObjectsOfType<MarinerAI>();
                if (mariners.Length == 0) continue;

                MarinerAI target = mariners[Random.Range(0, mariners.Length)];
                if (target == null || !target.isActiveAndEnabled || target.isCharmed || target.IsDead) continue;

                charmCount++;
                OceanEventManager.instance.BeginCoroutine(CharmRoutine(target));
            }
        }
    }

    private IEnumerator CharmRoutine(MarinerAI target)
    {
        if (target == null || !target.isActiveAndEnabled || target.IsDead || target.isCharmed) yield break;
        target.isCharmed = true;
        AudioManager.instance?.PlaySfx(AudioManager.SFX.LaughSaren);

        CreateDebuffEffect(target);
        CreateAppearEffect(target);

        if (!charmedMariners.Contains(target))
            charmedMariners.Add(target);

        target.StopAllCoroutines();
        if (target.Agent != null && target.Agent.isActiveAndEnabled && target.Agent.isOnNavMesh)
            target.Agent.isStopped = false;

        float attackInterval = 2f;
        int clickCount = 0;
        float timer = 0f;
        float nextAttackTime = attackInterval;

        while (timer < charmDuration && IsRunning)
        {
            yield return null;
            if (Time.timeScale <= 0f) continue;
            timer += Time.deltaTime;

            if (!IsRunning)
            {
                if (target != null && !target.IsDead)
                {
                    RemoveDebuffEffect(target);
                    target.isCharmed = false;
                    target.RestartNormalAI();
                    charmedMariners.Remove(target);
                }
                yield break;
            }

            if (target == null || !target.isActiveAndEnabled || target.IsDead)
            {
                RemoveDebuffEffect(target);
                if (target != null) target.isCharmed = false;
                charmedMariners.Remove(target);
                yield break;
            }

            //플레이어 근처 클릭 3회 -> 해제
            Collider[] cols = Physics.OverlapBox(target.transform.position, new Vector3(4f, 1f, 4f));
            foreach (var col in cols)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("Player") && Input.GetMouseButtonDown(0))
                {
                    clickCount++;
                    if (clickCount >= 3)
                    {
                        RemoveDebuffEffect(target);
                        target.isCharmed = false;
                        target.RestartNormalAI();
                        charmedMariners.Remove(target);
                        yield break;
                    }
                    break; // Multiple player colliders still represent one click.
                }
            }

            if (timer < nextAttackTime) continue;
            nextAttackTime += attackInterval;

            //주변 피해 (Player, Mariner)
            Collider[] hits = Physics.OverlapBox(target.transform.position, new Vector3(1.5f, 1f, 1.5f));
            HashSet<CommonBase> damagedTargets = new HashSet<CommonBase>();
            foreach (var hit in hits)
            {
                int layer = hit.gameObject.layer;
                if (layer == LayerMask.NameToLayer("Player") || layer == LayerMask.NameToLayer("Mariner"))
                {
                    CommonBase cb = hit.GetComponentInParent<CommonBase>();
                    if (cb != null && cb != target && !cb.IsDead && damagedTargets.Add(cb))
                    {
                        int dmg = Mathf.Max(1, Mathf.RoundToInt(cb.maxHp * 0.01f));
                        cb.TakeDamage(dmg, target.gameObject);
                    }
                }
            }

            //랜덤 이동 승무원 베이스 코드 가져옴
            NavMeshAgent agent = target.Agent;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 5f + target.transform.position;
                randomDirection.y = target.transform.position.y;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        if (target != null && !target.IsDead)
        {
            RemoveDebuffEffect(target);
            target.IsDead = true;
            target.WhenDestroy();
            target.isCharmed = false;
            charmedMariners.Remove(target);
        }
    }

    private void CreateDebuffEffect(MarinerAI target)
    {
        if (target == null) return;
        if (sirenDebuffEffectPrefab == null) return;
        if (debuffEffects.ContainsKey(target)) return;

        GameObject effect = GameObject.Instantiate(sirenDebuffEffectPrefab, target.transform);
        effect.transform.localPosition = Vector3.zero;
        debuffEffects.Add(target, effect);
    }

    private void RemoveDebuffEffect(MarinerAI target)
    {
        if (target == null) return;
        if (!debuffEffects.ContainsKey(target)) return;

        if (debuffEffects[target] != null)
            GameObject.Destroy(debuffEffects[target]);

        debuffEffects.Remove(target);
    }

    private void CreateAppearEffect(MarinerAI target)
    {
        if (target == null) return;
        if (mainCamera == null) return;

        Vector3 viewPos = mainCamera.WorldToViewportPoint(target.transform.position);
        bool isLeft = viewPos.x < 0.5f;

        GameObject prefab = isLeft ? sirenAppearLeftEffectPrefab : sirenAppearRightEffectPrefab;
        if (prefab == null) return;

        Vector3 spawnPos = mainCamera.transform.position;
        spawnPos += mainCamera.transform.right * (isLeft ? -5f : 5f);
        spawnPos += mainCamera.transform.up * 1.5f;
        spawnPos += mainCamera.transform.forward * 8f;

        appearEffects.RemoveAll(effect => effect == null);
        appearEffects.Add(GameObject.Instantiate(prefab, spawnPos, Quaternion.identity));
    }
}
