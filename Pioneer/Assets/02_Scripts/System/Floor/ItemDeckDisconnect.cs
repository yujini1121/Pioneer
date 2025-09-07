using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 갑판 파괴 : BFS로 구현할 것 + 아이템 파괴할 때마다 모든 바닥 검사
public class ItemDeckDisconnect : MonoBehaviour
{
	public static ItemDeckDisconnect instance;
	public bool IsConnected { get; private set; }

	[SerializeField] private GameObject mast;
	[SerializeField] private GameObject worldSpace;

    private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);  // 중복 방지
			return;
		}
		instance = this;
	}


	public void ScanConnected()
	{

	}

	public void DestroyDeck()
    {
		  
    }
}
