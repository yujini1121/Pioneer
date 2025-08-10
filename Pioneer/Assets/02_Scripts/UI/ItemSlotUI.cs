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
    public bool isSlot;

    public void Show(SItemStack item,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {
        Debug.Log($">> ItemSlotUI.Show(SItemStack item) : È£ÃâµÊ");

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
        
        if (ItemTypeManager.Instance.itemTypeSearch[item.id].image != null)
        {
            image.enabled = true;
            image.sprite = ItemTypeManager.Instance.itemTypeSearch[item.id].image;
        }
        count.text = item.amount.ToString();
    }
    public void Clear()
    {
        image.enabled = false;
        count.text = "";
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
