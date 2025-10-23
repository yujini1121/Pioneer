using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// mast의 '로컬 기준' 동/북/서/남 4방향을 초기 고정하고,
/// 각 방향 콘(±coneAngle) 내에서 mast로부터 가장 먼 '서로 다른' 플랫폼 1개씩 선택.
/// - 방향/플랫폼 중복 금지(그리디 매칭)
/// - 방향은 mast 로컬축(right/forward)에서 자동 추출하거나, 로컬축으로 직접 지정 가능
/// - GameManager 등 기존 코드는 수정 불필요
/// </summary>
public class EnemySpawnerFinder : MonoBehaviour
{
    [Header("기준")]
    public Transform mast;
    public LayerMask platformLayer;

    [Header("방향(로컬 기준) 초기 고정")]
    [Tooltip("시작 시 mast의 로컬축(오른쪽/앞)으로 자동 설정 (E=+right, N=+forward, W=-right, S=-forward)")]
    public bool autoFromMastLocalOnStart = true;

    [Tooltip("수동 지정 4방향 (mast의 로컬 좌표계 기준). 길이는 무시, 방향만 사용됨.\n인덱스: 0=E,1=N,2=W,3=S")]
    public Vector3[] customLocalDirs = new Vector3[4] {
        Vector3.right, Vector3.forward, Vector3.left, Vector3.back
    };

    [Header("필터 옵션")]
    [Range(1f, 90f)] public float coneAngle = 45f;     // 각 방향 허용 반각(도)
    [Tooltip("섹터 경계 떨림 방지용 보정 각도(도)")]
    [Range(0f, 2f)] public float boundaryEpsilonDeg = 0.1f;
    public bool refreshEveryFrame = false;

    [Header("Gizmo")]
    public bool drawGizmos = true;
    public float gizmoRadius = 0.2f;
    public Color mastColor;
    public Color[] dirColors = new Color[4];

    // 고정된 4방향 (mast 기준)
    readonly Vector3[] fixedDirsWorld = new Vector3[4];

    // 결과: 0:E 1:N 2:W 3:S
    public Transform[] result = new Transform[4];
    public Vector3[] resultPos = new Vector3[4];
    public bool[] found = new bool[4];

    // 내부 캐시
    readonly List<Cand> cands = new(128);
    float cosThreshold;
    float eps;

    struct Cand
    {
        public Transform tr;
        public Vector3 pos;
        public Vector3 v;      // mast->pos (y=0)
        public float dist;
    }

    struct Edge // 후보-방향 매칭 가중치
    {
        public int candIdx;
        public int dirIdx;
        public float score;    // proj = dist * cos(theta)
    }

    void Awake()
    {
        SetupDirsFromLocal();
        cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);
        eps = boundaryEpsilonDeg * Mathf.Deg2Rad;
    }

    void Start()
    {
        if (autoFromMastLocalOnStart) SetupDirsFromLocal();
        Refresh();
    }

    void Update()
    {
        if (refreshEveryFrame) Refresh();
    }

    /// <summary>
    /// mast의 '로컬' 방향을 월드로 변환해 4방향을 고정한다.
    /// (mast가 회전해도 방향을 바꾸지 않으려면 Start 이후 이 함수를 다시 호출하지 말 것)
    /// </summary>
    public void SetupDirsFromLocal()
    {
        if (mast == null)
        {
            // mast가 없으면 월드 기준으로라도 안전한 기본값
            fixedDirsWorld[0] = Vector3.right;
            fixedDirsWorld[1] = Vector3.forward;
            fixedDirsWorld[2] = Vector3.left;
            fixedDirsWorld[3] = Vector3.back;
            return;
        }

        if (autoFromMastLocalOnStart)
        {
            // mast '로컬' 기준축으로부터 (오른쪽/앞) 설정
            Vector3 r = mast.right; r.y = 0f; r = r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right;
            Vector3 f = mast.forward; f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;

            fixedDirsWorld[0] = r;    // E = +right
            fixedDirsWorld[1] = f;    // N = +forward
            fixedDirsWorld[2] = -r;   // W = -right
            fixedDirsWorld[3] = -f;   // S = -forward
        }
        else
        {
            // 사용자가 지정한 'mast 로컬 벡터'를 월드 방향으로 변환
            for (int i = 0; i < 4; i++)
            {
                Vector3 localDir = customLocalDirs[i].sqrMagnitude > 1e-6f ? customLocalDirs[i].normalized : Vector3.right;
                Vector3 worldDir = mast.TransformDirection(localDir);
                worldDir.y = 0f;
                fixedDirsWorld[i] = worldDir.sqrMagnitude > 1e-6f ? worldDir.normalized : Vector3.right;
            }
        }
    }

    /// <summary>
    /// 4방향 모두 '서로 다른' 플랫폼으로 채움(방향/플랫폼 중복 금지).
    /// 네 방향이 모두 채워져야 true.
    /// </summary>
    public bool Refresh()
    {
        for (int i = 0; i < 4; i++) { result[i] = null; resultPos[i] = default; found[i] = false; }
        if (mast == null) return false;

        CollectCandidates();
        if (cands.Count == 0) return false;

        // 모든 (candidate, direction) 조합 생성
        var edges = new List<Edge>(cands.Count * 2);
        for (int i = 0; i < cands.Count; i++)
        {
            var c = cands[i];
            if (c.dist < 1e-6f) continue;

            Vector3 vNorm = c.v / c.dist;

            for (int d = 0; d < 4; d++)
            {
                float cos = Vector3.Dot(vNorm, fixedDirsWorld[d]);
                // cone 내부만 허용 (경계 보정)
                if (cos <= cosThreshold - 1e-5f) continue;

                float proj = cos * c.dist; // 해당 방향 성분 길이(멀수록 우선)
                if (proj <= 0f) continue;

                edges.Add(new Edge { candIdx = i, dirIdx = d, score = proj });
            }
        }

        if (edges.Count == 0) return false;

        // 점수 내림차순
        edges.Sort((a, b) => b.score.CompareTo(a.score));

        // 그리디 매칭: 플랫폼/방향 중복 금지
        var candTaken = new bool[cands.Count];
        var dirTaken = new bool[4];
        int taken = 0;

        for (int i = 0; i < edges.Count && taken < 4; i++)
        {
            var e = edges[i];
            if (candTaken[e.candIdx] || dirTaken[e.dirIdx]) continue;

            candTaken[e.candIdx] = true;
            dirTaken[e.dirIdx] = true;
            taken++;

            var c = cands[e.candIdx];
            result[e.dirIdx] = c.tr;
            resultPos[e.dirIdx] = c.pos;
            found[e.dirIdx] = true;
        }

        // 4방향 모두 채워졌는지 확인
        for (int i = 0; i < 4; i++) if (!found[i]) return false;
        return true;
    }

    void CollectCandidates()
    {
        cands.Clear();
        var objs = FindObjectsOfType<GameObject>();

        // 같은 타일 중복 제거(격자 1x1 가정). 필요 시 태그/컴포넌트로 루트만 필터링하세요.
        var seen = new HashSet<Vector2Int>();

        for (int i = 0; i < objs.Length; i++)
        {
            var go = objs[i];
            if ((platformLayer.value & (1 << go.layer)) == 0) continue;

            Vector3 c = GetCenter(go);
            Vector3 v = c - mast.position; v.y = 0f;
            if (v.sqrMagnitude < 1e-6f) continue;

            var key = new Vector2Int(Mathf.RoundToInt(c.x), Mathf.RoundToInt(c.z));
            if (!seen.Add(key)) continue;

            cands.Add(new Cand { tr = go.transform, pos = c, v = v, dist = v.magnitude });
        }
    }

    static Vector3 GetCenter(GameObject go)
    {
        if (go.TryGetComponent<BoxCollider>(out var box)) return box.bounds.center;
        if (go.TryGetComponent<Renderer>(out var r)) return r.bounds.center;
        return go.transform.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // 에디터에서도 즉시 확인
        SetupDirsFromLocal();
        Refresh();

        // mast
        if (mast != null)
        {
            Gizmos.color = mastColor;
            Gizmos.DrawSphere(mast.position, gizmoRadius * 1.25f);
        }

        // 결과 4방향
        for (int i = 0; i < 4; i++)
        {
            if (!found[i] || result[i] == null) continue;
            Vector3 p = resultPos[i];

            Gizmos.color = dirColors[i % dirColors.Length];
            if (mast != null) Gizmos.DrawLine(mast.position, p);
            Gizmos.DrawSphere(p, gizmoRadius * 1.1f);

            string name = i switch { 0 => "E(local +right)", 1 => "N(local +forward)", 2 => "W(local -right)", 3 => "S(local -forward)", _ => i.ToString() };
            Handles.Label(p + Vector3.up * 0.15f, $"[{name}] {(mast ? Vector3.Distance(mast.position, p) : 0f):0.0}m");
        }

        // 누락 안내
        for (int i = 0; i < 4; i++)
            if (!found[i])
            {
                string name = i switch { 0 => "E(+right)", 1 => "N(+forward)", 2 => "W(-right)", 3 => "S(-forward)", _ => i.ToString() };
                Handles.Label((mast ? mast.position : transform.position) + Vector3.up * (0.3f + 0.1f * i),
                    $"[{name}] 없음 (coneAngle={coneAngle}°)");
            }
    }
#endif
}
