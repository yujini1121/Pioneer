using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogFade : MonoBehaviour
{
    [Header("안개 파티클")]
    [SerializeField] private ParticleSystem[] fogParticles;

    [Header("페이드 설정")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    private Coroutine fadeCoroutine;

    private readonly List<float> baseEmissionRates = new List<float>();

    private void Awake()
    {
        if (fogParticles == null || fogParticles.Length == 0)
            fogParticles = GetComponentsInChildren<ParticleSystem>(true);

        CacheBaseEmissionRates();
        SetEmissionMultiplier(0f);
    }

    private void CacheBaseEmissionRates()
    {
        baseEmissionRates.Clear();

        for (int i = 0; i < fogParticles.Length; i++)
        {
            if (fogParticles[i] == null)
            {
                baseEmissionRates.Add(0f);
                continue;
            }

            var emission = fogParticles[i].emission;
            baseEmissionRates.Add(emission.rateOverTimeMultiplier);
        }
    }

    public void ShowFog()
    {
        gameObject.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration, false));
    }

    public void HideFog()
    {
        if (!gameObject.activeSelf)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, fadeOutDuration, true));
    }

    private IEnumerator FadeRoutine(float start, float end, float duration, bool disableAfterFade)
    {
        PlayAllParticles();

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);
            float value = Mathf.Lerp(start, end, t);

            SetEmissionMultiplier(value);
            yield return null;
        }

        SetEmissionMultiplier(end);

        if (disableAfterFade)
        {
            StopAllParticles();
            gameObject.SetActive(false);
        }

        fadeCoroutine = null;
    }

    private void SetEmissionMultiplier(float multiplier)
    {
        for (int i = 0; i < fogParticles.Length; i++)
        {
            if (fogParticles[i] == null) continue;

            var emission = fogParticles[i].emission;
            emission.rateOverTimeMultiplier = baseEmissionRates[i] * multiplier;
        }
    }

    private void PlayAllParticles()
    {
        for (int i = 0; i < fogParticles.Length; i++)
        {
            if (fogParticles[i] == null) continue;

            if (!fogParticles[i].isPlaying)
                fogParticles[i].Play();
        }
    }

    private void StopAllParticles()
    {
        for (int i = 0; i < fogParticles.Length; i++)
        {
            if (fogParticles[i] == null) continue;

            fogParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}