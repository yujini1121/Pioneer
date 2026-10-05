using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private Text durationText;
    [SerializeField] private RectTransform tooltip;
    [SerializeField] private Text tooltipText;
    private RectTransform tooltipLayer;
    private readonly Vector3[] corners = new Vector3[4];
    private Sequence transition;
    private Vector3 restingScale;
    private int displayedSeconds = -1;
    public EffectType Type { get; private set; }
    public bool IsRemoving { get; private set; }
    private void Awake() { restingScale = transform.localScale; }

    public void Show(EffectType type, Sprite sprite, string label, string description, bool isBuff)
    {
        transition?.Kill();
        IsRemoving = false;
        Type = type;
        icon.sprite = sprite;
        frame.color = isBuff ? new Color(0.36f, 0.62f, 0.54f, 0.85f) : new Color(0.73f, 0.43f, 0.32f, 0.85f);
        tooltipText.text = label + "\n" + description;
        HideTooltip();
        durationText.gameObject.SetActive(false);
        displayedSeconds = -1;
        group.blocksRaycasts = true;
        group.alpha = 0f;
        transform.localScale = restingScale * 0.9f;
        transition = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(1f, 0.15f))
            .Join(transform.DOScale(restingScale, 0.18f).SetEase(Ease.OutCubic));
    }

    public void SetRemainingTime(float remaining)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        if (seconds == displayedSeconds) return;
        displayedSeconds = seconds;
        durationText.gameObject.SetActive(true);
        durationText.text = seconds.ToString();
    }

    public void Hide(Action complete)
    {
        if (IsRemoving) return;
        IsRemoving = true;
        transition?.Kill();
        HideTooltip();
        group.blocksRaycasts = false;
        transition = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(0f, 0.14f))
            .Join(transform.DOScale(restingScale * 0.93f, 0.14f).SetEase(Ease.InCubic))
            .OnComplete(() => complete?.Invoke());
    }

    public void ResetVisuals()
    {
        transition?.Kill(); transition = null;
        transform.localScale = restingScale;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; }
        HideTooltip();
        IsRemoving = false;
    }
    public void SetTooltipLayer(RectTransform layer) { HideTooltip(); tooltipLayer = layer; }

    private void HideTooltip()
    {
        if (tooltip == null) return;
        tooltip.gameObject.SetActive(false);
        if (isActiveAndEnabled && tooltip.parent != transform) tooltip.SetParent(transform, false);
    }

    private void PositionTooltip()
    {
        if (tooltipLayer == null || tooltip == null || !tooltip.gameObject.activeSelf) return;
        var canvas = tooltipLayer.GetComponentInParent<Canvas>().rootCanvas;
        var canvasRect = (RectTransform)canvas.transform;
        tooltipLayer.SetAsLastSibling();
        ((RectTransform)transform).GetWorldCorners(corners);
        Vector3 bottomLeft = canvasRect.InverseTransformPoint(corners[0]);
        Vector3 topRight = canvasRect.InverseTransformPoint(corners[2]);
        tooltip.GetWorldCorners(corners);
        Vector3 size = canvasRect.InverseTransformPoint(corners[2]) - canvasRect.InverseTransformPoint(corners[0]);
        const float gap = 8f;
        Rect bounds = canvasRect.rect;
        float x = topRight.x + gap;
        float y = topRight.y;
        if (x + size.x > bounds.xMax - gap) x = bottomLeft.x - gap - size.x;
        if (y > bounds.yMax - gap) y = bottomLeft.y - gap;
        x = Mathf.Clamp(x, bounds.xMin + gap, Mathf.Max(bounds.xMin + gap, bounds.xMax - gap - size.x));
        y = Mathf.Clamp(y, Mathf.Min(bounds.yMax - gap, bounds.yMin + gap + size.y), bounds.yMax - gap);
        tooltip.position = canvasRect.TransformPoint(new Vector3(x, y, 0f));
    }

    private void LateUpdate() { PositionTooltip(); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsRemoving || group.alpha <= 0.9f || tooltipLayer == null) return;
        tooltip.SetParent(tooltipLayer, false);
        tooltip.localScale = Vector3.one;
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.SetAsLastSibling();
        tooltip.gameObject.SetActive(true);
        PositionTooltip();
    }
    public void OnPointerExit(PointerEventData eventData) { HideTooltip(); }
    private void OnDisable() { ResetVisuals(); }
    private void OnDestroy() { transition?.Kill(); if (tooltip != null && tooltip.parent != transform) Destroy(tooltip.gameObject); }
}
