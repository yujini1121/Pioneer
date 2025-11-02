using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ItemGetNoticeUI : MonoBehaviour
{
    public static ItemGetNoticeUI Instance;

    // 원소 4개짜리 리스트
    // 리스트 원소 => 나타나기 / 사라지기
    // 이미 꽉 참 -> 이전 원소 사라지기(필요한 만큼만) -> 쉬프트 -> 나타나기

    public GameObject prefab;

    public List<ItemGetNoticeSingleUI> uiList;
    public GameObject[] objectPool;
    private bool[] isUsing = new bool[4] { false, false, false, false };

    public void Add(SItemStack item)
    {
        Debug.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 시작 {item.id}");


        //if ()


        GameObject newUi = null;
        for (int forIndex = 0; forIndex < objectPool.Length; forIndex++)
        {
            if (!isUsing[forIndex])
            {
                isUsing[forIndex] = true;
                objectPool[forIndex].SetActive(true);
                newUi = objectPool[forIndex];
                break;
            }
        }
        Debug.Assert(newUi != null);
        newUi.transform.localPosition = new Vector3(0, 100, 0);
        

        Debug.Assert(newUi != null, "!! ItemGetNoticeUI: Object Pool is full!");

        ItemGetNoticeSingleUI newUiScript = newUi.GetComponent<ItemGetNoticeSingleUI>();
        newUiScript.Show(item);
        newUiScript.Begin();
        uiList.Insert(0, newUiScript);
        
        for (int uiListIndex = uiList.Count - 1; uiListIndex > 0; --uiListIndex)
        {
            // 만약 4번째(인덱스3)의 대상은 치워버림
            // 그 미만의 대상들은 아래로 이동
            ItemGetNoticeSingleUI one = uiList[uiListIndex];


            if (uiListIndex == 3)
            {
                //isUsing[one.index] = false;
                //objectPool[one.index].SetActive(false);
                //uiList.RemoveAt(3);
                RemoveUI(one.index, uiList[3]);
                continue;
            }
            objectPool[one.index].transform.localPosition += new Vector3(0, -100, 0);
            Debug.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 중간 - {objectPool[one.index].transform.localPosition}");
        }

        Debug.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 종료 {item.id}");

    }

    public void RemoveUI(int index, ItemGetNoticeSingleUI script)
    {
        isUsing[index] = false;
        objectPool[index].SetActive(false);
        uiList.Remove(script);
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

    GameObject GetUiObjectIndex()
    {
        for (int index = 0; index < objectPool.Length; index++)
        {
            if (!isUsing[index])
            {
                isUsing[index] = true;
                return objectPool[index];
            }
        }
        return null;
    }

}
