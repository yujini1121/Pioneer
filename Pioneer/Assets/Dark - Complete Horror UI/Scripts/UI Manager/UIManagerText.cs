using UnityEngine;
using TMPro;

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dark UI/UI Manager/UI Manager Text")]
    public class UIManagerText : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager UIManagerAsset;
        public TextMeshProUGUI textObject;

        [Header("Settings")]
        public bool keepAlphaValue = false;
        public bool useCustomColor = false;
        public ColorType colorType;
        public FontType fontType;

        public enum ColorType
        {
            PRIMARY,
            SECONDARY,
            PRIMARY_REVERSED,
            NEGATIVE,
            BACKGROUND,
            BACKGROUND_ALT
        }

        public enum FontType
        {
            LIGHT,
            MEDIUM,
            BOLD,
            ALTERNATIVE_1,
            ALTERNATIVE_2
        }

        void Awake()
        {
            if (UIManagerAsset == null)
            {
                try
                {
                    UIManagerAsset = Resources.Load<UIManager>("Dark UI Manager");
                    this.enabled = true;

                    if (UIManagerAsset.enableDynamicUpdate == false)
                    {
                        UpdateText();
                        this.enabled = false;
                    }
                }

                catch { Debug.LogWarning("No <b>UI Manager</b> variable found. Please assign it manually.", this); }
            }

            if (textObject == null)
            {
                try { textObject = gameObject.GetComponent<TextMeshProUGUI>(); }
                catch { }
            }
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null || textObject == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateText();
        }

        void UpdateText()
        {
            if (useCustomColor == false)
            {
                if (keepAlphaValue == false)
                {
                    if (colorType == ColorType.PRIMARY)
                        textObject.color = UIManagerAsset.primaryColor;
                    else if (colorType == ColorType.SECONDARY)
                        textObject.color = UIManagerAsset.secondaryColor;
                    else if (colorType == ColorType.PRIMARY_REVERSED)
                        textObject.color = UIManagerAsset.primaryReversed;
                    else if (colorType == ColorType.NEGATIVE)
                        textObject.color = UIManagerAsset.negativeColor;
                    else if (colorType == ColorType.BACKGROUND)
                        textObject.color = UIManagerAsset.backgroundColor;
                    else if (colorType == ColorType.BACKGROUND_ALT)
                        textObject.color = UIManagerAsset.backgroundColorAlt;
                }

                else
                {
                    if (colorType == ColorType.PRIMARY)
                        textObject.color = new Color(UIManagerAsset.primaryColor.r, UIManagerAsset.primaryColor.g, UIManagerAsset.primaryColor.b, textObject.color.a);
                    else if (colorType == ColorType.SECONDARY)
                        textObject.color = new Color(UIManagerAsset.secondaryColor.r, UIManagerAsset.secondaryColor.g, UIManagerAsset.secondaryColor.b, textObject.color.a);
                    else if (colorType == ColorType.PRIMARY_REVERSED)
                        textObject.color = new Color(UIManagerAsset.primaryReversed.r, UIManagerAsset.primaryReversed.g, UIManagerAsset.primaryReversed.b, textObject.color.a);
                    else if (colorType == ColorType.NEGATIVE)
                        textObject.color = new Color(UIManagerAsset.negativeColor.r, UIManagerAsset.negativeColor.g, UIManagerAsset.negativeColor.b, textObject.color.a);
                    else if (colorType == ColorType.BACKGROUND)
                        textObject.color = new Color(UIManagerAsset.backgroundColor.r, UIManagerAsset.backgroundColor.g, UIManagerAsset.backgroundColor.b, textObject.color.a);
                    else if (colorType == ColorType.BACKGROUND_ALT)
                        textObject.color = new Color(UIManagerAsset.backgroundColorAlt.r, UIManagerAsset.backgroundColorAlt.g, UIManagerAsset.backgroundColorAlt.b, textObject.color.a);
                }
            }

            if (fontType == FontType.LIGHT)
                textObject.font = UIManagerAsset.lightFont;
            else if (fontType == FontType.MEDIUM)
                textObject.font = UIManagerAsset.mediumFont;
            else if (fontType == FontType.BOLD)
                textObject.font = UIManagerAsset.boldFont;
            else if (fontType == FontType.ALTERNATIVE_1)
                textObject.font = UIManagerAsset.altFont;
            else if (fontType == FontType.ALTERNATIVE_2)
                textObject.font = UIManagerAsset.alt2Font;
        }
    }
}