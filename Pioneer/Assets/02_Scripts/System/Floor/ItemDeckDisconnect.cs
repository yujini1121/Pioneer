using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 갑판 개개별의 정보
/// </summary>
[System.Serializable]
class DeckInfo
{
    public Vector2Int coord;
    public GameObject obj;
    public bool isConnected;
}

/// <summary>
/// 갑판 파괴 시스템:
/// - 씬 시작 시 "Platform" 레이어의 모든 갑판 자동 수집
/// - 갑판 설치/파괴 이후 BFS로 돛대와 연결 여부 갱신
/// - isConnected = true/false 로 표시
/// </summary>
public class ItemDeckDisconnect : MonoBehaviour
{
    public static ItemDeckDisconnect instance;

    [SerializeField] private Transform mast;                // 돛대
    [SerializeField] private GameObject worldSpace;         // 갑판 부모 오브젝트
    [SerializeField] private LayerMask deckLayer;           // "Platform" 레이어
    [SerializeField] private Vector2 gridSize = new(2, 2);  // 좌표 스냅 단위
    [SerializeField] private List<DeckInfo> deckLists = new();  // 리스트 확인 용

    private Dictionary<Vector2Int, DeckInfo> decks = new();
    private readonly Vector2Int[] DIR4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Awake()
    {
        instance = this;
        deckLayer = LayerMask.GetMask("Platform"); // "Platform" 레이어 전용
        InitScan();
        UpdateConnectivity();
    }

    /// <summary>
    /// 특정 GameObject가 deckLayer에 속해 있는지 체크
    /// </summary>
    private bool IsInLayer(GameObject go)
    {
        return (deckLayer & (1 << go.layer)) != 0;
    }

    /// <summary>
    /// 씬 내 모든 Platform 레이어 갑판 자동 스캔
    /// </summary>
    private void InitScan()
    {
        decks.Clear();
        var all = FindObjectsOfType<Transform>(false);

        foreach (var d in all)
        {
            GameObject go = d.gameObject;
            if (!IsInLayer(go)) continue;

            var coord = WorldToCoord(go.transform.position);
            if (decks.ContainsKey(coord)) continue;

            decks.Add(coord, new DeckInfo { obj = go, isConnected = false });
        }
    }

    /// <summary>
    /// BFS로 돛대와 연결 여부를 계산 → decks[*].isConnected 갱신
    /// </summary>
    public void UpdateConnectivity()
    {
        // 1. 전체 초기화
        foreach (var kv in decks)
            kv.Value.isConnected = false;

        if (decks.Count == 0 || mast == null) return;

        // 2. 돛대와 가장 가까운 갑판 찾기
        if (!TryGetNearestDeckToMast(out var root)) return;

        // 3. BFS 탐색 시작!!!!!!!!!!!!!
        var visited = new HashSet<Vector2Int>();
        var q = new Queue<Vector2Int>();
        visited.Add(root);
        q.Enqueue(root);

        while (q.Count > 0)
        {
            Debug.Log("테스트용3333");
            var cur = q.Dequeue();
            if (!decks.ContainsKey(cur)) continue;

            decks[cur].isConnected = true;

            foreach (var d in DIR4)
            {
                Debug.Log("테스트용4444");
                var nxt = cur + d;
                if (visited.Contains(nxt)) continue;
                if (decks.ContainsKey(nxt))
                {
                    Debug.Log("테스트용5555");
                    visited.Add(nxt);
                    q.Enqueue(nxt);
                }
            }
        }

		RefreshDebugView();
	}

    /// <summary>
    /// 연결되지 않은 갑판(GameObject) 리스트 반환
    /// </summary>
    public List<GameObject> GetDisconnectedDecks()
    {
        var list = new List<GameObject>();
        foreach (var kv in decks)
            if (!kv.Value.isConnected)
                list.Add(kv.Value.obj);
        return list;
    }

	public void RefreshDebugView()
	{
		deckLists.Clear();
		foreach (var kv in decks)
		{
			deckLists.Add(new DeckInfo
			{
				coord = kv.Key,
				obj = kv.Value.obj,
				isConnected = kv.Value.isConnected
			});
		}
	}

	// -------------------- 내부 유틸 --------------------
	private bool TryGetNearestDeckToMast(out Vector2Int coord)
    {
		coord = default;
        float best = float.MaxValue;
        bool found = false;

        foreach (var kv in decks)
        {
            float d = (kv.Value.obj.transform.position - mast.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                coord = kv.Key;
                found = true;
            }
        }
        return found;
    }

    private Vector2Int WorldToCoord(Vector3 pos)
    {
        int cx = Mathf.RoundToInt(pos.x / Mathf.Max(0.0001f, gridSize.x));
        int cy = Mathf.RoundToInt(pos.z / Mathf.Max(0.0001f, gridSize.y));
        return new Vector2Int(cx, cy);
    }

	private void OnDrawGizmos()
	{
		if (decks == null) return;
		foreach (var kv in decks)
		{
			Gizmos.color = kv.Value.isConnected ? Color.green : Color.red;
			Gizmos.DrawSphere(kv.Value.obj.transform.position + Vector3.up * 0.5f, 0.2f);
		}
	}

}
