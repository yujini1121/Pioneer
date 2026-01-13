using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.UI.Dark
{
    [RequireComponent(typeof(Slider))]
    public class SliderManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // Resources
        public Animator sliderAnimator;
        public Slider mainSlider;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI popupValueText;

        // Saving
        [SerializeField] private bool enableSaving = false;
        public string sliderTag = "Tag Text";
        public float saveValue;

        // Settings
        public bool usePercent = false;
        public bool showValue = true;
        public bool showPopupValue = true;
        public bool useRoundValue = false;
        [SerializeField] private bool invokeOnAwake = true;
        public float valueMultiplier = 1;

        void Awake()
        {
            if (mainSlider == null) { mainSlider = gameObject.GetComponent<Slider>(); }
            if (enableSaving)
            {
                if (PlayerPrefs.HasKey(sliderTag + "Slider") == false) { saveValue = mainSlider.value; }
                else { saveValue = PlayerPrefs.GetFloat(sliderTag + "Slider"); }

                mainSlider.value = saveValue;
            }

            mainSlider.onValueChanged.AddListener(delegate
            {
                saveValue = mainSlider.value;
                UpdateUI();

                PlayerPrefs.SetFloat(sliderTag + "Slider", saveValue);
            });

            if (invokeOnAwake) { mainSlider.onValueChanged.Invoke(mainSlider.value); }
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (useRoundValue)
            {
                if (usePercent && valueText != null) { valueText.text = Mathf.Round(mainSlider.value * valueMultiplier).ToString() + "%"; }
                else if (!usePercent && valueText != null) { valueText.text = Mathf.Round(mainSlider.value * valueMultiplier).ToString(); }
            }

            else
            {
                if (usePercent && valueText != null) { valueText.text = mainSlider.value.ToString("F1") + "%"; }
                else if (!usePercent && valueText != null) { valueText.text = mainSlider.value.ToString("F1"); }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showPopupValue)
                sliderAnimator.Play("Value In");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (showPopupValue)
                sliderAnimator.Play("Value Out");
        }
    }
}