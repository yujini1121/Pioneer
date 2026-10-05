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

    public static void PlayOpen(GameObject target, float duration = 0.16f)
    {
        if (target == null) return;
        target.SetActive(true);
        var panel = target.GetComponent<UIPanelTween>() ?? target.AddComponent<UIPanelTween>();
        panel.Open(duration);
    }

    public static void PlayClose(GameObject target, Action onComplete, float duration = 0.1f)
    {
        if (target == null || !target.activeInHierarchy) { onComplete?.Invoke(); return; }
        var panel = target.GetComponent<UIPanelTween>() ?? target.AddComponent<UIPanelTween>();
        panel.Close(onComplete, duration);
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
