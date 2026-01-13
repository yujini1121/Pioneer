using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Michsky.UI.Dark
{
    public class ScrollRectElement : MonoBehaviour
    {
        [Header("Settings")]
        [Range(0, 1)] public float scrollValue = 0.2f;
        public int minItemCount = 4;
        public bool isVertical = true;

        private GameObject prevObj;
        private GameObject currentObj;
        private ScrollRect sRect;

        void Update()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == currentObj)
                return;

            InitializeSRE();
        }

        void InitializeSRE()
        {
            if (currentObj != null) { prevObj = currentObj; }
            currentObj = EventSystem.current.currentSelectedGameObject;
            if (prevObj == null) { prevObj = currentObj; }

            if (sRect == null)
            {
                try { sRect = transform.parent.GetComponent<ScrollRect>(); sRect.movementType = ScrollRect.MovementType.Clamped; }
                catch { return; }
            }

            if (isVertical == true)
            {
                if (sRect.verticalScrollbar == null)
                    return;
                if (currentObj.transform.GetSiblingIndex() < minItemCount && sRect.verticalScrollbar.value > 0.99f)
                    return;

                if (currentObj.transform.GetSiblingIndex() > prevObj.transform.GetSiblingIndex()) { sRect.verticalScrollbar.value -= scrollValue; }
                else { sRect.verticalScrollbar.value += scrollValue; }
            }

            else
            {
                if (sRect.horizontalScrollbar == null)
                    return;
                // if (currentObj.transform.GetSiblingIndex() < minItemCount && sRect.horizontalScrollbar.value > 0.01f)
                //    return;

                if (currentObj.transform.GetSiblingIndex() > prevObj.transform.GetSiblingIndex()) { sRect.horizontalScrollbar.value += scrollValue; }
                else { sRect.horizontalScrollbar.value -= scrollValue; }
            }
        }
    }
}