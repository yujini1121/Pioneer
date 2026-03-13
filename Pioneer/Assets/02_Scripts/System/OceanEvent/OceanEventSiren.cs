using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OceanEventSiren : OceanEventBase
{
    private readonly List<MarinerAI> charmedMariners = new List<MarinerAI>();

    public OceanEventSiren()
    {
        EventName = "세이렌";
    }

    public override void EventRun()
    {
        base.EventRun();

        Debug.Log("[OceanEventSiren][세이렌 이벤트 시작]");
        OceanEventManager.instance.BeginCoroutine(CharmLoop());
    }

    public override void EventEnd()
    {
        base.EventEnd();

        for (int i = 0; i < charmedMariners.Count; i++)
        {
            if (charmedMariners[i] == null) continue;
            if (charmedMariners[i].IsDead) continue;

            charmedMariners[i].isCharmed = false;
            charmedMariners[i].RestartNormalAI();
        }

        charmedMariners.Clear();

        Debug.Log("[OceanEventSiren][세이렌 이벤트 종료]");
    }

    private IEnumerator CharmLoop()
    {
        float totalDuration = GameManager.Instance.dayDuration + GameManager.Instance.nightDuration;
        float elapsed = 0f; // 하루 전체 체크용

        while (elapsed < totalDuration && IsRunning)
        {
            yield return new WaitForSeconds(30f);
            elapsed += 30f;

            if (!IsRunning) yield break;

            if (Random.value <= 0.5f) // 30초가 지날때마다 50퍼 확률로
            {
                MarinerAI[] mariners = GameObject.FindObjectsOfType<MarinerAI>();
                if (mariners.Length == 0) continue;

                MarinerAI target = mariners[Random.Range(0, mariners.Length)];
                if (target == null || target.isCharmed || target.IsDead) continue;

                OceanEventManager.instance.BeginCoroutine(CharmRoutine(target));
            }
        }
    }

    private IEnumerator CharmRoutine(MarinerAI target)
    {
        target.isCharmed = true;

        if (!charmedMariners.Contains(target))
            charmedMariners.Add(target);

        target.StopAllCoroutines();
        target.Agent.isStopped = false;

        Debug.Log($"[OceanEventSiren][세이렌 매혹 시작 : {target.name}]");

        float charmDuration = 10f;
        float attackInterval = 1f;
        int clickCount = 0;
        float timer = 0f;

        while (timer < charmDuration && IsRunning)
        {
            yield return new WaitForSeconds(attackInterval);
            timer += attackInterval;

            if (!IsRunning) yield break;
            if (target == null || target.IsDead) yield break;

            //플레이어 근처 클릭 3회 -> 해제
            Collider[] cols = Physics.OverlapBox(target.transform.position, new Vector3(2f, 1f, 2f));
            foreach (var col in cols)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("Player") && Input.GetMouseButtonDown(0))
                {
                    clickCount++;
                    if (clickCount >= 3)
                    {
                        Debug.Log($"[OceanEventSiren][세이렌 매혹 해제 : {target.name}]");
                        target.isCharmed = false;
                        target.RestartNormalAI();
                        charmedMariners.Remove(target);
                        yield break;
                    }
                }
            }

            //주변 피해 (Player, Mariner)
            Collider[] hits = Physics.OverlapBox(target.transform.position, new Vector3(4f, 1f, 4f));
            foreach (var hit in hits)
            {
                if (hit.gameObject == target.gameObject) continue;

                int layer = hit.gameObject.layer;
                if (layer == LayerMask.NameToLayer("Player") || layer == LayerMask.NameToLayer("Mariner"))
                {
                    CommonBase cb = hit.GetComponent<CommonBase>();
                    if (cb != null && !cb.IsDead)
                    {
                        int dmg = Mathf.Max(1, Mathf.RoundToInt(cb.maxHp * 0.01f));
                        cb.TakeDamage(dmg, target.gameObject);
                    }
                }
            }

            //랜덤 이동 승무원 베이스 코드 가져옴
            NavMeshAgent agent = target.Agent;
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 5f + target.transform.position;
                randomDirection.y = target.transform.position.y;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        if (!IsRunning)
        {
            if (target != null && !target.IsDead)
            {
                target.isCharmed = false;
                target.RestartNormalAI();
                charmedMariners.Remove(target);
            }
            yield break;
        }

        if (target != null && !target.IsDead)
        {
            Debug.Log($"[OceanEventSiren][세이렌 매혹 실패 사망 : {target.name}]");
            target.IsDead = true;
            target.WhenDestroy();
            target.isCharmed = false;
            charmedMariners.Remove(target);
        }
    }
}