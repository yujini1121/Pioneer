using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.UI.Dark
{
    public class GamepadScrollEvent : MonoBehaviour
    {
        [Header("Resources")]
        public GamepadChecker gamepadManager;

        [Header("Settings")]
        public bool requireSelecting;
        public bool optimizeUpdates;
        [Range(0.0001f, 0.25f)] public float changeValue = 0.01f;

        [Header("Slider")]
        public bool useSlider;
        public bool reversePosition;
        public Slider sliderObject;

        [Header("Scrollbar")]
        public bool useScrolbar;
        public bool isHorizontalScrollbar;
        public Scrollbar scrollbarObject;

        [Header("Horizontal Selector")]
        public bool useHorizontalSelector;
        public float selectorCooldown = 0.3f;
        public HorizontalSelector selectorObject;
        bool hsIsInCooldown = false;

        EventTrigger triggerEvent;

        void Start()
        {
            if (gamepadManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                var tempManager = FindObjectsByType<GamepadChecker>(FindObjectsSortMode.None)[0];
#else
                var tempManager = (GamepadChecker)FindObjectsOfType(typeof(GamepadChecker))[0];
#endif

                if (tempManager != null)
                    gamepadManager = tempManager;
            }

            if (optimizeUpdates == true)
            {
                triggerEvent = gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry selectEntry = new EventTrigger.Entry();
                selectEntry.eventID = EventTriggerType.Select;
                selectEntry.callback.AddListener((eventData) => { this.enabled = true; });
                triggerEvent.triggers.Add(selectEntry);

                EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
                deselectEntry.eventID = EventTriggerType.Deselect;
                deselectEntry.callback.AddListener((eventData) => { this.enabled = false; });
                triggerEvent.triggers.Add(deselectEntry);

                this.enabled = false;
            }
        }

        void Update()
        {
            if (Gamepad.current == null || gamepadManager == null)
                return;

            if (useSlider == true)
            {                        
                if (requireSelecting == true && useSlider == true && EventSystem.current != sliderObject)
                    return;

                if (reversePosition == false && gamepadManager.hAxis >= 0.1f)
                    sliderObject.value -= changeValue * gamepadManager.hAxis;
                if (reversePosition == true && gamepadManager.hAxis >= 0.1f)
                    sliderObject.value += changeValue * gamepadManager.hAxis;
                else if (reversePosition == false && gamepadManager.hAxis <= -0.1f)
                    sliderObject.value += changeValue * Mathf.Abs(gamepadManager.hAxis);
                else if (reversePosition == true && gamepadManager.hAxis <= -0.1f)
                    sliderObject.value -= changeValue * Mathf.Abs(gamepadManager.hAxis);
            }

            if (useScrolbar == true)
            {
                if (requireSelecting == true && useScrolbar == true && EventSystem.current != scrollbarObject)
                    return;

                if (isHorizontalScrollbar == false && gamepadManager.vAxis >= 0.1f && scrollbarObject.value <= 1)
                    scrollbarObject.value += changeValue * gamepadManager.vAxis;
                else if (isHorizontalScrollbar == false && gamepadManager.vAxis <= -0.1 && scrollbarObject.value >= 0)
                    scrollbarObject.value -= changeValue * Mathf.Abs(gamepadManager.vAxis);
                else if (isHorizontalScrollbar == true && gamepadManager.hAxis >= 0.1 && scrollbarObject.value <= 1)
                    scrollbarObject.value += changeValue * gamepadManager.hAxis;
                else if (isHorizontalScrollbar == true && gamepadManager.hAxis <= -0.1 && scrollbarObject.value >= 0)
                    scrollbarObject.value -= changeValue * Mathf.Abs(gamepadManager.hAxis);
            }

            if (useHorizontalSelector == true && hsIsInCooldown == false)
            {
                if (gamepadManager.hAxis >= 0.75)
                {
                    selectorObject.ForwardClick();
                    hsIsInCooldown = true;
                    StartCoroutine("SelectorCooldownTimer");
                }

                else if (gamepadManager.hAxis <= -0.75)
                {
                    selectorObject.PreviousClick();
                    hsIsInCooldown = true;
                    StartCoroutine("SelectorCooldownTimer");
                }
            }
        }

        IEnumerator SelectorCooldownTimer()
        {
            yield return new WaitForSecondsRealtime(selectorCooldown);
            hsIsInCooldown = false;
        }
    }
}