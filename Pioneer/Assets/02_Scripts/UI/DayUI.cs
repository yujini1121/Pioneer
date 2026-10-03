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
        morningBriefingTween?.Kill();
        morningBriefingTween = null;
        if (morningBriefingPanel != null) morningBriefingPanel.alpha = 0f;
        if (!HasMorningBriefing || !isActiveAndEnabled || oceanEvent == null) return false;
        dayText.text = day + "일차";
        eventText.text = oceanEvent.EventName;
        descriptionText.text = oceanEvent is OceanEventFog
            ? "정신력이 흔들리고 낮에도 적이 나타날 수 있습니다."
            : oceanEvent is OceanEventSiren
            ? "승무원이 매혹될 수 있습니다. 빠르게 구해주세요."
            : oceanEvent is OceanEventThunder
            ? "경고가 표시된 갑판에서 벗어나세요."
            : oceanEvent is OceanEventWaterBloom
            ? "낚시에서 추가 자원을 얻을 수 있습니다."
            : oceanEvent is OceanEventWind
            ? "갑판을 가로지르는 돌풍을 피하세요."
            : "잔잔한 하루입니다. 특별한 이상 현상은 없습니다.";
        morningBriefingPanel.gameObject.SetActive(true);
        morningBriefingPanel.alpha = 1f;
        morningBriefingPanel.blocksRaycasts = false;
        morningBriefingPanel.interactable = false;
        dayText.alpha = 0f;
        dayText.transform.localScale = Vector3.one * 0.92f;
        eventText.alpha = 0f;
        eventText.transform.localScale = Vector3.one * 0.95f;
        descriptionText.alpha = 0f;
        morningBriefingTween = DOTween.Sequence().SetUpdate(true)
            .Append(dayText.DOFade(1f, 0.25f).SetEase(Ease.OutCubic))
            .Join(dayText.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutCubic))
            .AppendInterval(1f)
            .Append(dayText.DOFade(0f, 0.2f).SetEase(Ease.Linear))
            .Append(eventText.DOFade(1f, 0.25f).SetEase(Ease.OutCubic))
            .Join(eventText.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutCubic))
            .Join(descriptionText.DOFade(1f, 0.25f).SetDelay(0.1f).SetEase(Ease.OutCubic))
            .AppendInterval(2f)
            .Append(eventText.DOFade(0f, 0.3f).SetEase(Ease.Linear))
            .Join(descriptionText.DOFade(0f, 0.3f).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                morningBriefingTween = null;
                if (morningBriefingPanel != null) morningBriefingPanel.alpha = 0f;
                if (dayText != null) dayText.transform.localScale = Vector3.one;
                if (eventText != null) eventText.transform.localScale = Vector3.one;
            });
        return true;
    }

    private void OnDisable()
    {
        morningBriefingTween?.Kill();
        morningBriefingTween = null;
        if (morningBriefingPanel != null)
        {
            morningBriefingPanel.alpha = 0f;
            morningBriefingPanel.blocksRaycasts = false;
            morningBriefingPanel.interactable = false;
        }
        if (dayText != null)
        {
            dayText.alpha = 0f;
            dayText.transform.localScale = Vector3.one;
        }
        if (eventText != null)
        {
            eventText.alpha = 0f;
            eventText.transform.localScale = Vector3.one;
        }
        if (descriptionText != null) descriptionText.alpha = 0f;
    }

    private void Update()
    {
        if (currentDay == null || GameManager.Instance == null)
            return;

        currentDay.text = "Day " + GameManager.Instance.currentDay.ToString();
    }
}
