using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
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

        //Debug.Log($">> 아이템 스택 : {currentSelectedSlot[0].index} / {mItemStack.id} {mItemStack.amount}");

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
        if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory))
        {
            Debug.Log($">> InventoryUiMain.ClickOut() : 아이템이 비어 있습니다.");
            return;
        }

        //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭 {InventoryManager.Instance.mouseInventory.id} / {InventoryManager.Instance.mouseInventory.amount}");
        //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭1");
        InventoryManager.Instance.MouseDrop();
        //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭2");
        mouseUI.Clear();
        //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭3");
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

        // 인벤토리 핫키 선택 시작
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
        // ~~종료~~ 인벤토리 핫키 선택 시작
    }

    string GetInfomation(SItemStack target)
    {
        SItemTypeSO info = ItemTypeManager.Instance.itemTypeSearch[target.id];

        string categoriesName = "";
        switch (info.categories)
        {
            case EDataType.CommonResource: categoriesName = "공통 자원"; break;
            case EDataType.WeaponItem: categoriesName = "무기 아이템"; break;
            case EDataType.NormalItem: categoriesName = "일반 아이템"; break;
            case EDataType.ConsumeItem: categoriesName = "소모 아이템"; break;
            case EDataType.BuildObject: categoriesName = "설치형 오브젝트"; break;
            case EDataType.Recipe: categoriesName = "제작 레시피"; break;
            case EDataType.Unit: categoriesName = "유닛"; break;
            default: break;
        }

        return $"{info.typeName}\n{categoriesName}\n{info.infomation}";
    }

    public void IconRefresh()
    {
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
