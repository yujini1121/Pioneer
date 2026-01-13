using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dark UI/UI Manager/UI Manager Image")]
    public class UIManagerImage : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager UIManagerAsset;
        public Image imageObject;

        [Header("Settings")]
        public bool keepAlphaValue = false;
        public bool useCustomColor = false;
        public ColorType colorType;

        public enum ColorType
        {
            PRIMARY,
            SECONDARY,
            PRIMARY_REVERSED,
            NEGATIVE,
            BACKGROUND,
            BACKGROUND_ALT
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
                        UpdateImage();
                        this.enabled = false;
                    }
                }

                catch { Debug.LogWarning("No <b>UI Manager</b> variable found. Please assign it manually.", this); }
            }

            if (imageObject == null)
            {
                try { imageObject = gameObject.GetComponent<Image>(); }
                catch { }
            }
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null || imageObject == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateImage();
        }

        void UpdateImage()
        {
            if (useCustomColor == true)
                return;

            if (keepAlphaValue == false)
            {
                if (colorType == ColorType.PRIMARY)
                    imageObject.color = UIManagerAsset.primaryColor;
                else if (colorType == ColorType.SECONDARY)
                    imageObject.color = UIManagerAsset.secondaryColor;
                else if (colorType == ColorType.PRIMARY_REVERSED)
                    imageObject.color = UIManagerAsset.primaryReversed;
                else if (colorType == ColorType.NEGATIVE)
                    imageObject.color = UIManagerAsset.negativeColor;
                else if (colorType == ColorType.BACKGROUND)
                    imageObject.color = UIManagerAsset.backgroundColor;
                else if (colorType == ColorType.BACKGROUND_ALT)
                    imageObject.color = UIManagerAsset.backgroundColorAlt;
            }

            else
            {
                if (colorType == ColorType.PRIMARY)
                    imageObject.color = new Color(UIManagerAsset.primaryColor.r, UIManagerAsset.primaryColor.g, UIManagerAsset.primaryColor.b, imageObject.color.a);
                else if (colorType == ColorType.SECONDARY)
                    imageObject.color = new Color(UIManagerAsset.secondaryColor.r, UIManagerAsset.secondaryColor.g, UIManagerAsset.secondaryColor.b, imageObject.color.a);
                else if (colorType == ColorType.PRIMARY_REVERSED)
                    imageObject.color = new Color(UIManagerAsset.primaryReversed.r, UIManagerAsset.primaryReversed.g, UIManagerAsset.primaryReversed.b, imageObject.color.a);
                else if (colorType == ColorType.NEGATIVE)
                    imageObject.color = new Color(UIManagerAsset.negativeColor.r, UIManagerAsset.negativeColor.g, UIManagerAsset.negativeColor.b, imageObject.color.a);
                else if (colorType == ColorType.BACKGROUND)
                    imageObject.color = new Color(UIManagerAsset.backgroundColor.r, UIManagerAsset.backgroundColor.g, UIManagerAsset.backgroundColor.b, imageObject.color.a);
                else if (colorType == ColorType.BACKGROUND_ALT)
                    imageObject.color = new Color(UIManagerAsset.backgroundColorAlt.r, UIManagerAsset.backgroundColorAlt.g, UIManagerAsset.backgroundColorAlt.b, imageObject.color.a);
            }
        }
    }
}