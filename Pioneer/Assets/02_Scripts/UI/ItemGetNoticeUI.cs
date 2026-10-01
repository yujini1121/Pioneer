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
        if (SItemStack.IsEmpty(item) || item.itemBaseType == null
            || objectPool == null || objectPool.Length == 0) return;
        UtilityFunctions.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 시작 {item.id}");


        //if ()


        if (uiList.Count >= objectPool.Length)
        {
            ItemGetNoticeSingleUI oldest = uiList[uiList.Count - 1];
            RemoveUI(oldest.index, oldest);
        }

        GameObject newUi = null;
        int poolIndex = -1;
        for (int forIndex = 0; forIndex < objectPool.Length; forIndex++)
        {
            if (!isUsing[forIndex] && objectPool[forIndex] != null
                && objectPool[forIndex].GetComponent<ItemGetNoticeSingleUI>() != null)
            {
                isUsing[forIndex] = true;
                objectPool[forIndex].SetActive(true);
                newUi = objectPool[forIndex];
                poolIndex = forIndex;
                break;
            }
        }
        UtilityFunctions.Assert(newUi != null);
        if (newUi == null) return;
        newUi.transform.localPosition = new Vector3(0, 75, 0);
        

        UtilityFunctions.Assert(newUi != null, "!! ItemGetNoticeUI: Object Pool is full!");

        ItemGetNoticeSingleUI newUiScript = newUi.GetComponent<ItemGetNoticeSingleUI>();
        newUiScript.index = poolIndex;
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
            one.MoveToLocalY(75 - (72 * uiListIndex));
            UtilityFunctions.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 중간 - {objectPool[one.index].transform.localPosition}");
        }

        UtilityFunctions.Log($">> ItemGetNoticeUI.Add(SItemStack item) : 종료 {item.id}");

    }

    public void RemoveUI(int index, ItemGetNoticeSingleUI script)
    {
        if (objectPool == null || index < 0 || index >= objectPool.Length) return;
        isUsing[index] = false;
        if (objectPool[index] != null) objectPool[index].SetActive(false);
        uiList.Remove(script);
    }

    private void Awake()
    {
        Instance = this;
        if (uiList == null) uiList = new List<ItemGetNoticeSingleUI>();
        isUsing = new bool[objectPool != null ? objectPool.Length : 0];
    }


    // Start is called before the first frame update


    // Update is called once per frame


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
