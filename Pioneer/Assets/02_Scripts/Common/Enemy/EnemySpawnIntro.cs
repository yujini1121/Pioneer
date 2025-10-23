// EnemySpawnIntro.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnIntro : MonoBehaviour
{
    [Header("Intro")]
    [Min(0f)] public float fadeDuration = 1.0f;
    public bool playOnEnable = true;
    [Tooltip("페이드 중 이동 멈춤")]
    public bool stopAgentDuringIntro = true;

    // 내부 캐시
    SpriteRenderer sprite;             // 첫 번째 자식 "2D Sprite"의 SR
    NavMeshAgent agent;                // 항상 제외
    readonly List<Behaviour> toToggleBehaviours = new();
    readonly List<Collider> toToggleColliders = new();
    readonly List<Renderer> toToggleRenderers = new(); // SpriteRenderer 제외
    bool cached;

    void Awake()
    {
        Cache();
    }

    void OnEnable()
    {
        if (playOnEnable) StartCoroutine(IntroCo());
    }

    /// <summary>스폰 직후 수동으로 호출하고 싶을 때</summary>
    public void TriggerNow() => StartCoroutine(IntroCo());

    void Cache()
    {
        if (cached) return;
        cached = true;

        agent = GetComponent<NavMeshAgent>();

        // === 첫 번째 자식 "2D Sprite" 가정 ===
        if (transform.childCount > 0)
        {
            var child = transform.GetChild(0);
            sprite = child.GetComponent<SpriteRenderer>();
        }

        if (sprite == null)
            Debug.LogWarning($"[EnemySpawnIntro] '{name}'에 첫 번째 자식 SpriteRenderer가 없습니다. (\"2D Sprite\" 형태를 기대)");

        // 끄고/켜줄 Behaviour 수집 (NavMeshAgent, 자신 제외)
        var behaviours = GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (ReferenceEquals(b, this)) continue;
            if (b is NavMeshAgent) continue;     // 요구사항: Agent는 제외
            toToggleBehaviours.Add(b);
        }

        // Collider들 수집
        GetComponentsInChildren(true, toToggleColliders);

        // 다른 Renderer는 모두 숨김 (SpriteRenderer는 페이드로만 처리)
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (sprite != null && ReferenceEquals(r, sprite)) continue;
            toToggleRenderers.Add(r);
        }
    }

    IEnumerator IntroCo()
    {
        Cache();

        // 0) 사전 상태 세팅
        foreach (var b in toToggleBehaviours) if (b) b.enabled = false;
        foreach (var c in toToggleColliders) if (c) c.enabled = false;
        foreach (var r in toToggleRenderers) if (r) r.enabled = false;

        if (stopAgentDuringIntro && agent) agent.isStopped = true;

        // 스프라이트 알파 0으로 시작
        SetAlpha(0f);

        // 1) 페이드 0→1
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = (fadeDuration <= 0f) ? 1f : Mathf.Clamp01(t / fadeDuration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(1f);

        // 2) 모두 활성화
        foreach (var r in toToggleRenderers) if (r) r.enabled = true;
        foreach (var c in toToggleColliders) if (c) c.enabled = true;
        foreach (var b in toToggleBehaviours) if (b) b.enabled = true;

        if (stopAgentDuringIntro && agent) agent.isStopped = false;
    }

    void SetAlpha(float a)
    {
        if (!sprite) return;
        var c = sprite.color;
        c.a = a;
        sprite.color = c;
    }
}
