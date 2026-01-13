using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dark UI/UI Manager/UI Game Logo")]
    public class UIManagerLogo : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager UIManagerAsset;
        public Image logoObject;

        [Header("Settings")]
        public bool keepAlphaValue = false;
        public bool useCustomColor = false;
        public LogoType logoType;

        public enum LogoType
        {
            BRAND,
            GAME
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
                        UpdateLogo();
                        this.enabled = false;
                    }
                }

                catch { Debug.LogWarning("No <b>UI Manager</b> variable found. Please assign it manually.", this); }
            }

            if (logoObject == null)
            {
                try { logoObject = gameObject.GetComponent<Image>(); }
                catch { }
            }
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null || logoObject == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateLogo();
        }

        void UpdateLogo()
        {
            if (logoType == LogoType.GAME)
                logoObject.sprite = UIManagerAsset.gameLogo;
            else
            {
                logoObject.sprite = UIManagerAsset.brandLogo;
                return;
            }

            if (useCustomColor == false)
            {
                if (keepAlphaValue == false)
                    logoObject.color = UIManagerAsset.logoColor;
                else
                    logoObject.color = new Color(UIManagerAsset.logoColor.r, UIManagerAsset.logoColor.g, UIManagerAsset.logoColor.b, logoObject.color.a);
            }
        }
    }
}