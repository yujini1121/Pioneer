using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Michsky.UI.Dark
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ModalWindowManager : MonoBehaviour
    {
        // Content
        public string title = "Title";
        [TextArea(0, 4)] public string description = "Description";

        // Resources
        [SerializeField] private Animator windowAnimator;
        public TextMeshProUGUI titleObject;
        public TextMeshProUGUI descriptionObject;
        [SerializeField] private BlurManager blurManager;

        // Effects
        public bool enableDissolve = true;
        [SerializeField] private UIDissolveEffect dissolveEffect;

        // Settings
        [SerializeField] private bool useCustomContent;
        public bool disableOnOut = true;
        [SerializeField] [Range(0.1f, 5)] private float disableAfter = 1;
        [SerializeField] [Range(0, 1)] private float inputBlockDuration = 0.1f;

        // Events
        public UnityEvent onEnable = new UnityEvent();
        public UnityEvent onCancel = new UnityEvent();

        // Helpers
        bool waitingForTimer = false;
        [HideInInspector] public bool isOn;

        void OnEnable()
        {
            isOn = false;
            waitingForTimer = false;
        }

        public void ModalWindowIn()
        {
            if (isOn || waitingForTimer)
                return;

            if (windowAnimator == null) { windowAnimator = gameObject.GetComponent<Animator>(); }
            if (!useCustomContent) { UpdateUI(); }
            if (blurManager != null) { blurManager.BlurInAnim(); }

            StopCoroutine("DisableModal");
            StopCoroutine("StateLatency");

            windowAnimator.gameObject.SetActive(true);
            windowAnimator.Play("In");
            onEnable.Invoke();

            if (enableDissolve && dissolveEffect != null)
            {
                dissolveEffect.location = 1;
                dissolveEffect.DissolveIn();
            }

            if (inputBlockDuration == 0) { isOn = true; }
            else { StartCoroutine("StateLatency", true); waitingForTimer = true; }
        }

        public void ModalWindowOut()
        {
            if (!isOn || waitingForTimer)
                return;

            StopCoroutine("DisableModal");
            StopCoroutine("StateLatency");

            windowAnimator.Play("Out");
            onCancel.Invoke();

            if (enableDissolve && dissolveEffect != null) { dissolveEffect.DissolveOut(); }
            if (disableOnOut) { StartCoroutine("DisableModal"); }
            if (blurManager != null) { blurManager.BlurOutAnim(); }

            if (inputBlockDuration == 0) { isOn = false; }
            else { StartCoroutine("StateLatency", false); waitingForTimer = true; }
        }

        public void UpdateUI()
        {
            titleObject.text = title;
            descriptionObject.text = description;
        }

        IEnumerator DisableModal()
        {
            yield return new WaitForSecondsRealtime(disableAfter);
            gameObject.SetActive(false);
            waitingForTimer = false;
            isOn = false;
        }

        IEnumerator StateLatency(bool value)
        {
            yield return new WaitForSecondsRealtime(inputBlockDuration);
            waitingForTimer = false;
            isOn = value;
        }
    }
}