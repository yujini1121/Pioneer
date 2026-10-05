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
    [SerializeField] int inventoryCount;
    [SerializeField] Transform positionDrop;
    [SerializeField] Vector3 dropOffset = new Vector3(1, -0.8f, -1);
    [Header("디버깅")]
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
        }

        UtilityFunctions.Assert(InventoryUiMain.instance != null);
        SItemStack remain;
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
        var ui = InventoryUiMain.instance;
        if (ui == null) return;
        InventorySort.Sort(itemLists, ui.RegularSlotIndices);
        UpdateSlot();
    }

    public void SelectSlot(int index)
    {
        SelectedSlotInventory = itemLists[index];
        selectedSlotIndex = index;
    }

    public void UpdateSlot() => SelectedSlotInventory = itemLists[selectedSlotIndex];



    protected override void SafeClean()
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

        for (int i = 0; i < inventoryCount; ++i)
        {
            itemLists.Add(null);
        }

        mouseInventory = null;

    }
}
