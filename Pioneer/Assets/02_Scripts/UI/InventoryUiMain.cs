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
    public ItemSlotUI MouseUI => mouseUI;
    [SerializeField] List<GameObject> slotGameObjects;
    [SerializeField] List<GameObject> inventorySlot;
    [SerializeField] List<GameObject> quickSlot;
    [SerializeField] GameObject imageMouseHoldingItem; // 마우스
    [SerializeField] GameObject windowMouse; // 마우스
    [SerializeField] Canvas canvas;
    [SerializeField] TextMeshProUGUI windowMouseTextType;
    [SerializeField] TextMeshProUGUI windowMouseTextCategory;
    [SerializeField] TextMeshProUGUI windowMouseTextInfo;
    [SerializeField] Button trashButton;
    [SerializeField] Sprite trashOpen;
    [SerializeField] Sprite trashClose;
    RectTransform followUiRect1; // 마우스
    RectTransform followUiRect2; // 마우스
    ItemSlotUI[] itemSlotUIs;
    ItemSlotUI mCurrentSelectedHotbarSlot;

    public void InventoryExpand(bool value)
    {
        foreach (GameObject i in inventorySlot)
        {
            CanvasGroup cg = i.GetComponent<CanvasGroup>();
            cg.alpha = value ? 1f : 0f;
            cg.blocksRaycasts = value;
            cg.interactable = value;


            //i.SetActive(value);
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

        // windowMouseText.text = 
        (windowMouseTextType.text, windowMouseTextCategory.text, windowMouseTextInfo.text) = GetInfomation(mItemStack); // 마우스
    }

    public void ClickSlot(int index)
    {
        if (InGameUI.instance.IsPannelExpanded == false)
        {
            SelectSlot(index);
            IconRefresh();
            return;
        }    


        // 현재 크래프팅 중
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

        InventoryManager.Instance.UpdateSlot();

		IconRefresh();
		PlayerStatUI.Instance.UpdateBasicStatUI();
	}
    public void ClickOut()
    {
        // 마우스 아이탬 핸들
        // 플레이어 아이템 핸들

        if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false)
        {
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭 {InventoryManager.Instance.mouseInventory.id} / {InventoryManager.Instance.mouseInventory.amount}");
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭1");
            InventoryManager.Instance.MouseDrop();
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭2");
            mouseUI.Clear();
			//Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭3");
			InventoryUiMain.instance.IconRefresh();
			PlayerStatUI.Instance.UpdateBasicStatUI();
			return;
        }

        if(PlayerCore.Instance.currentState != PlayerCore.PlayerState.ActionFishing)
        {
            // 플레이어 아이템 핸들
            Debug.Log($">> InventoryUiMain.ClickOut() : 아이템이 비어 있습니다.");
            if (SItemStack.IsEmpty(InventoryManager.Instance.SelectedSlotInventory) ||
                InventoryManager.Instance.SelectedSlotInventory.itemBaseType.categories == EDataType.NormalItem)
            {
                // 빈 아이템 주먹 공격

                PlayerCore.Instance.BeginCoroutine(WeaponUseUtils.AttackCoroutine(
                    PlayerCore.Instance,
                    PlayerCore.Instance.dummyHandAttackItem,
                    PlayerCore.Instance.CalculatedHandAttack));
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

            InventoryUiMain.instance.IconRefresh();
            PlayerStatUI.Instance.UpdateBasicStatUI();

            //if (InventoryManager.Instance.SelectedSlotInventory != null)
            //{
            //    SItemTypeSO receved = ItemTypeManager.
            //                            Instance.
            //                            types[InventoryManager.Instance.SelectedSlotInventory.id];
            //    // 만약 무기다 && 내구도가 있다
            //    SItemWeaponTypeSO weaponObject = receved as SItemWeaponTypeSO;
            //    if (weaponObject != null && InventoryManager.Instance.SelectedSlotInventory.duability > 0)
            //    {
            //        PlayerCore.Instance.BeginCoroutine()
            //        PlayerCore.Instance.Attack(weaponObject);
            //        return;
            //    }
            //    // 소비형 아이템이다
            //    SItemConsumeTypeSO consumeObject = receved as SItemConsumeTypeSO;
            //}
            // 내구도가 만료된 무기 혹은 맨손
        }
    }

    public void Sort()
    {
        InventoryManager.Instance.SortSelf();

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.ArrayItem);

        IconRefresh();
    }
    public void Remove()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.RemoveItem);
        
        InventoryManager.Instance.RemoveMouseItem();
        mouseUI.Clear();
    }
    public void SelectSlot(int index)
    {
        Debug.Assert(index >= 0);
        Debug.Assert(index < slotGameObjects.Count, $"!!>> {index} / {slotGameObjects.Count}");

        InventoryManager.Instance.SelectSlot(index);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.SelectQuickSlot);

        mCurrentSelectedHotbarSlot = slotGameObjects[index].GetComponent<ItemSlotUI>();
        IconRefresh();
        PlayerStatUI.Instance.UpdateBasicStatUI();

        switch (InventoryManager.Instance.SelectedSlotInventory.id)
        {
            case 20001:
                Debug.Log($">> 선택된 슬롯 아이템 ID : 나무검");
                break;
            case 20002:
                Debug.Log($">> 선택된 슬롯 아이템 ID : 철 검");
                break;
            case 20003:
                Debug.Log($">> 선택된 슬롯 아이템 ID : 해신의 뿔피리");
                break;
            default:
                Debug.Log($">> 선택된 슬롯 아이템 ID : {InventoryManager.Instance.SelectedSlotInventory.id}");
                break;
        }

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

# warning 나중에 채빈씨 브랜치 머지 하고 업데이트 된경우 주석 풀기
        //if (hotkeyInventoryNum > -1 &&
        //    PlayerCore.Instance.currentState != PlayerCore.PlayerState.Default) SelectSlot(hotkeyInventoryNum);
        // ~~종료~~ 인벤토리 핫키 선택 시작
    }

    (string outTypeName, string outCategoriesName, string outInfomation) GetInfomation(SItemStack target)
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


        // return $"{info.typeName}\n{categoriesName}\n{info.infomation}";
        return (info.typeName, categoriesName, info.infomation);
    }

    public void IconRefresh()
    {
        // 모든 아이템을
        // + 선택되지 않은 상태로 바꿈
        // + 내구도 체크
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
            mCurrentSelectedHotbarSlot.image.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        PlayerStatUI.Instance.UpdateBasicStatUI();

	}
}
