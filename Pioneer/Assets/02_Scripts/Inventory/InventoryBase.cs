using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryBase : MonoBehaviour
{
    public List<SItemStack> itemLists;

    public void MouseSwitch(int index)
    {
        if (InventoryManager.Instance.mouseInventory != null &&
            itemLists[index] != null &&
            InventoryManager.Instance.mouseInventory.id == itemLists[index].id)
        {
            itemLists[index].amount += InventoryManager.Instance.mouseInventory.amount;
            InventoryManager.Instance.mouseInventory = null;
            SafeClean();
            return;
        }

        //Debug.Log("!!!");

        SItemStack temp = itemLists[index];
        itemLists[index] = InventoryManager.Instance.mouseInventory;
        InventoryManager.Instance.mouseInventory = temp;
    }

    public int Get(int id)
    {
        int sum = 0;
        for (int index = 0; index < itemLists.Count; ++index)
        {
            if (itemLists[index] != null &&
                itemLists[index].id == id)
            {
                sum += itemLists[index].amount;
            }
        }

        return sum;
    }

    public int GetAllItem()
    {
        int sum = 0;
        for (int index = 0; index < itemLists.Count; ++index)
        {
            if (itemLists[index] != null)
            {
                sum += itemLists[index].amount;
            }
        }
        return sum;
    }

    public bool TryAdd(SItemStack itemStack, out SItemStack remainOrNull)
    {
        remainOrNull = null;
        if (itemStack.amount < 1) return true;

        int amount = itemStack.amount;
        int maxStack = ItemTypeManager.Instance.itemTypeSearch[itemStack.id].maxStack;

        for (int inventoryIndex = 0; inventoryIndex < itemLists.Count; ++inventoryIndex)
        {
            if (SItemStack.IsEmpty(itemLists[inventoryIndex])) continue;// 빈 슬롯
            if (itemLists[inventoryIndex].id == itemStack.id && maxStack > 1) // 같은 아이템 슬롯 && 스택 가능함.
            {
                itemLists[inventoryIndex].amount += amount;
                if (itemLists[inventoryIndex].amount <= maxStack) // amount는 사실상 이제 0이 됨
                {
                    return true;
                }
                amount = itemLists[inventoryIndex].amount - maxStack; // 사실상 감소함.
                itemLists[inventoryIndex].amount = maxStack;
                continue;
            }
        }

        for (int inventoryIndex = 0; inventoryIndex < itemLists.Count; ++inventoryIndex)
        {
            if (SItemStack.IsEmpty(itemLists[inventoryIndex]))
            {
                itemLists[inventoryIndex] = new SItemStack(itemStack.id, amount, itemStack.duability);
                if (itemLists[inventoryIndex].amount <= maxStack) // amount는 사실상 이제 0이 됨
                {
                    return true;
                }
                amount = itemLists[inventoryIndex].amount - maxStack; // 사실상 감소함.
                itemLists[inventoryIndex].amount = maxStack;
            }
        }

        remainOrNull = itemStack.Copy();
        remainOrNull.amount = amount;

        Debug.Log($">> InventoryBase.TryAdd : itemLists.Count = {itemLists.Count} / adding : {itemStack.id} + {itemStack.amount}");
        return false;
    }

    public void Remove(params SItemStack[] removeTargets)
    {
        for (int targetIndex = 0; targetIndex < removeTargets.Length; targetIndex++)
        {
            int targetAmount = removeTargets[targetIndex].amount;

            for (int inventoryIndex = itemLists.Count - 1; inventoryIndex >= 0; --inventoryIndex)
            {
                if (itemLists[inventoryIndex] == null) continue;

                if (removeTargets[targetIndex].id == itemLists[inventoryIndex].id)
                {
                    if (targetAmount > itemLists[inventoryIndex].amount)
                    {
                        targetAmount -= itemLists[inventoryIndex].amount;
                        itemLists[inventoryIndex] = null;
                        continue;
                    }
                    itemLists[inventoryIndex].amount -= targetAmount;
                    break;
                }
            }
        }
        SafeClean();
    }

    protected virtual void SafeClean()
    {
        for (int index = 0; index < itemLists.Count; ++index)
        {
            if (itemLists[index] == null)
            {
                continue;
            }
            if (itemLists[index].amount < 1)
            {
                itemLists[index] = null;
            }
        }
    }
}
