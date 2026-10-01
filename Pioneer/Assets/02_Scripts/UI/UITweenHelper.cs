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
        Vector3 baseScale = targetTransform.localScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        targetTransform.localScale = baseScale * 0.96f;

        Sequence sequence = DOTween.Sequence().SetTarget(target).SetUpdate(true)
            .SetLink(target, LinkBehaviour.KillOnDisable);
        sequence.Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        sequence.Join(targetTransform.DOScale(baseScale, duration).SetEase(Ease.OutCubic));
        sequence.OnKill(() => { if (targetTransform != null) targetTransform.localScale = baseScale; });
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
        Vector3 baseScale = targetTransform.localScale;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence().SetTarget(target).SetUpdate(true)
            .SetLink(target, LinkBehaviour.KillOnDisable);
        sequence.Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic));
        sequence.Join(targetTransform.DOScale(baseScale * 0.98f, duration).SetEase(Ease.InCubic));
        sequence.OnKill(() => { if (targetTransform != null) targetTransform.localScale = baseScale; });
        sequence.OnComplete(() =>
        {
            if (targetTransform != null) targetTransform.localScale = baseScale;
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
            .SetLink(canvasGroup.gameObject, LinkBehaviour.KillOnDisable)
            .SetUpdate(ignoreTimeScale);
    }

    public static void PunchScale(Transform target, float strength = 0.12f, float duration = 0.18f, bool ignoreTimeScale = false)
    {
        if (target == null) return;

        target.DOKill();
        Vector3 baseScale = target.localScale;
        target.localScale = baseScale;
        target.DOPunchScale(Vector3.one * strength, duration, 8, 0.7f)
            .SetUpdate(ignoreTimeScale)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable)
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
