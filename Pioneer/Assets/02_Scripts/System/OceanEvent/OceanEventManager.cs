using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventManager : MonoBehaviour
{
	public static OceanEventManager instance;

	// 임의로 삽입
	private List<OceanEventBase> eventList;
	public OceanEventBase currentEvent;

	public Coroutine currentCoroutine;
	private bool isCoroutineStoped = true;

	private void Awake()
	{
		instance = this;

        eventList = new List<OceanEventBase>()
		{
			new OcenaEventNormal(),		// 평범
			new OceanEventFog(),		// 안개
			new OceanEventSiren(),		// 세이렌
			new OceanEventThunder(),    // 뇌우
			new OceanEventWaterBloom()	// 녹조
		};
		currentEvent = new OcenaEventNormal();
		currentEvent.EventRun();
	}

	// 첫날에 해당 함수를 실행 해 선 안됩니다.
	public void EnterDay()
	{
		currentEvent.EventEnd();
		int selectedIndex = Random.Range(0, eventList.Count);
		currentEvent = eventList[selectedIndex];
		eventList.RemoveAt(selectedIndex);

		currentEvent.EventRun();
	}

	public void EnterNight()
	{
		currentEvent.EnterNight();
	}

	public void BeginCoroutine(IEnumerator coroutine)
	{
		IEnumerator m_LocalCoroutine(IEnumerator mCoroutine)
		{
			isCoroutineStoped = false;
			yield return mCoroutine;
			isCoroutineStoped = true;
        }


		if (isCoroutineStoped) currentCoroutine = StartCoroutine(m_LocalCoroutine(coroutine));
	}
}
