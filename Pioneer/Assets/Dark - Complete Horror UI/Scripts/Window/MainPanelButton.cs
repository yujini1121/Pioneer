using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Michsky.UI.Dark
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class MainPanelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Animator buttonAnimator;

        void Awake()
        {
            if (buttonAnimator == null) { buttonAnimator = GetComponent<Animator>(); }
        }

        void OnEnable()
        {
            StartCoroutine(FixLayout());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hover to Pressed"))
                buttonAnimator.Play("Hover");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hover to Pressed"))
                buttonAnimator.Play("Hover to Normal");
        }

        IEnumerator FixLayout()
        {
            yield return new WaitForSecondsRealtime(0.025f);

            // Waiting UGUI to be initialized and then forcing it to rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}