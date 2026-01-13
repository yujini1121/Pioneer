using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Michsky.UI.Dark
{
    [AddComponentMenu("Dark UI/Rendering/Blur Manager")]
    public class BlurManager : MonoBehaviour
    {
        // Settings
        public Material blurMaterial;
        [Range(0.0f, 10)] public float blurValue = 5.0f;
        [Range(0.1f, 50)] public float animationSpeed = 25;
        public string customProperty = "_Size";

        float currentBlurValue;

        void Start()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
                return;

            if (customProperty == null)
                customProperty = "_Size";

            blurMaterial.SetFloat(customProperty, 0);
        }

        IEnumerator BlurIn()
        {
            currentBlurValue = blurMaterial.GetFloat(customProperty);

            while (currentBlurValue <= blurValue)
            {
                currentBlurValue += Time.unscaledDeltaTime * animationSpeed;

                if (currentBlurValue >= blurValue)
                    currentBlurValue = blurValue;

                blurMaterial.SetFloat(customProperty, currentBlurValue);
                yield return null;
            }
        }

        IEnumerator BlurOut()
        {
            currentBlurValue = blurMaterial.GetFloat(customProperty);

            while (currentBlurValue >= 0)
            {
                currentBlurValue -= Time.unscaledDeltaTime * animationSpeed;

                if (currentBlurValue <= 0)
                    currentBlurValue = 0;

                blurMaterial.SetFloat(customProperty, currentBlurValue);
                yield return null;
            }
        }

        public void BlurInAnim()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
                return;

            if (gameObject.activeSelf == false)
                gameObject.SetActive(true);

            StopCoroutine("BlurOut");
            StartCoroutine("BlurIn");
        }

        public void BlurOutAnim()
        {
            if (GraphicsSettings.renderPipelineAsset != null || gameObject.activeInHierarchy == false)
                return;

            StopCoroutine("BlurIn");
            StartCoroutine("BlurOut");
        }

        public void SetBlurValue(float cbv)
        {
            blurValue = cbv;
        }
    }
}