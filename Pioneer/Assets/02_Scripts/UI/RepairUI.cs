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

    // Start is called before the first frame update
    void Start()
    {
        itemSlotUIs = new ItemSlotUI[slotGameObjects.Count];
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            itemSlotUIs[index] = slotGameObjects[index].GetComponent<ItemSlotUI>();
        }
        //IconRefresh();
        // repairWindow.SetActive(false);
        ClickClose();
    }

    // Update is called once per frame
    void Update()
    {
        remainRepairToolAmount.text = $"{RepairSystem.instance.remainRepairCount}";
    }

    public void ClickRepairButton()
    {
        RepairSystem.instance.ClickRepair();
        IconRefresh();
    }

    public void ClickClose()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void Open()
    {
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
            // slot == 0
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

    // 입력
    // 인벤토리 UI에서 마우스 다운 ->

    public void IconRefresh()
    {
        // 모든 아이템을
        // + 선택되지 않은 상태로 바꿈
        // + 내구도 체크
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            //if (InventoryManager.Instance.itemLists[index] == null) continue;

            ItemSlotUI _forUi = slotGameObjects[index].GetComponent<ItemSlotUI>();

            _forUi.Show(RepairSystem.instance.slot.itemLists[index]);
            _forUi.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        }
        InventoryUiMain.instance.MouseUI.Show(InventoryManager.Instance.mouseInventory);
        InventoryUiMain.instance.MouseUI.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        PlayerStatUI.Instance.UpdateBasicStatUI();

    }
}
