using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemGetNoticeSingleUI : MonoBehaviour
{
    public Coroutine myCoroutine;
    public Image icon;
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    public int index;
    private Sequence sequence;
    private Tween moveTween;

    public void Show(SItemStack target)
    {
        icon.sprite = target.itemBaseType.image;
        text.text = $"{target.itemBaseType.typeName} {target.amount}개 획득";
    }

    public void Begin()
    {
        if (canvasGroup == null) canvasGroup = UITweenHelper.EnsureCanvasGroup(gameObject);
#region
        if (myCoroutine != null)
        {
            StopCoroutine(myCoroutine);
            myCoroutine = null;
        }

        sequence?.Kill();
        transform.DOKill();
        canvasGroup.DOKill();

        transform.localScale = Vector3.one;
        canvasGroup.alpha = 0.0f;

        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        sequence.Join(canvasGroup.DOFade(1.0f, 0.18f).SetEase(Ease.OutCubic));
        sequence.Join(transform.DOPunchScale(Vector3.one * 0.08f, 0.22f, 8, 0.7f));
        sequence.AppendInterval(4.5f);
        sequence.Append(canvasGroup.DOFade(0.0f, 0.35f).SetEase(Ease.InCubic));
        sequence.OnComplete(() =>
        {
            if (ItemGetNoticeUI.Instance != null) ItemGetNoticeUI.Instance.RemoveUI(index, this);
        });
#endregion
    }

    public void MoveToLocalY(float targetY)
    {
        moveTween?.Kill();
        moveTween = transform.DOLocalMoveY(targetY, 0.18f).SetEase(Ease.OutCubic)
            .SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }


    private void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }
    }

    private void OnDisable()
    {
        sequence?.Kill();
        transform.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }

    // Start is called before the first frame update


    // Update is called once per frame




}
