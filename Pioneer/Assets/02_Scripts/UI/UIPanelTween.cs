using System;
using DG.Tweening;
using UnityEngine;

public class UIPanelTween : MonoBehaviour
{
    private CanvasGroup group;
    private Vector3 restingScale;
    private Sequence transition;
    private bool visible = true;

    private void Awake()
    {
        restingScale = transform.localScale;
        group = UITweenHelper.EnsureCanvasGroup(gameObject);
    }

    public void Open(float duration)
    {
        transition?.Kill();
        visible = true;
        gameObject.SetActive(true);
        group.alpha = 0f;
        group.interactable = group.blocksRaycasts = true;
        transform.localScale = restingScale * 0.96f;
        transition = DOTween.Sequence().SetUpdate(true).SetTarget(gameObject)
            .Join(group.DOFade(1f, duration).SetEase(Ease.OutCubic))
            .Join(transform.DOScale(restingScale, duration).SetEase(Ease.OutCubic));
    }

    public void Close(Action complete, float duration)
    {
        transition?.Kill();
        visible = false;
        group.interactable = group.blocksRaycasts = false;
        transition = DOTween.Sequence().SetUpdate(true).SetTarget(gameObject)
            .Join(group.DOFade(0f, duration).SetEase(Ease.InCubic))
            .Join(transform.DOScale(restingScale * 0.98f, duration).SetEase(Ease.InCubic))
            .OnComplete(() => { transform.localScale = restingScale; complete?.Invoke(); });
    }

    private void OnDisable()
    {
        transition?.Kill(); transition = null;
        transform.localScale = restingScale;
        if (group != null) group.alpha = visible ? 1f : 0f;
    }
}
