using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGetNoticeUI : MonoBehaviour
{
    public static ItemGetNoticeUI Instance;

    // 원소 4개짜리 리스트
    // 리스트 원소 => 나타나기 / 사라지기
    // 이미 꽉 참 -> 이전 원소 사라지기(필요한 만큼만) -> 쉬프트 -> 나타나기

    public GameObject prefab;

    public List<ItemGetNoticeSingleUI> uiList;

    public void Add(SItemStack item)
    {
        //if ()
    }

    private void Awake()
    {
        Instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
