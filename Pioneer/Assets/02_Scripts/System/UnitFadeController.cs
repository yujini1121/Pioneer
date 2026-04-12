using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitFadeController : MonoBehaviour
{
    [Header("Fade")]
    [Min(0f)] public float fadeInDurations = 1.5f;
    [Min(0f)] public float fadeOutDurations = 1.5f;
    public bool playFadeInOnEnable = true;
    public bool stopAgentDuringFade = true;

    private SpriteRenderer targetSprite;
    private NavMeshAgent agent;

    private readonly List<Behaviour> toToggleBehaviours = new();
    private readonly List<Collider> toToggleColliders = new();

    private Coroutine fadeRoutine;
    private bool cached;

    void Awake()
    {
        Cache();
    }

    void OnEnable()
    {
        if (playFadeInOnEnable)
            PlaySpawnFade();
    }

    private void Cache()
    {
        if (cached) return;
        cached = true;

        agent = GetComponent<NavMeshAgent>();

        if (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            targetSprite = child.GetComponent<SpriteRenderer>();
        }

        var behaviours = GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (ReferenceEquals(b, this)) continue;
            if (b is NavMeshAgent) continue;
            if (b is Animator) continue; // 애니메이션은 유지
            toToggleBehaviours.Add(b);
        }

        GetComponentsInChildren(true, toToggleColliders);
    }

    public void PlaySpawnFade()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInCo());
    }

    public void PlayDespawnFadeAndDestroy()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOutAndDestroyCo());
    }

    private IEnumerator FadeInCo()
    {
        Cache();

        SetInteractionEnabled(false);

        if (stopAgentDuringFade && agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        SetAlpha(0f);

        float t = 0f;
        while (t < fadeInDurations)
        {
            t += Time.deltaTime;
            float a = (fadeInDurations <= 0f) ? 1f : Mathf.Clamp01(t / fadeInDurations);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(1f);

        if (stopAgentDuringFade && agent != null)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        SetInteractionEnabled(true);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndDestroyCo()
    {
        Cache();

        SetInteractionEnabled(false);

        if (stopAgentDuringFade && agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        float startAlpha = GetCurrentAlpha();
        float t = 0f;

        while (t < fadeOutDurations)
        {
            t += Time.deltaTime;
            float n = (fadeOutDurations <= 0f) ? 1f : Mathf.Clamp01(t / fadeOutDurations);
            SetAlpha(Mathf.Lerp(startAlpha, 0f, n));
            yield return null;
        }

        SetAlpha(0f);
        fadeRoutine = null;
        Destroy(gameObject);
    }

    private void SetInteractionEnabled(bool enabledState)
    {
        foreach (var c in toToggleColliders)
        {
            if (c != null) c.enabled = enabledState;
        }

        foreach (var b in toToggleBehaviours)
        {
            if (b != null) b.enabled = enabledState;
        }
    }

    private float GetCurrentAlpha()
    {
        if (targetSprite == null) return 1f;
        return targetSprite.color.a;
    }

    private void SetAlpha(float a)
    {
        if (targetSprite == null) return;

        Color c = targetSprite.color;
        c.a = a;
        targetSprite.color = c;
    }
}