using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 갑판 개개별의 정보
/// </summary>
[System.Serializable]
class DeckInfo
{
	public GameObject obj;
	public bool isConnected;	
	//public bool IsConnected { get; private set; }
}


/// <summary>
/// 갑판 파괴 : BFS로 구현할 것 + 아이템 파괴할 때마다 모든 바닥 검사
/// </summary>
public class ItemDeckDisconnect : MonoBehaviour
{
	public static ItemDeckDisconnect instance;

	[SerializeField] private Transform mast;
	[SerializeField] private GameObject worldSpace;
	[SerializeField] private LayerMask deckLayer;
	[SerializeField] private Vector2 gridSize = new(1, 1);

	private Dictionary<Vector2Int, DeckInfo> decks = new();
	private Vector2Int[] DIR4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

	private void Awake()
	{
		deckLayer = LayerMask.GetMask("Platform");
	}

	private bool IsInLayer(int layer)
	{
		return layer == deckLayer;
	}

	private void InitScan()
	{
		decks.Clear();
		var all = FindObjectsOfType<Transform>(false);

		foreach(var d in all)
		{
			GameObject go = d.gameObject;

			if (!IsInLayer(go.layer)) continue;

			var 
		}
	}

}
