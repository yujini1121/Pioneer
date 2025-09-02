using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDeckDisconnect : MonoBehaviour
{
	public static ItemDeckDisconnect instance;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);  // 중복 방지
			return;
		}
		instance = this;
	}

	// 갑판 파괴 : BFS로 구현할 것 + 아이템 파괴할 때마다 모든 바닥 검사
	public void DestroyItemDeck()
    {
		  
    }
}
