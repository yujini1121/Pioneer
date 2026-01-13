using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dark UI/UI Manager/UI Manager Background")]
    public class UIManagerBackground : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager UIManagerAsset;
        public Image backgroundObject;

        public enum BackgroundType
        {
            BASIC,
            ADVANCED
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
                        UpdateBackground();
                        this.enabled = false;
                    }
                }

                catch { Debug.LogWarning("No <b>UI Manager</b> variable found. Please assign it manually.", this); }
            }

            if (backgroundObject == null)
                backgroundObject = gameObject.GetComponent<Image>();
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateBackground();
        }

        public void UpdateBackground()
        {
            backgroundObject.enabled = true;
            backgroundObject.sprite = UIManagerAsset.backgroundImage;
            backgroundObject.color = UIManagerAsset.backgroundColorTint;
        }
    }
}