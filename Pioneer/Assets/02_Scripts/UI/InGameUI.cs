using System;
using System.Collections.Generic;
using UnityEngine;

// 모든 게임 Ui는 여기서 해결합니다.
// 세부적 조작은 해당 컴포넌트를 경유해서 세팅합니다.
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

    [Header("서브 UI 게임오브젝트")]// UI 게임 오브젝트가 존재하고 외부 스크립트에서 접근할 필요가 있다고 판단하는 경우, 여기에 추가하실 수 있습니다.
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
    [Header("서브 UI 로직 클래스")]
    public CraftUiMain mainCraft;
    public MakeshiftCraftUiMain makeshiftCraft;
    [HideInInspector]
    public DefaultFabrication currentFabricationUi;
    public PlayerStatUI playerStatUi;

    public List<GameObject> currentOpenedUI = new List<GameObject>();
    public List<InGameUiChunk> uiChunkStack = new List<InGameUiChunk>();
    private List<GameObject> mainCraftSelectUi;

    //Coroutine coroutineDenyESC = null;
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

    // Start is called before the first frame update
    void Start()
    {
        //OpenUI(new List<GameObject>() { makeshiftCraftUI }, ID_MAKESHIFT,
        //    () => { Debug.Log("InGameUI.CloseAction 창 닫기 - makeshiftCraftUI"); makeshiftCraftUI.SetActive(false); });
        UseTab();

    }

    // Update is called once per frame
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
        CommonUI.instance.CloseTab(mainCraft.ui);
        //Clear();
        CloseUI(ID_MAKESHIFT);
        // 여기서 세팅

        defaultCraftUI.SetActive(true);
        OpenUI(new List<GameObject>() { defaultCraftUI }, ID_CRAFTTABLE,
            () =>
            {
                Debug.Log("InGameUI.CloseAction 창 닫기 - defaultCraftUI");
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
                CommonUI.instance.ShowCategoryButton(
                    defaultCraftUiSubPivot,
                    ItemCategoryManager.Instance.categories[index],
                    mainCraft.ui,
                    geometryCategoryButton,
                    geometryCraftSelectCategory,
                    geometryItemSelectButton,
                    mainCraftSelectUi);
            }




            //CommonUI.instance.ShowCategoryButton(defaultCraftUI, );

        }

        currentFabricationUi = mainCraft.ui;
        isNearCraft = true;
    }

    public void CloseDefaultCraftUI()
    {
        Debug.Log("InGameUI.CloseDefaultCraftUI() 호출됨");
        //CommonUI.instance.CloseTab(makeshiftCraft.ui);
        //Clear();
        
        //if (isPannelExpand && (IsOpened(ID_MAKESHIFT) == false))
        //{
        //    OpenUI(new List<GameObject>() { makeshiftCraftUI }, ID_MAKESHIFT,
        //    () =>
        //    {
        //        Debug.Log("InGameUI.CloseAction 창 닫기 - makeshiftCraftUI");
        //        CommonUI.instance.CloseTab(makeshiftCraft.ui);
        //        makeshiftCraftUI.SetActive(false);
        //    }
        //    );
        //}

        // makeshiftCraftUI.SetActive(isPannelExpand);
        InventoryUiMain.instance.IconRefresh();
        currentFabricationUi = makeshiftCraft.ui;
        isNearCraft = false;

        CloseUI(ID_CRAFTTABLE);
    }

    //public void Show(GameObject UiGo)
    //{
    //    UiGo.SetActive(true);
    //    currentOpenedUI.Add(UiGo);
    //}

    //public void Clear() // 모든 열린 UI 닫기
    //{
    //    foreach (GameObject go in currentOpenedUI)
    //    {
    //        go.SetActive(false);
    //    }
    //}

    public void UseESC()
    {
        CreateObject.instance.ExitInstallMode();



        if (uiChunkStack.Count > 0)
        {
            CloseUI();
        }

        //if (defaultCraftUI.activeInHierarchy)
        //{
        //    CloseDefaultCraftUI();
        //    return;
        //}

        else if (ManuUI.activeInHierarchy)
        {
            //ManuUI.SetActive(false);
            Option.instance.SetDeactivateEscUI();
        }
        else
        {
            if (GuiltySystem.instance.canUseESC)
            {
                //ManuUI.SetActive(true);
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

    public void UseTab()
    {
        // 간이 제작 탭이 열림(조합대 닿지 않을때)
        // 인벤토리 탭이 확장됨
        // 정렬 버튼
        // 버리기 버튼
        // 장비 창
        // 플레이어 스탯 창

        isPannelExpand = !isPannelExpand;

        gameObjectBackgroundWhiteScreen.SetActive(isPannelExpand);

        foreach (GameObject g in gameObjectListExpandedInventory)
        {
            g.SetActive(isPannelExpand);
        }
        InventoryUiMain.instance.InventoryExpand(isPannelExpand);
        if (isPannelExpand == false)
        {
            if (currentFabricationUi != null) CommonUI.instance.CloseTab(currentFabricationUi);
            
            makeshiftCraftUI.SetActive(false);
            gameObjectPlayerStatUiParent.SetActive(false);
            CloseUI(ID_MAKESHIFT);
            CloseUI(ID_CHAR_PANNEL);


            Debug.Log(">> 닫기");
        }
        if (isPannelExpand == true && isNearCraft == false) //
        {
            Debug.Log(">> InGameUI.UseTab() 열기");

            OpenUI(new List<GameObject>() { gameObjectPlayerStatUiParent }, ID_CHAR_PANNEL,
                () => {
                    Debug.Log("InGameUI.CloseAction 창 닫기 - gameObjectPlayerStatUiParent");
                    gameObjectPlayerStatUiParent.SetActive(false); }
                );
            OpenUI(new List<GameObject>() { makeshiftCraftUI }, ID_MAKESHIFT,
                () => {
                    Debug.Log("InGameUI.CloseAction 창 닫기 - makeshiftCraftUI");
                    makeshiftCraftUI.SetActive(false);
                    Debug.Assert(makeshiftCraftUI.activeInHierarchy == false);
                    Debug.Log($"InGameUI.CloseAction 창 닫기 - makeshiftCraftUI 상태 : {makeshiftCraftUI.activeInHierarchy}");
                }
                );
            
            // -> 여기에 확장 할당
            makeshiftCraftUI.SetActive(true);
            gameObjectPlayerStatUiParent.SetActive(true);
        }
    }

    //public void ApplyUiStack(List<GameObject> uiGameobjects) =>
    //    uiChunkStack.Add(new InGameUiChunk(uiGameobjects));
    //public void ApplyUiStack(List<GameObject> uiGameobjects, bool isNeedCloseAction, System.Action closeAction) =>
    //    uiChunkStack.Add(new InGameUiChunk(uiGameobjects, isNeedCloseAction, closeAction));

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
                Debug.Log("InGameUI.CloseAction 창 닫기");
                foreach (GameObject go in uiGameobjects) { go.SetActive(false); }
            });
    }
    public void OpenUI(List<GameObject> uiGameobjects, int id, System.Action closeAction)
    {
        Debug.Log("InGameUI.OpenUI 창 열기");

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
