using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class InventoryBase : MonoBehaviour
{
    public List<SItemStack> itemLists;

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

    public bool TryAdd(SItemStack itemStack)
    {
        if (itemStack.amount < 1) return true;

        int firstEmpty = -1;

        for (int inventoryIndex = 0; inventoryIndex < itemLists.Count; ++inventoryIndex)
        {
            if (itemLists[inventoryIndex] == null)
            {
                if (firstEmpty == -1) firstEmpty = inventoryIndex;
                continue; // ���� ĭ�� ���� �������� ���� �� �־ break�� ���� ����.
            }

            if (itemLists[inventoryIndex].id == itemStack.id)
            {
                itemLists[inventoryIndex].amount += itemStack.amount;
                InventoryUiMain.instance.IconRefresh();
                return true;
            }
        }
        if (firstEmpty != -1)
        {
            itemLists[firstEmpty] = new SItemStack(itemStack.id, itemStack.amount);

            SItemWeaponTypeSO weaponSo = ItemTypeManager.Instance.itemTypeSearch[itemStack.id] as SItemWeaponTypeSO;
            if (weaponSo != null)
            {
                Debug.Log($">> InventoryBase.TryAdd : ���� �߰�. ������ = {weaponSo.weaponDuability}");
                itemLists[firstEmpty].duability = weaponSo.weaponDuability;
            }
            return true;
        }

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
