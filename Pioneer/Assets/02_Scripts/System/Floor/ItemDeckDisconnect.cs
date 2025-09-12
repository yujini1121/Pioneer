using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 갑판 개개별의 정보
/// </summary>
[System.Serializable]
class DeckInfo
{
	public Vector2Int coord;        // 좌표
	public GameObject obj;          // 각각의 게임 오브젝트
	public bool isConnected;        // 연결되어있는지의 여부
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

	[SerializeField] private Transform mast;                    // 돛대
	[SerializeField] private GameObject worldSpace;             // 갑판 부모 오브젝트
	[SerializeField] private LayerMask deckLayer;               // "Platform" 레이어
	[SerializeField] private Vector2 gridSize = new(1, 1);      // 좌표 스냅 단위
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

			decks.Add(coord, new DeckInfo { coord = coord, obj = go, isConnected = false });
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

		// 2. 돛대가 항상 정확히 중앙 갑판 위라는 전제 → mast.position을 스냅하여 루트 사용
		var root = WorldToCoord(mast.position);
		if (!decks.ContainsKey(root))
		{
			// 만약 여기서 없다고 나오면 gridSize/배치가 어긋난 것.
			Debug.Log($"[ItemDeckDisconnect] 루트 좌표 {root}에 갑판이 없습니다. gridSize/배치 확인 필요.");
			RefreshDebugView(); 
			return;
		}

		// 3. BFS 탐색 시작!@!!!!!!!!!!!!!!!!!!!!!
		var visited = new HashSet<Vector2Int>();
		var q = new Queue<Vector2Int>();
		visited.Add(root);
		q.Enqueue(root);

		while (q.Count > 0)
		{
			var cur = q.Dequeue();
			if (!decks.ContainsKey(cur)) continue;

			decks[cur].isConnected = true;

			foreach (var d in DIR4)
			{
				var next = cur + d;
				if (visited.Contains(next)) continue;
				if (decks.ContainsKey(next))
				{
					visited.Add(next);
					q.Enqueue(next);
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
			if (kv.Value?.obj == null) continue;
			Gizmos.color = kv.Value.isConnected ? Color.green : Color.red;
			Gizmos.DrawSphere(kv.Value.obj.transform.position + Vector3.up * 0.5f, 0.2f);
		}
	}
}
