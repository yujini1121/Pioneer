using System.Collections;
using UnityEngine;

namespace Michsky.UI.Dark
{
    [RequireComponent(typeof(Animator))]
    public class SplashScreenTitle : MonoBehaviour
    {
        [Header("Resources")]
        public UIDissolveEffect dissolveEffect;
        public AudioSource soundSource;
        public AudioClip inSound;
        public AudioClip outSound;
        Animator objAnimator;

        [Header("Settings")]
        [Range(3, 30)] public float screenTime = 8;
        [Range(0.1f, 1)] public float titleSpeed = 1;
        [Range(1, 10)] public float transitionMultiplier = 4;

        void OnEnable()
        {
            if (objAnimator == null)
                objAnimator = gameObject.GetComponent<Animator>();

            objAnimator.SetFloat("Speed", titleSpeed);
            dissolveEffect.gameObject.SetActive(true);
            dissolveEffect.location = 0;
            dissolveEffect.DissolveOut();
            StartCoroutine("StartDissolveIn");

            if (soundSource != null && inSound != null)
                soundSource.PlayOneShot(inSound);
        }

        IEnumerator StartDissolveIn()
        {
            yield return new WaitForSecondsRealtime(screenTime - dissolveEffect.animationSpeed * transitionMultiplier);
            dissolveEffect.DissolveIn();

            if (soundSource != null && inSound != null)
                soundSource.PlayOneShot(outSound);

            StopCoroutine("StartDissolveIn");
        }
    }
}