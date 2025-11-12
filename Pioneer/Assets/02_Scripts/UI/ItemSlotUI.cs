using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class ItemSlotUI : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    const bool IS_DEBUG_LOG = false;


    public int index;
    public UnityEngine.UI.Image image;
    public TextMeshProUGUI hotKey;
    public TextMeshProUGUI count;
    public TextMeshProUGUI durability;
    public GameObject SelectImage;
    public bool isSlot;
    public bool isRepairSlot;
    public List<System.Action> buttonClickAction;


    private void Awake()
    {
        buttonClickAction = new List<System.Action>();
    }

    private void Start()
    {
        if (SelectImage != null) SelectImage.SetActive(false);
    }

    public void Show(SItemStack item,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {
        if (IS_DEBUG_LOG) Debug.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : »£√‚µ ");

        if (item == null || item.id == 0)
        {
            Clear();
            return;
        }
        if (IS_DEBUG_LOG) Debug.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : ≥ª±∏µµ = {item.duability}");


        Debug.Assert(item != null);
        Debug.Assert(item.id != 0);
        Debug.Assert(ItemTypeManager.Instance != null);
        Debug.Assert(ItemTypeManager.Instance.itemTypeSearch != null);
        Debug.Assert(ItemTypeManager.Instance.itemTypeSearch[item.id] != null);
        if (IS_DEBUG_LOG) 
            Debug.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : {item.id} / {item.amount}");

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
            durability.text = $"{item.duability}%";
            image.color = (item.duability > 0) ? Color.white : Color.red;
        }
        else
        {
            durability.text = "";
            image.color = Color.white;
        }
    }
    public void Clear()
    {
        //Debug.Log($">> {gameObject.name} -> ItemSlotUI.Clear() : »£√‚µ ");
        
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
        else if (isRepairSlot)
        {
            RepairUI.instance.ClickSlot(index);
        }

        foreach (var one in buttonClickAction)
        {
            one();
        }
    }
}
