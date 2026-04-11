using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OceanEventManager : MonoBehaviour
{
    public static OceanEventManager instance;

    private List<OceanEventBase> allEvents;
    private List<OceanEventBase> remainingEvents;
    public OceanEventBase currentEvent;
    public TextMeshProUGUI currentEventName;

    private readonly List<Coroutine> runningCoroutines = new List<Coroutine>();

    [Header("뇌우")]
    [SerializeField] private GameObject thunderEffect;
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private float thunderInterval = 30f;
    [SerializeField] private float thunderWarningDuration = 2f;
    [SerializeField] private float thunderRadius = 3f;
    [SerializeField] private float thunderStunDuration = 2f;
    
    [Header("세이렌")]
    [SerializeField] private GameObject sirenDebuffEffect;
    [SerializeField] private GameObject sirenAppearLeftEffect;
    [SerializeField] private GameObject sirenAppearRightEffect;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float sirenCheckInterval = 30f;
    [SerializeField] private float sirenCharmDuration = 10f;
    [SerializeField] private float sirenProcChance = 0.5f;

    [Header("안개")]
    [SerializeField] private FogFade fogFade;

    [Header("돌풍")]
    [SerializeField] private GameObject windEffect;
    [SerializeField] private float windInterval = 10f;
    [SerializeField] private float windMoveSpeed = 16f;
    [SerializeField] private float windLifetime = 5f;
    [SerializeField] private float windSpawnDistance = 10f;
    [SerializeField] private float windAirborneHeight = 4f;
    [SerializeField] private float windAirborneDuration = 1f;
    [SerializeField] private float windStunDuration = 2f;

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
            new OceanEventNormal(),
            new OceanEventFog(fogFade),
            new OceanEventSiren(sirenDebuffEffect,
                                sirenAppearLeftEffect,
                                sirenAppearRightEffect,
                                mainCamera,
                                sirenCheckInterval,
                                sirenCharmDuration,
                                sirenProcChance),
            new OceanEventThunder(thunderEffect,
                                  rainEffect,
                                  thunderInterval,
                                  thunderWarningDuration,
                                  thunderRadius,
                                  thunderStunDuration),
            new OceanEventWaterBloom(),
            new OceanEventWind(windEffect,
                               windInterval,
                               windMoveSpeed,
                               windLifetime,
                               windSpawnDistance,
                               windAirborneHeight,
                               windAirborneDuration,
                               windStunDuration)
        };

        ResetRemainingEvents();

        currentEvent = new OceanEventNormal();
        currentEvent.EventRun();

        RemoveNormalFromRemainingEvents();

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

        // 전체 선택
        //int selectedIndex = Random.Range(0, remainingEvents.Count);
        //currentEvent = remainingEvents[selectedIndex];
        //remainingEvents.RemoveAt(selectedIndex);

        #region 하나만 선택
        //// 평범 
        //currentEvent = new OceanEventNormal();

        //// 안개 
        //currentEvent = new OceanEventFog(fogFade);

        // 세이렌
        currentEvent = new OceanEventSiren(sirenDebuffEffect,
                                   sirenAppearLeftEffect,
                                   sirenAppearRightEffect,
                                   mainCamera,
                                   sirenCheckInterval,
                                   sirenCharmDuration,
                                   sirenProcChance);

        //// 뇌우
        //currentEvent = new OceanEventThunder(thunderEffect,
        //                             rainEffect,
        //                             thunderInterval,
        //                             thunderWarningDuration,
        //                             thunderRadius,
        //                             thunderStunDuration);

        //// 녹조
        //currentEvent = new OceanEventWaterBloom();

        //// 돌풍
        //currentEvent = new OceanEventWind(windEffect,
        //                          windInterval,
        //                          windMoveSpeed,
        //                          windLifetime,
        //                          windSpawnDistance,
        //                          windAirborneHeight,
        //                          windAirborneDuration,
        //                          windStunDuration);
        #endregion
        Debug.Log($"[OceanEventManager][오늘의 바다이벤트 : {currentEvent.EventName}]");
        currentEventName.text = currentEvent.EventName;

        currentEvent.EventRun();
    }
    
    // 첫날 평범한 날 예외때문에 이렇게 만들었는데 분명 더 좋은 방법이 있을거 같음
    private void RemoveNormalFromRemainingEvents()
    {
        for (int i = remainingEvents.Count - 1; i >= 0; i--)
        {
            if (remainingEvents[i] is OceanEventNormal)
            {
                remainingEvents.RemoveAt(i);
                break;
            }
        }
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