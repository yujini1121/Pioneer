using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CreatureEffect : MonoBehaviour
{
    public static CreatureEffect Instance;

    [Header("게임 오브젝트에 넣을 이펙트")]
    public ParticleSystem[] Effects;

    [Header("자신 혹은 먼 다른 게임오브젝트에 소환시킬 이펙트를 담은 프리펩")]
    public GameObject prefabForEffect;
    // 이후에 더 추가할 것
    // 그러나 게임오브젝트는 반드시 프로젝트 창에 있는 프리펩이여야지.
    // 절대로 씬에 위치한, 어떤 게임오브젝트의 자식 게임오브젝트를 긁어와선 안 됨

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayEffect(ParticleSystem pooled, Vector3 pos)
    {
        if (!pooled) return;

        if (pooled.gameObject.activeInHierarchy)
        {
            // 이미 누군가 쓰는 중 → 복제본 생성 후 자동 소멸
            var clone = Instantiate(pooled, pos, pooled.transform.rotation);
            SetOneShot(clone);
            clone.Clear(true);
            clone.Play(true);
            Destroy(clone.gameObject, GetTotalDuration(clone.gameObject) + 0.1f);
            return;
        }

        // 놀고 있으면 풀링 개체 재사용 + 타임아웃으로 꺼주기
        pooled.transform.position = pos;
        SetPooled(pooled);
        pooled.gameObject.SetActive(true);
        pooled.Clear(true);
        pooled.Play(true);

        float t = GetTotalDuration(pooled.gameObject) + 0.1f;
        StartCoroutine(DisableAfter(pooled.gameObject, t));
    }

    void SetOneShot(ParticleSystem ps)
    {
        var m = ps.main;
        m.loop = false;
        m.stopAction = ParticleSystemStopAction.Destroy; // 끝나면 파괴
    }

    void SetPooled(ParticleSystem ps)
    {
        var m = ps.main;
        m.loop = false;
        m.stopAction = ParticleSystemStopAction.None; // 끝나면 직접 비활성화
    }

    IEnumerator DisableAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) go.SetActive(false);
    }

    // 자식 포함 가장 긴 재생 시간 계산(곡선/랜덤 대응)
    float GetTotalDuration(GameObject root)
    {
        float maxT = 0f;
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var m = ps.main;
            float lifeMax = 0f;
            switch (m.startLifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    lifeMax = m.startLifetime.constant; break;
                case ParticleSystemCurveMode.TwoConstants:
                    lifeMax = Mathf.Max(m.startLifetime.constantMin, m.startLifetime.constantMax); break;
                case ParticleSystemCurveMode.Curve:
                    lifeMax = m.startLifetime.curve.Evaluate(1f); break;
                case ParticleSystemCurveMode.TwoCurves:
                    lifeMax = Mathf.Max(m.startLifetime.curveMin.Evaluate(1f), m.startLifetime.curveMax.Evaluate(1f)); break;
            }
            float t = m.duration + lifeMax;
            if (t > maxT) maxT = t;
        }
        return maxT;
    }

    // CreatureEffect 안에 추가
    public void PlayEffectFollow(ParticleSystem pooled, Transform target, Vector3 localOffset)
    {
        if (!pooled || !target) return;

        // 재생 중이면 복제본 생성 (끝나면 자동 파괴)
        if (pooled.gameObject.activeInHierarchy)
        {
            var clone = Instantiate(pooled, target.position, pooled.transform.rotation, target);
            clone.transform.localPosition = localOffset;
            var m = clone.main;
            m.loop = false;
            m.simulationSpace = ParticleSystemSimulationSpace.Local; // 부모 기준으로 따라감
            m.stopAction = ParticleSystemStopAction.Destroy;
            clone.Clear(true);
            clone.Play(true);
            return;
        }

        // 풀링된 개체 재사용
        pooled.transform.SetParent(target);
        pooled.transform.localPosition = localOffset;
        var main = pooled.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.stopAction = ParticleSystemStopAction.None;
        pooled.gameObject.SetActive(true);
        pooled.Clear(true);
        pooled.Play(true);

        float t = GetTotalDuration(pooled.gameObject) + 0.05f;
        StartCoroutine(DisableAfterAndUnparent(pooled.gameObject, t));
    }

    private IEnumerator DisableAfterAndUnparent(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (!go) yield break;
        go.transform.SetParent(Instance.transform, worldPositionStays: true); // 풀로 되돌림(원하면 전용 부모 사용)
        go.SetActive(false);
    }
    /*public void PlayEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log("이펙트 널 버그");
        }
    }*/
}
