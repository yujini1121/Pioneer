using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventWind : OceanEventBase
{
    private readonly GameObject windEffectPrefab;

    private readonly float windInterval;
    private readonly float windMoveSpeed;
    private readonly float windLifetime;
    private readonly float windSpawnDistance;
    private readonly float windAirborneHeight;
    private readonly float windAirborneDuration;
    private readonly float windStunDuration;

    public OceanEventWind(GameObject windEffectPrefab,
                          float windInterval,
                          float windMoveSpeed,
                          float windLifetime,
                          float windSpawnDistance,
                          float windKnockUpHeight,
                          float windKnockUpDuration,
                          float windStunDuration)
    {
        EventName = "µπ«≥";

        this.windEffectPrefab = windEffectPrefab;
        this.windInterval = windInterval;
        this.windMoveSpeed = windMoveSpeed;
        this.windLifetime = windLifetime;
        this.windSpawnDistance = windSpawnDistance;
        this.windAirborneHeight = windKnockUpHeight;
        this.windAirborneDuration = windKnockUpDuration;
        this.windStunDuration = windStunDuration;
    }

    public override void EventRun()
    {
        base.EventRun();

        OceanEventManager.instance.BeginCoroutine(WindLoop());

        Debug.Log("[OceanEventWind][µπ«≥ ¿Ã∫•∆Æ Ω√¿€]");
    }

    public override void EventEnd()
    {
        base.EventEnd();

        Debug.Log("[OceanEventWind][µπ«≥ ¿Ã∫•∆Æ ¡æ∑·]");
    }

    private IEnumerator WindLoop()
    {
        while (IsRunning)
        {
            yield return new WaitForSeconds(windInterval);

            if (!IsRunning) yield break;

            Transform target = GetRandomTarget();
            if (target == null) continue;

            SpawnWind(target);
        }
    }

    private Transform GetRandomTarget()
    {
        List<Transform> validTargets = new List<Transform>();

        if (PlayerCore.Instance != null && !PlayerCore.Instance.IsDead)
        {
            validTargets.Add(PlayerCore.Instance.transform);
        }

        MarinerAI[] mariners = GameObject.FindObjectsOfType<MarinerAI>();
        for (int i = 0; i < mariners.Length; i++)
        {
            if (mariners[i] == null) continue;
            if (mariners[i].IsDead) continue;

            validTargets.Add(mariners[i].transform);
        }

        if (validTargets.Count == 0)
            return null;

        int randomIndex = Random.Range(0, validTargets.Count);
        return validTargets[randomIndex];
    }

    private void SpawnWind(Transform target)
    {
        if (target == null) return;
        if (windEffectPrefab == null) return;

        Vector3 targetPosition = target.position;

        Vector3 spawnOffset = GetRandomSpawnOffset();
        Vector3 spawnPosition = targetPosition + spawnOffset;

        Vector3 moveDirection = targetPosition - spawnPosition;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.0001f)
            return;

        moveDirection.Normalize();

        Quaternion rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        GameObject windObject = GameObject.Instantiate(windEffectPrefab, spawnPosition, rotation);

        WindHit windHit = windObject.GetComponent<WindHit>();
        if (windHit != null)
        {
            windHit.Initialize(moveDirection,
                               windMoveSpeed,
                               windLifetime,
                               windAirborneHeight,
                               windAirborneDuration,
                               windStunDuration);
        }
    }

    private Vector3 GetRandomSpawnOffset()
    {
        int randomDirection = Random.Range(0, 4);

        switch (randomDirection)
        {
            case 0: return new Vector3(windSpawnDistance, 0f, 0f);
            case 1: return new Vector3(-windSpawnDistance, 0f, 0f);
            case 2: return new Vector3(0f, 0f, windSpawnDistance);
            default: return new Vector3(0f, 0f, -windSpawnDistance);
        }
    }
}