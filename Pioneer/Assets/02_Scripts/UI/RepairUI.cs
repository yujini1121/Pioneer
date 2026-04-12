using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RepairUI : MonoBehaviour
{
    public static RepairUI instance;

    public CanvasGroup cg;
    public GameObject repairWindow;
    public TextMeshProUGUI remainRepairToolAmount;
    public List<GameObject> slotGameObjects;
    ItemSlotUI[] itemSlotUIs;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        itemSlotUIs = new ItemSlotUI[slotGameObjects.Count];
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            itemSlotUIs[index] = slotGameObjects[index].GetComponent<ItemSlotUI>();
        }
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    void Update()
    {
        if (remainRepairToolAmount == null || RepairSystem.instance == null)
            return;

        remainRepairToolAmount.text = $"{RepairSystem.instance.remainRepairCount}";
    }

    public void ClickRepairButton()
    {
        RepairSystem.instance.ClickRepair();
        IconRefresh();
    }

    public void ClickClose()
    {
        InGameUI.instance.CloseUI(InGameUI.ID_REPAIR_ITEM);
    }

    public void Open()
    {
        InGameUI.instance.OpenUI(new List<GameObject>() { }, InGameUI.ID_REPAIR_ITEM,
            () =>
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            );
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        IconRefresh();
    }

    public void ClickSlot(int index)
    {
        if (index == 1)
        {
            if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false)
            {
                return;
            }
        }
        else
        {
            if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false &&
                InventoryManager.Instance.mouseInventory.itemBaseType.categories != EDataType.WeaponItem)
            {
                return;
            }
        }

        if (index == 0 &&
            SItemStack.IsEmpty(RepairSystem.instance.slot.itemLists[1]) == false &&
            SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false &&
            InventoryManager.Instance.mouseInventory.itemBaseType.categories == EDataType.WeaponItem)
        {
            RepairSystem.instance.Collect();
        }
        RepairSystem.instance.slot.MouseSwitch(index);

        InventoryUiMain.instance.MouseUI.Show(InventoryManager.Instance.mouseInventory);
        itemSlotUIs[index].Show(RepairSystem.instance.slot.itemLists[index]);

        IconRefresh();
        PlayerStatUI.Instance.UpdateBasicStatUI();

        if (SItemStack.IsEmpty(RepairSystem.instance.slot.itemLists[0]) == false &&
            RepairSystem.instance.slot.itemLists[0].itemBaseType.categories == EDataType.WeaponItem &&
            RepairSystem.instance.slot.itemLists[0].duability < 51)
        {
            itemSlotUIs[1].image.enabled = true;
            itemSlotUIs[1].image.sprite = RepairSystem.instance.slot.itemLists[0].itemBaseType.image;
            Color c = itemSlotUIs[1].image.color;
            c.a = 0.5f;
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;
            itemSlotUIs[1].image.color = c;
            itemSlotUIs[1].durability.text = $"{RepairSystem.instance.slot.itemLists[0].duability + 50}%";
        }
        else
        {
            itemSlotUIs[1].Show(RepairSystem.instance.slot.itemLists[1]);
        }

    }

    public void IconRefresh()
    {
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            ItemSlotUI _forUi = slotGameObjects[index].GetComponent<ItemSlotUI>();

            _forUi.Show(RepairSystem.instance.slot.itemLists[index]);
            _forUi.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        }
        InventoryUiMain.instance.MouseUI.Show(InventoryManager.Instance.mouseInventory);
        InventoryUiMain.instance.MouseUI.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        PlayerStatUI.Instance.UpdateBasicStatUI();

    }
}
