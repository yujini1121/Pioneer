using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class InventoryUiMain : MonoBehaviour, IBegin
{
    static public InventoryUiMain instance;
    
    public List<ItemSlotUI> currentSelectedSlot;
    [SerializeField] ItemSlotUI mouseUI;
    [SerializeField] List<GameObject> slotGameObjects;
    [SerializeField] List<GameObject> inventorySlot;
    [SerializeField] List<GameObject> quickSlot;
    [SerializeField] GameObject imageMouseHoldingItem;
    [SerializeField] GameObject windowMouse;
    [SerializeField] Canvas canvas;
    [SerializeField] TextMeshProUGUI windowMouseText;
    [SerializeField] Button trashButton;
    [SerializeField] Sprite trashOpen;
    [SerializeField] Sprite trashClose;
    RectTransform followUiRect1;
    RectTransform followUiRect2;
    ItemSlotUI[] itemSlotUIs;
    ItemSlotUI mCurrentSelectedHotbarSlot;

    public void InventoryExpand(bool value)
    {
        foreach (GameObject i in inventorySlot)
        {
            i.SetActive(value);
        }
    }

    public void HideWindow()
    {
        windowMouse.SetActive(false);
    }

    public void ShowWindow()
    {
        if (currentSelectedSlot.Count == 0)
        {
            windowMouse.SetActive(false);
            return;
        }

        windowMouse.SetActive(true);
        SItemStack mItemStack = InventoryManager.Instance.itemLists[currentSelectedSlot[0].index];

        if (mItemStack == null || mItemStack.id == 0)
        {
            windowMouse.SetActive(false);
            return;
        }

        //Debug.Log($">> ������ ���� : {currentSelectedSlot[0].index} / {mItemStack.id} {mItemStack.amount}");

        windowMouseText.text = GetInfomation(mItemStack);
    }

    public void ClickSlot(int index)
    {
        if (CommonUI.instance.IsCurrentCrafting && InGameUI.instance.currentFabricationUi != null)
        {
            CommonUI.instance.StopCraft(InGameUI.instance.currentFabricationUi);
        }


        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            InventoryManager.Instance.MouseSplit(index);
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            InventoryManager.Instance.MouseSingle(index);
        }
        else
        {
            InventoryManager.Instance.MouseSwitch(index);
        }

        mouseUI.Show(InventoryManager.Instance.mouseInventory);
        itemSlotUIs[index].Show(InventoryManager.Instance.itemLists[index]);
    }
    public void ClickOut()
    {
        // ���콺 ������ �ڵ�
        // �÷��̾� ������ �ڵ�

        if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false)
        {
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : ������ ��� {InventoryManager.Instance.mouseInventory.id} / {InventoryManager.Instance.mouseInventory.amount}");
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : ������ ���1");
            InventoryManager.Instance.MouseDrop();
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : ������ ���2");
            mouseUI.Clear();
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : ������ ���3");

            return;
        }

        // �÷��̾� ������ �ڵ�
        Debug.Log($">> InventoryUiMain.ClickOut() : �������� ��� �ֽ��ϴ�.");
        if (SItemStack.IsEmpty(InventoryManager.Instance.SelectedSlotInventory))
        {

        }
        else
        {
            PlayerCore.Instance.BeginCoroutine(
                ItemTypeManager.Instance.itemTypeSearch[
                    InventoryManager.Instance.SelectedSlotInventory.id].Use(
                            PlayerCore.Instance,
                            InventoryManager.Instance.SelectedSlotInventory
                        )
                );
        }


        //if (InventoryManager.Instance.SelectedSlotInventory != null)
        //{
        //    SItemTypeSO receved = ItemTypeManager.
        //                            Instance.
        //                            types[InventoryManager.Instance.SelectedSlotInventory.id];
        //    // ���� ����� && �������� �ִ�
        //    SItemWeaponTypeSO weaponObject = receved as SItemWeaponTypeSO;
        //    if (weaponObject != null && InventoryManager.Instance.SelectedSlotInventory.duability > 0)
        //    {
        //        PlayerCore.Instance.BeginCoroutine()
        //        PlayerCore.Instance.Attack(weaponObject);
        //        return;
        //    }
        //    // �Һ��� �������̴�
        //    SItemConsumeTypeSO consumeObject = receved as SItemConsumeTypeSO;
        //}
        // �������� ����� ���� Ȥ�� �Ǽ�

    }

    public void Sort()
    {
        InventoryManager.Instance.SortSelf();
        IconRefresh();
    }
    public void Remove()
    {
        InventoryManager.Instance.RemoveMouseItem();
        mouseUI.Clear();
    }
    public void SelectSlot(int index)
    {
        Debug.Assert(index >= 0);
        Debug.Assert(index < slotGameObjects.Count, $"!!>> {index} / {slotGameObjects.Count}");

        InventoryManager.Instance.SelectSlot(index);

        mCurrentSelectedHotbarSlot = slotGameObjects[index].GetComponent<ItemSlotUI>();
        IconRefresh();
    }

    private void Awake()
    {
        instance = this;

        itemSlotUIs = new ItemSlotUI[slotGameObjects.Count];
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            itemSlotUIs[index] = slotGameObjects[index].GetComponent<ItemSlotUI>();
        }
        currentSelectedSlot = new List<ItemSlotUI>();
    }

    // Start is called before the first frame update
    public void Start()
    //void Start()
    {
        followUiRect1 = imageMouseHoldingItem.GetComponent<RectTransform>();
        followUiRect2 = windowMouse.GetComponent<RectTransform>();

        IconRefresh();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mMousePos
        );

        followUiRect1.anchoredPosition = mMousePos;
        followUiRect2.anchoredPosition = mMousePos + new Vector2(50, 50);

        ShowWindow();

        // �κ��丮 ��Ű ���� ����
        int hotkeyInventoryNum = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) hotkeyInventoryNum = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) hotkeyInventoryNum = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) hotkeyInventoryNum = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) hotkeyInventoryNum = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) hotkeyInventoryNum = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) hotkeyInventoryNum = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) hotkeyInventoryNum = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) hotkeyInventoryNum = 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) hotkeyInventoryNum = 8;
        if (hotkeyInventoryNum > -1) SelectSlot(hotkeyInventoryNum);

# warning ���߿� ä�� �귣ġ ���� �ϰ� ������Ʈ �Ȱ�� �ּ� Ǯ��
        //if (hotkeyInventoryNum > -1 &&
        //    PlayerCore.Instance.currentState != PlayerCore.PlayerState.Default) SelectSlot(hotkeyInventoryNum);
        // ~~����~~ �κ��丮 ��Ű ���� ����
    }

    string GetInfomation(SItemStack target)
    {
        SItemTypeSO info = ItemTypeManager.Instance.itemTypeSearch[target.id];

        string categoriesName = "";
        switch (info.categories)
        {
            case EDataType.CommonResource: categoriesName = "���� �ڿ�"; break;
            case EDataType.WeaponItem: categoriesName = "���� ������"; break;
            case EDataType.NormalItem: categoriesName = "�Ϲ� ������"; break;
            case EDataType.ConsumeItem: categoriesName = "�Ҹ� ������"; break;
            case EDataType.BuildObject: categoriesName = "��ġ�� ������Ʈ"; break;
            case EDataType.Recipe: categoriesName = "���� ������"; break;
            case EDataType.Unit: categoriesName = "����"; break;
            default: break;
        }

        return $"{info.typeName}\n{categoriesName}\n{info.infomation}";
    }

    public void IconRefresh()
    {
        // ��� ��������
        // + ���õ��� ���� ���·� �ٲ�
        // + ������ üũ
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            //if (InventoryManager.Instance.itemLists[index] == null) continue;

            ItemSlotUI _forUi = slotGameObjects[index].GetComponent<ItemSlotUI>();

            _forUi.Show(InventoryManager.Instance.itemLists[index]);
            _forUi.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        }
        mouseUI.Show(InventoryManager.Instance.mouseInventory);
        mouseUI.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);


        if (mCurrentSelectedHotbarSlot != null)
        {
            mCurrentSelectedHotbarSlot.image.gameObject.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        }
    }
}
