using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventFog : OceanEventBase
{
    private bool isDayFogApplied = false;
    private bool isNightFogApplied = false;

    public OceanEventFog()
    {
        EventName = "안개";
    }

    public override void EventRun()
    {
        base.EventRun();

        Debug.Log("[OceanEventFog][이벤트 시작]");

        ApplyMentalPenalty();
        ApplyDayFogVision();
        StartDayEffects();
    }

    public override void EnterNight()
    {
        Debug.Log("[OceanEventFog][밤 효과 적용]");

        RemoveDayFogVision();
        ApplyNightFogVision();
        ApplyNightGuiltBonus();
    }

    public override void EventEnd()
    {
        base.EventEnd();

        RemoveDayFogVision();
        RemoveNightFogVision();
        StopDayEffects();

        Debug.Log("[OceanEventFog][이벤트 종료]");
    }

    private void ApplyMentalPenalty()
    {
        Debug.Log("[OceanEventFog][정신력 감소량 10% 추가 적용]");
        if (PlayerCore.Instance != null)
        {
            PlayerCore.Instance.ReduceMentalByFog();
        }
    }

    #region 시야 효과 (빈껍데기) 
    private void ApplyDayFogVision()
    {
        if (isDayFogApplied) return;
        isDayFogApplied = true;

        Debug.Log("[OceanEventFog][낮 시야에 밤 시야 효과 적용]");
        // TODO : 시야 시스템 연결
    }

    private void RemoveDayFogVision()
    {
        if (!isDayFogApplied) return;
        isDayFogApplied = false;

        Debug.Log("[OceanEventFog][낮 시야 효과 해제]");
        // TODO : 시야 시스템 원복
    }

    private void ApplyNightFogVision()
    {
        if (isNightFogApplied) return;
        isNightFogApplied = true;

        Debug.Log("[OceanEventFog][밤 시야 범위 20% 감소]");
        // TODO : 밤 시야 20% 감소 적용
    }

    private void RemoveNightFogVision()
    {
        if (!isNightFogApplied) return;
        isNightFogApplied = false;

        Debug.Log("[OceanEventFog][밤 시야 효과 해제]");
        // TODO : 밤 시야 원복
    }
    #endregion  

    private void ApplyNightGuiltBonus()
    {
        Debug.Log("[OceanEventFog][현재 죄책감 가중치의 20% 추가 증가]");
        if (GuiltySystem.instance != null)
        {
            GuiltySystem.instance.AddFogNightWeight();
        }
    }

    private void StartDayEffects()
    {
        OceanEventManager.instance.BeginCoroutine(DayMinionSpawnLoop());
        OceanEventManager.instance.BeginCoroutine(DayGuiltIncreaseLoop());
    }

    private void StopDayEffects()
    {
        // 코루틴 정지는 OceanEventManager.EndCurrentEvent()에서 일괄 정지
    }

    private IEnumerator DayMinionSpawnLoop()
    {
        while (IsRunning)
        {
            yield return new WaitForSeconds(30f);

            if (!IsRunning) yield break;

            if (Random.value <= 0.5f)
            {
                if (GameManager.Instance != null)
                {
                    int spawnCount = Random.Range(1, 3);

                    // 바다이벤트 : 안개 낮 효과 -> 30초마다 50% 확률로 미니언 1~2마리 스폰
                    GameManager.Instance.SpawnFogMinions(spawnCount);
                }
            }
        }
    }

    private IEnumerator DayGuiltIncreaseLoop()
    {
        while (IsRunning)
        {
            yield return new WaitForSeconds(40f);

            if (!IsRunning) yield break;

            Debug.Log("[OceanEventFog][죄책감 가중치 1 증가]");
            if (GuiltySystem.instance != null)
            {
                GuiltySystem.instance.AddFogDayWeight();
            }
        }
    }
}