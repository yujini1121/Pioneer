using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OceanEventManager : MonoBehaviour
{
    public static OceanEventManager instance;

    // 임의로 삽입
    private List<OceanEventBase> allEvents;
    private List<OceanEventBase> remainingEvents;
    public OceanEventBase currentEvent;
    public TextMeshProUGUI currentEventName;

    private readonly List<Coroutine> runningCoroutines = new List<Coroutine>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        allEvents = new List<OceanEventBase>()
        {
            new OceanEventNormal(),		// 평범
			new OceanEventFog(),		// 안개
			new OceanEventSiren(),		// 세이렌
			new OceanEventThunder(),    // 뇌우
			new OceanEventWaterBloom(),	// 녹조
            new OceanEventWind()       // 돌풍
		};

        ResetRemainingEvents();

        currentEvent = new OceanEventNormal();
        currentEvent.EventRun();

        Debug.Log($"[OceanEventManager][첫날 이벤트 : {currentEvent.EventName}]");
        currentEventName.text = currentEvent.EventName;
    }

    // 첫날에 해당 함수를 실행해선 안됩니다.
    public void EnterDay()
    {
        EndCurrentEvent();

        if (remainingEvents.Count == 0)
        {
            ResetRemainingEvents();
            Debug.Log("[OceanEventManager][이벤트 목록 초기화]");
        }

        //int selectedIndex = Random.Range(0, remainingEvents.Count);
        //currentEvent = remainingEvents[selectedIndex];
        //remainingEvents.RemoveAt(selectedIndex);
        currentEvent = new OceanEventWaterBloom();
        currentEvent.EventRun();

        Debug.Log($"[OceanEventManager][오늘의 바다이벤트 : {currentEvent.EventName}]");
        currentEventName.text = currentEvent.EventName;

        currentEvent.EventRun();
    }

    public void EnterNight()
    {
        if (currentEvent == null) return;

        Debug.Log($"[OceanEventManager][밤 진입 : {currentEvent.EventName}]");
        currentEvent.EnterNight();
    }

    private void ResetRemainingEvents()
    {
        remainingEvents = new List<OceanEventBase>(allEvents);
    }

    public void EndCurrentEvent()
    {
        StopAllEventCoroutines();

        if (currentEvent == null) return;

        Debug.Log($"[OceanEventManager][이벤트 종료 : {currentEvent.EventName}]");
        currentEvent.EventEnd();
    }

    public Coroutine BeginCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null) return null;

        Coroutine routine = StartCoroutine(coroutine);
        runningCoroutines.Add(routine);
        return routine;
    }

    public void StopAllEventCoroutines()
    {
        for (int i = 0; i < runningCoroutines.Count; i++)
        {
            if (runningCoroutines[i] != null)
                StopCoroutine(runningCoroutines[i]);
        }

        runningCoroutines.Clear();
    }
}