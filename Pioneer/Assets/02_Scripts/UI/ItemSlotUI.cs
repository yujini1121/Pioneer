using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int index;
    public UnityEngine.UI.Image image;
    public TextMeshProUGUI hotKey;
    public TextMeshProUGUI count;
    public TextMeshProUGUI durability;
    public bool isSlot;

    public void Show(SItemStack item,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {
        Debug.Log($">> ItemSlotUI.Show(SItemStack item) : »£√‚µ ");

        if (item == null || item.id == 0)
        {
            Clear();
            return;
        }


        Debug.Assert(item != null);
        Debug.Assert(item.id != 0);
        Debug.Assert(ItemTypeManager.Instance != null);
        Debug.Assert(ItemTypeManager.Instance.itemTypeSearch != null);
        Debug.Assert(ItemTypeManager.Instance.itemTypeSearch[item.id] != null);
        Debug.Log($">> ItemSlotUI.Show(SItemStack item) : {item.id} / {item.amount}");

        SItemTypeSO itemType = ItemTypeManager.Instance.itemTypeSearch[item.id];
        bool isNeedShowDuability = itemType.categories == EDataType.WeaponItem;

        if (ItemTypeManager.Instance.itemTypeSearch[item.id].image != null)
        {
            image.enabled = true;
            image.sprite = ItemTypeManager.Instance.itemTypeSearch[item.id].image;
        }
        count.text = item.amount.ToString();

        if (isNeedShowDuability)
        {
            durability.text = $"{InventoryManager.Instance.itemLists[index].duability}%";
            image.color = (InventoryManager.Instance.itemLists[index].duability > 0) ? Color.white : Color.red;
        }
        else
        {
            durability.text = "";
            image.color = Color.white;
        }
    }
    public void Clear()
    {
        Debug.Log($">> {gameObject.name} -> ItemSlotUI.Clear() : »£√‚µ ");
        
        image.enabled = false;
        count.text = "";
        durability.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSlot) InventoryUiMain.instance.currentSelectedSlot.Add(this);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSlot) InventoryUiMain.instance.currentSelectedSlot.Remove(this);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSlot)
        {
            InventoryUiMain.instance.ClickSlot(index);
        }
    }
}
