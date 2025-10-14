using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)] // CinemachineBrain 적용 후 실행되도록(옵션)
public class CameraOcclusionFader : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public LayerMask occluderMask;

    [Header("Fade Settings")]
    [Range(0f, 1f)] public float fadeAlpha = 0.35f; // 목표 반투명도
    [Range(0f, 1f)] public float minAlpha = 0.30f; // 절대 최소 알파(안보일 정도로 내려가지 않게)
    public float fadeSpeed = 10f;
    public float sphereRadius = 0.35f;

    // 성능용 NonAlloc 버퍼 (필요시 크기 키우기)
    const int MaxHits = 64;
    static readonly RaycastHit[] _hitsBuffer = new RaycastHit[MaxHits];

    readonly Dictionary<Renderer, float> _current = new();   // 현재 적용 중 알파
    readonly HashSet<Renderer> _hitsThisFrame = new();       // 이번 프레임에 걸린 렌더러
    readonly List<Renderer> _toRestore = new();              // 복원 예정 임시 리스트
    MaterialPropertyBlock _mpb;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        _hitsThisFrame.Clear();

        // 카메라 → 플레이어 방향으로 캐스트
        var camPos = transform.position;
        var dir = player.position - camPos;
        var dist = dir.magnitude;
        if (dist <= 0.001f) return;

        var ray = new Ray(camPos, dir / dist);

        // NonAlloc 캐스트 (GC 없음)
        int hitCount = Physics.SphereCastNonAlloc(
            ray, sphereRadius, _hitsBuffer, dist,
            occluderMask, QueryTriggerInteraction.Ignore);

        // 맞은 콜라이더들의 모든 Renderer 수집
        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitsBuffer[i].collider;
            // 자식까지 모두 포함 (메시에 자식이 많으면 캐싱 구조 고려)
            var renderers = col.GetComponentsInChildren<Renderer>(includeInactive: false);
            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                _hitsThisFrame.Add(rend);
                if (!_current.ContainsKey(rend)) _current[rend] = 1f; // 최초 등록시 기본 알파 1
            }
        }

        // 1) 걸린 렌더러는 목표 알파까지 감소
        foreach (var rend in _hitsThisFrame)
        {
            float a0 = GetAlpha(rend);
            float a1 = Mathf.MoveTowards(a0, fadeAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(rend, a1);
        }

        // 2) 안 걸린 렌더러는 1.0으로 복원 (끝나면 딕셔너리 제거)
        _toRestore.Clear();
        foreach (var kv in _current)
        {
            var rend = kv.Key;
            if (_hitsThisFrame.Contains(rend)) continue;

            float a0 = kv.Value;
            float a1 = Mathf.MoveTowards(a0, 1f, fadeSpeed * Time.deltaTime);
            ApplyAlpha(rend, a1);

            if (Mathf.Approximately(a1, 1f))
                _toRestore.Add(rend);
        }
        for (int i = 0; i < _toRestore.Count; i++)
            _current.Remove(_toRestore[i]);
    }

    float GetAlpha(Renderer r)
        => _current.TryGetValue(r, out float a) ? a : 1f; // 기본값 1로 수정

    void ApplyAlpha(Renderer r, float a)
    {
        // 최소/최대 범위 고정 (완전 투명 방지)
        a = Mathf.Clamp(a, minAlpha, 1f);
        _current[r] = a;

        // 2D 스프라이트인 경우(가장 확실): SpriteRenderer.color
        if (r is SpriteRenderer sr)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
            return;
        }

        // 일반 Renderer: 머티리얼 슬롯(서브메시)별로 MPB 적용
        // 주의: Opaque 머티리얼은 알파가 렌더링에 반영되지 않음(Transparent/디더 셰이더 필요)
        var mats = r.sharedMaterials;
        int matCount = mats != null ? mats.Length : 0;
        for (int i = 0; i < matCount; i++)
        {
            var m = mats[i];
            if (m == null) continue;

            bool wrote = false;

            // URP Lit 계열
            if (m.HasProperty("_BaseColor"))
            {
                // 원본 색 유지 + 알파만 교체
                Color c = m.GetColor("_BaseColor");
                c.a = a;
                _mpb.Clear();
                _mpb.SetColor("_BaseColor", c);
                r.SetPropertyBlock(_mpb, i);
                wrote = true;
            }
            // 레거시 또는 커스텀 컬러
            else if (m.HasProperty("_Color"))
            {
                Color c = m.GetColor("_Color");
                c.a = a;
                _mpb.Clear();
                _mpb.SetColor("_Color", c);
                r.SetPropertyBlock(_mpb, i);
                wrote = true;
            }

            // 어떤 프로퍼티도 못썼다면(셰이더가 알파를 노출 안 함) - 아무것도 하지 않음
            // 필요시: 디버그 로그를 넣어 문제 머티리얼 추적 가능
            // if (!wrote) Debug.Log($"[OcclusionFader] No _BaseColor/_Color on {r.name} (mat:{m.name})");
        }
    }
}
