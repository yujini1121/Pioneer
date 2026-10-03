using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    private bool enteredNight;

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
    [SerializeField] private float windInterval = 7f;
    [SerializeField] private float windMoveSpeed = 16f;
    [SerializeField] private float windLifetime = 5f;
    [SerializeField] private float windSpawnDistance = 10f;
    [SerializeField] private float windAirborneHeight = 0.8f;
    [SerializeField] private float windAirborneDuration = 0.45f;
    [SerializeField] private float windStunDuration = 0.45f;

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

        UtilityFunctions.Log($"[OceanEventManager][첫날 이벤트 : {currentEvent.EventName}]");
        SetCurrentEventName(currentEvent.EventName, false);
    }

    // 첫날에 해당 함수를 실행해선 안됩니다.
    public void EnterDay()
    {
        EndCurrentEvent();
        enteredNight = false;

        if (remainingEvents.Count == 0)
        {
            ResetRemainingEvents();
            UtilityFunctions.Log("[OceanEventManager][이벤트 목록 초기화]");
        }

        // 전체 선택
        int selectedIndex = Random.Range(0, remainingEvents.Count);
        currentEvent = remainingEvents[selectedIndex];
        remainingEvents.RemoveAt(selectedIndex);

        #region 하나만 선택
        //// 평범 
        //currentEvent = new OceanEventNormal();

        //// 안개 
        //currentEvent = new OceanEventFog(fogFade);

        //// 세이렌
        //currentEvent = new OceanEventSiren(sirenDebuffEffect,
        //                           sirenAppearLeftEffect,
        //                           sirenAppearRightEffect,
        //                           mainCamera,
        //                           sirenCheckInterval,
        //                           sirenCharmDuration,
        //                           sirenProcChance);

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
        UtilityFunctions.Log($"[OceanEventManager][오늘의 바다이벤트 : {currentEvent.EventName}]");
        SetCurrentEventName(currentEvent.EventName, true);

        currentEvent.EventRun();
        string hint = currentEvent is OceanEventSiren ? "세이렌 — 매혹된 승무원을 세 번 클릭해 깨우세요."
            : currentEvent is OceanEventThunder ? "뇌우 — 경고가 표시된 갑판에서 벗어나세요."
            : currentEvent is OceanEventWind ? "돌풍 — 다가오는 바람을 피하세요."
            : currentEvent is OceanEventWaterBloom ? "녹조 — 낚시에서 추가 자원을 얻을 수 있습니다."
            : currentEvent is OceanEventFog ? "안개 — 주변의 위협을 살피세요." : null;
        if (hint != null) InGameUI.instance?.ShowActionFeedback(hint, 1);
    }

#region
    private void SetCurrentEventName(string eventName, bool animate)
    {
        if (currentEventName == null)
            return;

        currentEventName.DOKill();
        currentEventName.transform.DOKill();
        currentEventName.text = eventName;

        if (!animate)
        {
            currentEventName.alpha = 1.0f;
            currentEventName.transform.localScale = Vector3.one;
            return;
        }

        currentEventName.alpha = 0.0f;
        currentEventName.transform.localScale = Vector3.one * 0.9f;
        currentEventName.DOFade(1.0f, 0.25f).SetEase(Ease.OutCubic);
        currentEventName.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
    }
#endregion
    
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
        if (currentEvent == null || !currentEvent.IsRunning || enteredNight) return;
        enteredNight = true;

        UtilityFunctions.Log($"[OceanEventManager][밤 진입 : {currentEvent.EventName}]");
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

        UtilityFunctions.Log($"[OceanEventManager][이벤트 종료 : {currentEvent.EventName}]");
        currentEvent.EventEnd();
        currentEvent = null;
    }

    public Coroutine BeginCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null || !isActiveAndEnabled) return null;

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

    private void OnDisable()
    {
        EndCurrentEvent();
        if (currentEventName != null)
        {
            currentEventName.DOKill();
            currentEventName.transform.DOKill();
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
