using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// 이녀석 백앤드임. 프론트앤드의 통제를 받음.
public class InventoryManager : InventoryBase
{
    public static InventoryManager Instance;

    public SItemStack mouseInventory;
    public int selectedSlotIndex;
    public SItemStack SelectedSlotInventory;
    [SerializeField] int inventoryCount;
    [SerializeField] Transform positionDrop;
    private Vector3 dropOffset = new Vector3(0.5f, -0.75f, -0.5f); // 오프셋
    private bool isThisFrameReloadCraft = false;

    [Header("디버깅")]
    [SerializeField] bool isDebugging;
    [SerializeField] bool isDebuggingAdd;
    bool IsDebuggingAdd => isDebugging && isDebuggingAdd;

    public new void MouseSwitch(int index)
    {
        if (mouseInventory != null && itemLists[index] != null &&
            mouseInventory.id == itemLists[index].id &&
            mouseInventory.itemBaseType.maxStack > 1)
        {
            int maxStack = mouseInventory.itemBaseType.maxStack;

            itemLists[index].amount += mouseInventory.amount;

            if (itemLists[index].amount > maxStack)
            {
                mouseInventory.amount = itemLists[index].amount - maxStack;
                itemLists[index].amount = maxStack;
            }
            else
            {
                mouseInventory = null;
            }
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
        mouseInventory = new SItemStack(itemLists[index].id, mMouseNum, itemLists[index].duability);

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

                mouseInventory.amount--;
            }
            else if (itemLists[index].amount < itemLists[index].itemBaseType.maxStack)
            {
                itemLists[index].amount++;

                mouseInventory.amount--;
            }

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
        isThisFrameReloadCraft = true;

        if (item.id == 40007)
        {
            RepairSystem.instance.remainRepairCount += item.amount;
            return;
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

    public bool ConsumeOne(SItemStack stack)
    {
        if (stack == null || stack.amount <= 0 || (!itemLists.Contains(stack) && mouseInventory != stack)) return false;
        stack.amount--;
        SafeClean();
        return true;
    }

    public void SelectSlot(int index)
    {
        SelectedSlotInventory = itemLists[index];
        selectedSlotIndex = index;
    }

    public void UpdateSlot() => SelectedSlotInventory = itemLists[selectedSlotIndex];

    public void ApplyItemDuablilityUsed()
    {
        if (SelectedSlotInventory == null) return;

        SItemWeaponTypeSO weapon = SelectedSlotInventory.itemBaseType as SItemWeaponTypeSO;
        if (weapon == null) return;

        SelectedSlotInventory.duability = Mathf.Max(0, SelectedSlotInventory.duability -
                    Mathf.Max(0, weapon.duabilityRedutionPerHit - PlayerCore.Instance.DuabilityReducePrevent));
        SafeClean();
        if (InventoryUiMain.instance != null)
        {
            InventoryUiMain.instance.IconRefresh();
        }
    }

	protected override void SafeClean()
    {
        base.SafeClean();

        if (mouseInventory != null && mouseInventory.amount < 1)
        {
            mouseInventory = null;
        }

        if (selectedSlotIndex >= 0 && selectedSlotIndex < itemLists.Count)
            UpdateSlot();
        else
            SelectedSlotInventory = null;
    }

    private void Awake()
    {
        Instance = this;

        itemLists = new List<SItemStack>();

        for (int i = 0; i < inventoryCount; ++i)
        {
            itemLists.Add(null);
        }

        mouseInventory = null;

    }

    private void Start()
    {
        InventoryUiMain.instance.IconRefresh();
    }

	private void Update()
	{
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad1))
        {
            Add(new SItemStack(20001, 1, 100));
            Add(new SItemStack(20002, 1, 100));
            Add(new SItemStack(20003, 1, 100));
            Add(new SItemStack(20003, 1, int.MaxValue));
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            Demo();
        }
#endif
    }

    private void LateUpdate()
    {
        if (isThisFrameReloadCraft)
        {
            UtilityFunctions.Log("아이템 획득 업데이트");

            isThisFrameReloadCraft = false;
            if (MakeshiftCraftUiMain.instance.isOpened)
            {
                UtilityFunctions.Log("아이템 획득 업데이트 완료");

                MakeshiftCraftUiMain.instance.UpdateRecipe();
            }
            CommonUI.instance.PickUpUpdate();
        }
    }

    private void Demo()
    {

        Add(new SItemStack(20001, 1, 100));
        Add(new SItemStack(20002, 1, 100));
        Add(new SItemStack(20003, 1, 100));
        Add(new SItemStack(30001, 80));
        Add(new SItemStack(30002, 80));
        Add(new SItemStack(30003, 80));
        Add(new SItemStack(30004, 80));
        Add(new SItemStack(30005, 80));
        Add(new SItemStack(30006, 80));
        Add(new SItemStack(30007, 80));
        Add(new SItemStack(30008, 80));
        Add(new SItemStack(30010, 80));
        Add(new SItemStack(30011, 80));
        Add(new SItemStack(30012, 80));
        Add(new SItemStack(30013, 80));
        Add(new SItemStack(30014, 80));
        Add(new SItemStack(30015, 80));
        Add(new SItemStack(30016, 80));
        Add(new SItemStack(30017, 80));
        Add(new SItemStack(30018, 80));
        Add(new SItemStack(30019, 80));
        Add(new SItemStack(30020, 80));
        Add(new SItemStack(30021, 80));
        Add(new SItemStack(30022, 80));

        Add(new SItemStack(40001, 80));
        Add(new SItemStack(40009, 80));
        Add(new SItemStack(40007, 80));
    }
}
