using System;
using DG.Tweening;
using UnityEngine;

public static class UITweenHelper
{
#region
    public static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null) return null;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    public static void PlayOpen(GameObject target, float duration = 0.18f)
    {
        if (target == null) return;

        target.SetActive(true);

        CanvasGroup canvasGroup = EnsureCanvasGroup(target);
        Transform targetTransform = target.transform;

        DOTween.Kill(target);
        canvasGroup.DOKill();
        targetTransform.DOKill();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        targetTransform.localScale = Vector3.one * 0.96f;

        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        targetTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public static void PlayClose(GameObject target, Action onComplete, float duration = 0.12f)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        CanvasGroup canvasGroup = EnsureCanvasGroup(target);
        Transform targetTransform = target.transform;

        DOTween.Kill(target);
        canvasGroup.DOKill();
        targetTransform.DOKill();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence().SetTarget(target).SetUpdate(true);
        sequence.Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic));
        sequence.Join(targetTransform.DOScale(Vector3.one * 0.98f, duration).SetEase(Ease.InCubic));
        sequence.OnComplete(() =>
        {
            targetTransform.localScale = Vector3.one;
            onComplete?.Invoke();
        });
    }

    public static void FadeCanvasGroup(CanvasGroup canvasGroup, bool visible, float duration = 0.16f, bool ignoreTimeScale = false)
    {
        if (canvasGroup == null) return;

        canvasGroup.DOKill();
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.DOFade(visible ? 1f : 0f, duration)
            .SetEase(visible ? Ease.OutCubic : Ease.InCubic)
            .SetUpdate(ignoreTimeScale);
    }

    public static void PunchScale(Transform target, float strength = 0.12f, float duration = 0.18f, bool ignoreTimeScale = false)
    {
        if (target == null) return;

        Vector3 baseScale = target.localScale;
        target.DOKill();
        target.localScale = baseScale;
        target.DOPunchScale(Vector3.one * strength, duration, 8, 0.7f)
            .SetUpdate(ignoreTimeScale)
            .OnKill(() =>
            {
                if (target != null)
                    target.localScale = baseScale;
            })
            .OnComplete(() =>
            {
                if (target != null)
                    target.localScale = baseScale;
            });
    }
#endregion
}
