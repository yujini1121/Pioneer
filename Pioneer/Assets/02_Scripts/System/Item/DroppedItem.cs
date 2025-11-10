using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public bool isCanPickUp = false;
    public SItemStack itemValue;
    public Transform transformCanvas;
    public ItemSlotUI slotUI;
    float pickUpTime = 0f;

    public void SetItem(SItemStack item, float pickUpTime)
    {
        Debug.Log($">> DroppedItem.SetItem(SItemStack item) : »£√‚µ  / isItemNull : {item == null}");

        itemValue = item;
        slotUI.Show(item);
        this.pickUpTime = pickUpTime;
        IEnumerator EnablePickUpAfterTime()
        {
            yield return new WaitForSeconds(pickUpTime);
            isCanPickUp = true;
        }
        StartCoroutine(EnablePickUpAfterTime());
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (ThisIsPlayer.IsThisPlayer(collision) && isCanPickUp)
        {
            InventoryManager.Instance.Add(itemValue);
            InventoryManager.Instance.UpdateSlot();
            PlayerStatUI.Instance.UpdateBasicStatUI();
			InventoryUiMain.instance.IconRefresh();

            Destroy(gameObject);
        }
    }
}
