using System;
using System.Collections.Generic;
using UnityEngine;

// 모든 게임 UI 열기/닫기 연결 스크립트
// 각종 조작은 이 컴포넌트를 경유해서 처리합니다
public class InGameUI : MonoBehaviour, IBegin
{
    static public InGameUI instance;

    public const int ID_CHAR_PANNEL = 1;
    public const int ID_MAKESHIFT = 2;
    public const int ID_REPAIR_ITEM = 3;
    public const int ID_MAST_UI = 4;
    public const int ID_MAST_UPGRADE = 5;
    public const int ID_CRAFTTABLE = 6;
    public const int ID_ESC_OPTION = 7;
    public const int ID_ESC_OPTION_SETTINGS = 8;
    public const int ID_ESC_OPTION_HELP = 9;

    [Header("Sub UI GameObjects")]
    public GameObject gameObjectBarChart;
    public GameObject gameObjectGuiltyBarChart; // 죄책감
    public GameObject gameObjectBuffEffect;
    public GameObject gameObjectItemGet;
    public GameObject gameObjectClock;
    public GameObject gameObjectGameOverUI;
    public GameObject gameObjectRepair;
    public GameObject gameObjectBackgroundWhiteScreen;
    public GameObject gameObjectMastParent;
    public GameObject gameObjectMastBase;
    public GameObject gameObjectMastMessage;
    public GameObject gameObjectMastInteractiveText;
    public GameObject gameObjectMastUpgrade;
    public GameObject gameObjectPlayerStatUiParent;
    public GameObject gameObjectCharacterPannelUI;
    public GameObject defaultCraftUI;
    public GameObject defaultCraftUiSubPivot;
    public GameObject makeshiftCraftUI;
    public GameObject gameObjectStatus;
    public GameObject gameObjectInventory;
    public GameObject ManuUI;
    public GameObject ManuDenyUI;
    public List<GameObject> gameObjectListExpandedInventory; // 인벤토리 칸 / 정렬 버튼 / 버리기 버튼

    [Header("Sub UI Logic")]
    public CraftUiMain mainCraft;
    public MakeshiftCraftUiMain makeshiftCraft;
    [HideInInspector] public DefaultFabrication currentFabricationUi;
    public PlayerStatUI playerStatUi;

    public List<GameObject> currentOpenedUI = new List<GameObject>();
    public List<InGameUiChunk> uiChunkStack = new List<InGameUiChunk>();
    private List<GameObject> mainCraftSelectUi;

    float denyUiEndTime = 0.0f;
    float denyUiLifeTime = 2.0f;
    bool isCraftButtonExist = false;
    bool isPannelExpand = true;
    public bool IsPannelExpanded => isPannelExpand;
    bool isNearCraft = false;

    private void Awake()
    {
        instance = this;
        mainCraftSelectUi = new List<GameObject>();
    }

    void Start()
    {
        UseTab();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log($"InGameUI - makeshiftCraftUI 상태 : {makeshiftCraftUI.activeInHierarchy}");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UseESC();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UseTab();
        }

        if (Time.time < denyUiEndTime)
        {
            ManuDenyUI.SetActive(true);
        }
        else
        {
            ManuDenyUI.SetActive(false);
        }
    }

    public void ShowDefaultCraftUI()
    {
        Debug.Log($">> InGameUI.ShowDefaultCraftUI() / start / defaultCraftUI before={defaultCraftUI.activeSelf} / isCraftButtonExist={isCraftButtonExist} / categories={(ItemCategoryManager.Instance != null && ItemCategoryManager.Instance.categories != null ? ItemCategoryManager.Instance.categories.Count : -1)}");
        CommonUI.instance.CloseTab(mainCraft.ui);
        CloseUI(ID_MAKESHIFT);

        defaultCraftUI.SetActive(true);
        ApplyPanelExpandState();
        OpenUI(new List<GameObject>() { defaultCraftUI }, ID_CRAFTTABLE,
            () =>
            {
                Debug.Log("InGameUI.CloseAction 닫기 - defaultCraftUI");
                defaultCraftUI.SetActive(false);
            }
        );

        if (isCraftButtonExist == false)
        {
            isCraftButtonExist = true;
            for (int index = 0; index < ItemCategoryManager.Instance.categories.Count; ++index)
            {
                ArgumentGeometry geometryCategoryButton = new ArgumentGeometry()
                {
                    parent = defaultCraftUiSubPivot,
                    index = index,
                    rowCount = 1,
                    delta2D = new Vector2(0, -100),
                    start2D = new Vector2(0, 0),
                    size = new Vector2(100, 100)
                };
                ArgumentGeometry geometryCraftSelectCategory = new ArgumentGeometry()
                {
                    parent = defaultCraftUI,
                    index = 0,
                    rowCount = 1,
                    delta2D = new Vector2(0, 100),
                    start2D = new Vector2(-600, 300)
                };
                ArgumentGeometry geometryItemSelectButton = new ArgumentGeometry()
                {
                    parent = defaultCraftUI,
                    index = -1,
                    rowCount = 1,
                    delta2D = new Vector2(0, -100),
                    start2D = new Vector2(-600, 225),
                };

                Debug.Assert(defaultCraftUiSubPivot != null);
                Debug.Assert(ItemCategoryManager.Instance != null);
                Debug.Assert(ItemCategoryManager.Instance.categories != null);
                Debug.Assert(ItemCategoryManager.Instance.categories[index] != null);
                Debug.Assert(mainCraft != null);
                Debug.Assert(mainCraft.ui != null);
                Debug.Assert(geometryCategoryButton != null);
                Debug.Assert(geometryItemSelectButton != null);
                Debug.Log($">> InGameUI.ShowDefaultCraftUI() / create category button / idx={index} / name={ItemCategoryManager.Instance.categories[index].categoryName}");
                CommonUI.instance.ShowCategoryButton(
                    defaultCraftUiSubPivot,
                    ItemCategoryManager.Instance.categories[index],
                    mainCraft.ui,
                    geometryCategoryButton,
                    geometryCraftSelectCategory,
                    geometryItemSelectButton,
                    mainCraftSelectUi);
            }
        }

        currentFabricationUi = mainCraft.ui;
        isNearCraft = true;
        Debug.Log($">> InGameUI.ShowDefaultCraftUI() / end / defaultCraftUI active={defaultCraftUI.activeSelf} / currentFabricationUi active={mainCraft.ui.gameObject.activeSelf} / spawnedSelectUi={mainCraftSelectUi.Count}");
    }

    public void CloseDefaultCraftUI()
    {
        Debug.Log("InGameUI.CloseDefaultCraftUI() called");
        InventoryUiMain.instance.IconRefresh();
        currentFabricationUi = makeshiftCraft.ui;
        isNearCraft = false;

        CloseUI(ID_CRAFTTABLE);
        ApplyPanelExpandState();
    }

    public void UseESC()
    {
        CreateObject.instance.ExitInstallMode();

        if (uiChunkStack.Count > 0)
        {
            CloseUI();
        }
        else if (ManuUI.activeInHierarchy)
        {
            Option.instance.SetDeactivateEscUI();
        }
        else
        {
            if (GuiltySystem.instance.canUseESC)
            {
                Option.instance.SetActivateEscUI();
            }
            else
            {
                denyUiEndTime = Time.time + denyUiLifeTime;
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.CantESCNoise);
            }
        }
    }

    private void ApplyPanelExpandState()
    {
        gameObjectBackgroundWhiteScreen.SetActive(isPannelExpand);

        foreach (GameObject g in gameObjectListExpandedInventory)
        {
            g.SetActive(isPannelExpand);
        }
        InventoryUiMain.instance.InventoryExpand(isPannelExpand);
    }

    public void UseTab()
    {
        // 간이 제작 UI 열림
        // 인벤토리 UI 확장
        // 정렬 버튼
        // 버리기 버튼
        // 장비 창
        // 플레이어 스탯 창

        isPannelExpand = !isPannelExpand;
        MakeshiftCraftUiMain.instance.isOpened = isPannelExpand;

        ApplyPanelExpandState();
        if (isPannelExpand == false)
        {
            if (currentFabricationUi != null) CommonUI.instance.CloseTab(currentFabricationUi);

            makeshiftCraftUI.SetActive(false);
            gameObjectPlayerStatUiParent.SetActive(false);
            CloseUI(ID_MAKESHIFT);
            CloseUI(ID_CHAR_PANNEL);

            Debug.Log(">> 닫기");
        }
        if (isPannelExpand == true && isNearCraft == false)
        {
            Debug.Log(">> InGameUI.UseTab() 열기");

            OpenUI(new List<GameObject>() { gameObjectPlayerStatUiParent }, ID_CHAR_PANNEL,
                () => {
                    Debug.Log("InGameUI.CloseAction 닫기 - gameObjectPlayerStatUiParent");
                    gameObjectPlayerStatUiParent.SetActive(false);
                }
            );
            OpenUI(new List<GameObject>() { makeshiftCraftUI }, ID_MAKESHIFT,
                () => {
                    Debug.Log("InGameUI.CloseAction 닫기 - makeshiftCraftUI");
                    makeshiftCraftUI.SetActive(false);
                    Debug.Assert(makeshiftCraftUI.activeInHierarchy == false);
                    Debug.Log($"InGameUI.CloseAction 닫기 - makeshiftCraftUI 상태 : {makeshiftCraftUI.activeInHierarchy}");
                }
            );

            // 열기/확장 상태 반영
            MakeshiftCraftUiMain.instance.isOpened = true;
            makeshiftCraftUI.SetActive(true);
            gameObjectPlayerStatUiParent.SetActive(true);
            MakeshiftCraftUiMain.instance.UpdateRecipe();
        }
    }

    public bool IsOpened(int id)
    {
        for (int index = 0; index < uiChunkStack.Count; ++index)
        {
            if (uiChunkStack[index].id != id) continue;
            return true;
        }
        return false;
    }

    public void OpenUI(List<GameObject> uiGameobjects, int id)
    {
        OpenUI(uiGameobjects, id,
            () =>
            {
                Debug.Log("InGameUI.CloseAction 닫기");
                foreach (GameObject go in uiGameobjects) { go.SetActive(false); }
            });
    }

    public void OpenUI(List<GameObject> uiGameobjects, int id, System.Action closeAction)
    {
        Debug.Log("InGameUI.OpenUI 열기");

        foreach (GameObject g in uiGameobjects)
        {
            g.SetActive(true);
        }
        InGameUiChunk one = new InGameUiChunk(uiGameobjects, true, closeAction);
        one.id = id;
        uiChunkStack.Add(one);
    }

    public void CloseUI(int id)
    {
        for (int index = 0; index < uiChunkStack.Count; ++index)
        {
            if (uiChunkStack[index].id != id) continue;
            uiChunkStack[index].CloseAction();
            uiChunkStack.RemoveAt(index);
        }
    }

    public void CloseUI()
    {
        Debug.Log($"InGameUI.CloseUI() / Count = {uiChunkStack.Count}");

        if (uiChunkStack.Count <= 0) return;

        uiChunkStack[uiChunkStack.Count - 1].CloseAction();
        uiChunkStack.RemoveAt(uiChunkStack.Count - 1);
    }
}
