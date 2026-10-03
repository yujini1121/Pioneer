using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DayUI : MonoBehaviour
{
    public TextMeshProUGUI currentDay;

    [SerializeField] private CanvasGroup morningBriefingPanel;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI eventText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    private Sequence morningBriefingTween;

    public bool HasMorningBriefing => morningBriefingPanel != null
        && dayText != null && eventText != null && descriptionText != null;

    private void Awake()
    {
        if (morningBriefingPanel == null)
        {
            foreach (CanvasGroup group in FindObjectsOfType<CanvasGroup>(true))
            {
                if (group.name != "MorningBriefingPanel") continue;
                morningBriefingPanel = group;
                break;
            }
        }
        if (morningBriefingPanel == null) return;
        foreach (TextMeshProUGUI text in morningBriefingPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (dayText == null && text.name == "DayText") dayText = text;
            if (eventText == null && text.name == "EventText") eventText = text;
            if (descriptionText == null && text.name == "DescriptionText") descriptionText = text;
        }
        morningBriefingPanel.alpha = 0f;
        morningBriefingPanel.blocksRaycasts = false;
        morningBriefingPanel.interactable = false;
    }

    public bool ShowMorningBriefing(int day, OceanEventBase oceanEvent)
    {
        if (!HasMorningBriefing || !isActiveAndEnabled || oceanEvent == null) return false;
        morningBriefingTween?.Kill();
        dayText.text = day + "일차";
        eventText.text = oceanEvent.EventName;
        descriptionText.text = oceanEvent is OceanEventFog
            ? "짙은 안개가 배를 감쌉니다. 정신력이 흔들리고 낮에도 적이 나타날 수 있습니다."
            : oceanEvent is OceanEventSiren
            ? "세이렌의 노래가 승무원을 홀립니다. 매혹된 승무원을 빠르게 구해내세요."
            : oceanEvent is OceanEventThunder
            ? "거센 비와 번개가 몰아칩니다. 경고가 표시된 갑판에서 벗어나세요."
            : oceanEvent is OceanEventWaterBloom
            ? "바다가 풍요로워졌습니다. 낚시를 하면 추가 자원을 얻을 수 있습니다."
            : oceanEvent is OceanEventWind
            ? "강한 돌풍이 갑판을 가로지릅니다. 휩쓸리지 않도록 움직임에 주의하세요."
            : "잔잔한 하루입니다. 특별한 바다 이상 현상은 없습니다.";
        morningBriefingPanel.gameObject.SetActive(true);
        morningBriefingPanel.alpha = 0f;
        morningBriefingPanel.blocksRaycasts = false;
        morningBriefingPanel.interactable = false;
        morningBriefingTween = DOTween.Sequence().SetUpdate(true)
            .Append(morningBriefingPanel.DOFade(1f, 0.25f))
            .AppendInterval(2.5f)
            .Append(morningBriefingPanel.DOFade(0f, 0.35f));
        return true;
    }

    private void OnDisable()
    {
        morningBriefingTween?.Kill();
        morningBriefingTween = null;
        if (morningBriefingPanel != null) morningBriefingPanel.alpha = 0f;
    }

    private void Update()
    {
        if (currentDay == null || GameManager.Instance == null)
            return;

        currentDay.text = "Day " + GameManager.Instance.currentDay.ToString();
    }
}
