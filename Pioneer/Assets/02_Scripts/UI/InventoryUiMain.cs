using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
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
    [SerializeField] float clickTerm = 1.0f;
    RectTransform followUiRect1; // 마우스
    RectTransform followUiRect2; // 마우스
    ItemSlotUI[] itemSlotUIs;
    ItemSlotUI mCurrentSelectedHotbarSlot;
    float clickTime = 0.0f;
    int clickedSlotIndex = -1;
    Coroutine clickCoroutine = null;
    private string lastDescription;
    private bool inventoryVisibilityInitialized;
    private bool inventoryVisible;
    private readonly Vector3[] tooltipCorners = new Vector3[4];

    private readonly List<int> regularSlotIndices = new List<int>();
    public IReadOnlyList<int> RegularSlotIndices => regularSlotIndices;

    public void InventoryExpand(bool value)
    {
        if (inventoryVisibilityInitialized && inventoryVisible == value) return;
        foreach (GameObject i in inventorySlot)
        {
            if (!inventoryVisibilityInitialized && !value)
            {
                var cg = UITweenHelper.EnsureCanvasGroup(i);
                cg.alpha = 0f;
                cg.interactable = cg.blocksRaycasts = false;
            }
            else if (value) UITweenHelper.PlayOpen(i);
            else UITweenHelper.PlayClose(i, null);
        }
        inventoryVisible = value;
        inventoryVisibilityInitialized = true;
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


        (windowMouseTextType.text, windowMouseTextCategory.text, windowMouseTextInfo.text) = GetInfomation(mItemStack); // 마우스
        LayoutDescription();
        ClampDescriptionToCanvas();
    }

    private void ClampDescriptionToCanvas()
    {
        var outer = windowMouse.transform.Find("BackGround1") as RectTransform;
        var canvasRect = canvas.transform as RectTransform;
        if (outer == null || canvasRect == null) return;
        outer.GetWorldCorners(tooltipCorners);
        Vector3 min = canvasRect.InverseTransformPoint(tooltipCorners[0]);
        Vector3 max = canvasRect.InverseTransformPoint(tooltipCorners[2]);
        Rect bounds = canvasRect.rect;
        const float margin = 12f;
        Vector3 offset = Vector3.zero;
        offset.x = Mathf.Max(0f, bounds.xMin + margin - min.x) - Mathf.Max(0f, max.x - bounds.xMax + margin);
        offset.y = Mathf.Max(0f, bounds.yMin + margin - min.y) - Mathf.Max(0f, max.y - bounds.yMax + margin);
        followUiRect2.position += canvasRect.TransformVector(offset);
    }

    private void LayoutDescription()
    {
        if (lastDescription == windowMouseTextInfo.text) return;
        lastDescription = windowMouseTextInfo.text;
        windowMouseTextInfo.richText = true;
        windowMouseTextInfo.enableAutoSizing = false;
        windowMouseTextInfo.fontSize = 20f;
        windowMouseTextInfo.alignment = TextAlignmentOptions.TopLeft;
        var rect = windowMouseTextInfo.rectTransform;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, 22f);
        float height = windowMouseTextInfo.GetPreferredValues(lastDescription, 210f, Mathf.Infinity).y;
        rect.sizeDelta = new Vector2(210f, height);
        var outer = windowMouse.transform.Find("BackGround1") as RectTransform;
        var body = windowMouse.transform.Find("BackGround3") as RectTransform;
        if (outer != null)
        {
            outer.sizeDelta = new Vector2(235f, height + 130f);
            outer.anchoredPosition = new Vector2(0f, 130f - outer.sizeDelta.y * 0.5f);
        }
        if (body != null)
        {
            body.sizeDelta = new Vector2(220f, height + 20f);
            body.anchoredPosition = new Vector2(0f, 30f - body.sizeDelta.y * 0.5f);
        }
    }

    public void RightClickSlot(int index)
    {
        if (InventoryManager.Instance.itemLists[index] != null
            && InventoryManager.Instance.itemLists[index].itemBaseType.categories == EDataType.WeaponItem)
        {
            RepairUI.instance.Open();
        }
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
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            InventoryManager.Instance.MouseSingle(index);
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            InventoryManager.Instance.MouseSplit(index);
        else
            InventoryManager.Instance.MouseSwitch(index);

        // 마우스 슬롯 이미지 업뎃 + 클릭한 슬롯 이미지 업데이트
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
            InventoryManager.Instance.MouseDrop();
            mouseUI.Clear();
			InventoryUiMain.instance.IconRefresh();
			PlayerStatUI.Instance.UpdateBasicStatUI();
			return;
        }

        if(PlayerCore.Instance.currentState != PlayerCore.PlayerState.ActionFishing)
        {
            // 플레이어 아이템 핸들
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

            //{
            //    // 만약 무기다 && 내구도가 있다
            //    {
            //    }
            //    // 소비형 아이템이다
            //}
            // 내구도가 만료된 무기 혹은 맨손
        }

        if (Input.GetMouseButtonDown(1))
        {
            UtilityFunctions.Log("우클 감지");
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
        UtilityFunctions.Assert(index >= 0);
        UtilityFunctions.Assert(index < slotGameObjects.Count, $"!!>> {index} / {slotGameObjects.Count}");
        UtilityFunctions.Assert(InventoryManager.Instance != null);

        bool changed = InventoryManager.Instance.selectedSlotIndex != index || mCurrentSelectedHotbarSlot == null;
        InventoryManager.Instance.SelectSlot(index);

        if (changed && AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.SelectQuickSlot);
        mCurrentSelectedHotbarSlot = slotGameObjects[index].GetComponent<ItemSlotUI>();
        IconRefresh();
        if (changed && mCurrentSelectedHotbarSlot != null)
            mCurrentSelectedHotbarSlot.PlaySelectedFeedback();
        PlayerStatUI.Instance.UpdateBasicStatUI();

        if (CreateObject.instance != null)
        {
            SItemStack selected = InventoryManager.Instance.SelectedSlotInventory;
            SInstallableObjectDataSO installable = selected?.itemBaseType as SInstallableObjectDataSO;

            if (installable != null && selected.itemBaseType.categories == EDataType.BuildObject)
            {
                CreateObject.instance.EnterInstallMode(
                    installable,
                    new SItemStack[] { new SItemStack(selected.id, 1, selected.duability) }
                );
            }
            else
            {
                CreateObject.instance.ExitInstallMode();
            }
        }

        if (InventoryManager.Instance.SelectedSlotInventory == null)
        {
            return;
        }
        else
        {
            switch (InventoryManager.Instance.SelectedSlotInventory.id)
            {
                case 20001:
                    ;
                    break;
                case 20002:
                    ;
                    break;
                case 20003:
                    ;
                    break;
                default:
                    ;
                    break;
            }
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
        regularSlotIndices.Clear();
        foreach (var slot in inventorySlot)
        {
            int i = slotGameObjects.IndexOf(slot);
            if (i >= 0 && !quickSlot.Contains(slot)) regularSlotIndices.Add(i);
        }
        currentSelectedSlot = new List<ItemSlotUI>();
    }

    public void Start()
    {
        followUiRect1 = imageMouseHoldingItem.GetComponent<RectTransform>();
        followUiRect2 = windowMouse.GetComponent<RectTransform>();

        IconRefresh();
    }

    private bool IsDoubleClick(int currentSlot)
    {
        return (clickedSlotIndex == currentSlot) && clickTime >= Time.time;
    }
    private void BeginCheckDoubleClick(int currentIndex)
    {
        clickedSlotIndex = currentIndex;
        clickTime = Time.time + clickTerm;
    }
    private void EndCheckDoubleClick(int currentIndex)
    {
        clickedSlotIndex = -1;
    }

    private void DoubleClick(int index)
    {
        if (InventoryManager.Instance.itemLists[index] == null) return;
        EDataType category = InventoryManager.Instance.itemLists[index].itemBaseType.categories;
        if (category != EDataType.ConsumeItem && category != EDataType.BuildObject) return;

        PlayerCore.Instance.BeginCoroutine(
            ItemTypeManager.Instance.itemTypeSearch[
                InventoryManager.Instance.itemLists[index].id].Use(
                    PlayerCore.Instance,
                    InventoryManager.Instance.itemLists[index]
                )
        );

        // 더블 클릭 준비
        clickedSlotIndex = -1;
        clickTime = 0.0f;
    }
    private void PrepareSingleClick(int index)
    {
        IEnumerator mSingleClickCoroutine(int _index)
        {
            yield return new WaitForSeconds(clickTerm);
            InventoryManager.Instance.MouseSwitch(_index);
            IconRefresh();
            clickCoroutine = null;
            clickedSlotIndex = -1;
            clickTime = 0.0f;
        }

        clickCoroutine = StartCoroutine(mSingleClickCoroutine(index));
    }
    private void WithdrawSingleClick(int index)
    {
        if (clickCoroutine != null)
        {
            StopCoroutine(clickCoroutine);
            clickCoroutine = null;
            clickedSlotIndex = -1;
            clickTime = 0.0f;
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameResultActive) return;
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

        if (Time.timeScale <= 0f) return;

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

        // 인벤토리 핫키 휠 스크롤
        bool isBuilding = CreateObject.instance != null && CreateObject.instance.IsBuilding;
        float scroll = isBuilding ? 0f : Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) // 위로
        {
            hotkeyInventoryNum = InventoryManager.Instance.selectedSlotIndex - 1;
            if (hotkeyInventoryNum < 0) hotkeyInventoryNum = quickSlot.Count - 1;
            SelectSlot(hotkeyInventoryNum);
        }
        else if (scroll < 0f) // 아래로
        {
            hotkeyInventoryNum = InventoryManager.Instance.selectedSlotIndex + 1;
            if (hotkeyInventoryNum >= quickSlot.Count) hotkeyInventoryNum = 0;
            SelectSlot(hotkeyInventoryNum);
        }

# warning 나중에 채빈씨 브랜치 머지 하고 업데이트 된경우 주석 풀기
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

        if (info.categories == EDataType.WeaponItem)
        {
            categoriesName = $"{categoriesName} · {target.duability}%";
        }

        return (info.typeName, categoriesName, ItemPresentation.Description(info));
    }

    public void IconRefresh()
    {
        // 모든 아이템을
        // + 선택되지 않은 상태로 바꿈
        // + 내구도 체크
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {

            ItemSlotUI _forUi = slotGameObjects[index].GetComponent<ItemSlotUI>();

            _forUi.Show(InventoryManager.Instance.itemLists[index]);
            _forUi.image.gameObject.transform.DOKill();
            _forUi.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        }
        mouseUI.Show(InventoryManager.Instance.mouseInventory);
        mouseUI.image.gameObject.transform.DOKill();
        mouseUI.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);


        if (mCurrentSelectedHotbarSlot != null)
        {
            mCurrentSelectedHotbarSlot.image.gameObject.transform.DOKill();
            mCurrentSelectedHotbarSlot.image.gameObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        }

        PlayerStatUI.Instance.UpdateBasicStatUI();

	}
}





