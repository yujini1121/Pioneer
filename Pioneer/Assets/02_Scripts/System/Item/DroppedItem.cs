using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public SItemStack itemValue;
    public Transform transformCanvas;
    public ItemSlotUI slotUI;

    public void SetItem(SItemStack item)
    {
        Debug.Log($">> DroppedItem.SetItem(SItemStack item) : »£√‚µ  / isItemNull : {item == null}");

        itemValue = item;
        slotUI.Show(item);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ThisIsPlayer.IsThisPlayer(collision))
        {
            InventoryManager.Instance.Add(itemValue);
            InventoryManager.Instance.UpdateSlot();
            PlayerStatUI.Instance.UpdateBasicStatUI();
			InventoryUiMain.instance.IconRefresh();

            Destroy(gameObject);
        }
    }
}
