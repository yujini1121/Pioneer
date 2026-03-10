using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class InventoryUiable : InventoryBase
{
    public SItemStack mouseInventory;
    public int selectedSlotIndex;
    public SItemStack SelectedSlotInventory;
    //public List<SItemStack> itemLists;
    //public Dictionary<int, SItemStack> fastSearch;
    [SerializeField] int inventoryCount;
    [SerializeField] Transform positionDrop;
    [SerializeField] Vector3 dropOffset = new Vector3(1, -0.8f, -1);
    [Header("DEBUG")]
    [SerializeField] bool isDebugging;
    [SerializeField] bool isDebuggingAdd;
    bool IsDebuggingAdd => isDebugging && isDebuggingAdd;

    public new void MouseSwitch(int index)
    {
        if (mouseInventory != null && itemLists[index] != null && mouseInventory.id == itemLists[index].id)
        {
            itemLists[index].amount += mouseInventory.amount;
            mouseInventory = null;
            SafeClean();
            return;
        }

        //Debug.Log("!!!");

        SItemStack temp = itemLists[index];

        itemLists[index] = mouseInventory;
        mouseInventory = temp;
    }

    public void MouseSplit(int index)
    {
        if (itemLists[index] == null)
        {
            return;
        }
        if (mouseInventory != null)
        {
            MouseSwitch(index);
            return;
        }

        int mSlotNum = itemLists[index].amount / 2;
        int mMouseNum = itemLists[index].amount - mSlotNum;

        itemLists[index].amount = mSlotNum;
        mouseInventory = new SItemStack(itemLists[index].id, mMouseNum);

        SafeClean();
    }

    public void MouseDrop()
    {
        Debug.Log($">> MouseDrop() : 호출됨");

        // ItemDropManager.instance.Drop(mouseInventory, positionDrop.position);
        ItemDropManager.instance.Drop(mouseInventory, ThisIsPlayer.Player.transform.position + dropOffset);

        mouseInventory = null;
    }

    public void RemoveMouseItem()
    {
        mouseInventory = null;
    }

    public void MouseSingle(int index)
    {
        // 마우스는 비어있고 인벤은 아이템이 있는것을 선택할 때
        // 마우스에 존재하고 인벤은 빈 공간을 선택할 때

        if (mouseInventory != null && itemLists[index] != null && (mouseInventory.id != itemLists[index].id))
        {
            return;
        }
        // 여러 개의 아이템이 마우스 위에 존재할 때 , ctrl를 누른 상태로 좌클릭 시 한 개 씩 그 칸에 놓아진다.
        else if (mouseInventory != null)
        {
            if (itemLists[index] == null)
            {
                itemLists[index] = new SItemStack(mouseInventory);
                itemLists[index].amount = 1;
            }
            else
            {
                itemLists[index].amount++;
            }
            mouseInventory.amount--;

        }
        else if (itemLists[index] != null)
        {
            mouseInventory = new SItemStack(itemLists[index]);
            mouseInventory.amount = 1;
            itemLists[index].amount--;
        }

        SafeClean();
    }

    public void Add(SItemStack item)
    {
        if (IsDebuggingAdd)
        {
            Debug.Log($">> Add(SItemStack item) => 아이템 추가됨 : {item.id}를 {item.amount}갯수만큼 추가");
        }

        Debug.Assert(InventoryUiMain.instance != null);
        SItemStack remain;// = null;
        if (TryAdd(item, out remain) == false)
        {
            ItemDropManager.instance.Drop(remain, positionDrop.transform.position);
        }
        else
        {
            ItemGetNoticeUI.Instance.Add(item);
        }

        InventoryUiMain.instance.IconRefresh();
    }

    public void SortSelf()
    {
        // 완전히 합침
        // 그뒤 아이템 추가
        for (int index = 0; index < inventoryCount; index++)
        {
            if (itemLists[index] == null) continue;

            for (int x = index + 1; x < inventoryCount; ++x)
            {
                if (itemLists[x] == null) continue;

                if (itemLists[index].id == itemLists[x].id)
                {
                    itemLists[index].amount += itemLists[x].amount;
                    itemLists[x] = null;
                }
            }
        }
        SafeClean();
        List<SItemStack> list = new List<SItemStack>();
        for (int index = 0; index < inventoryCount; index++)
        {
            if (itemLists[index] == null) continue;
            list.Add(itemLists[index]);
            itemLists[index] = null;
        }

        // 여기서부터 정렬

        list = list
            .OrderBy(w => ItemTypeManager.Instance.itemTypeSearch[w.id].categories)
            .ThenBy(w => ItemTypeManager.Instance.itemTypeSearch[w.id].typeName, StringComparer.Create(
            new CultureInfo("ko-KR"), ignoreCase: false)).ToList();
        for (int index = 0; index < list.Count; index++)
        {
            itemLists[index] = (list[index]);
        }
        SafeClean();
    }

    public void SelectSlot(int index)
    {
        SelectedSlotInventory = itemLists[index];
        selectedSlotIndex = index;
    }

    public void UpdateSlot() => SelectedSlotInventory = itemLists[selectedSlotIndex];



    public override void SafeClean()
    {
        base.SafeClean();

        if (mouseInventory != null && mouseInventory.amount < 1)
        {
            mouseInventory = null;
        }
    }

    private void Awake()
    {
        itemLists = new List<SItemStack>();
        //fastSearch = new Dictionary<int, SItemStack>();

        for (int i = 0; i < inventoryCount; ++i)
        {
            itemLists.Add(null);
            // Debug.Log($"awake : {itemLists[i].id}");
        }

        mouseInventory = null;

    }
}
