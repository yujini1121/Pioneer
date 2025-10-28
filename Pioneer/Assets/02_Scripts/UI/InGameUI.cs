using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 모든 게임 Ui는 여기서 해결합니다.
// 세부적 조작은 해당 컴포넌트를 경유해서 세팅합니다.
public class InGameUI : MonoBehaviour, IBegin
{
    static public InGameUI instance;

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
    private List<GameObject> mainCraftSelectUi;

    //Coroutine coroutineDenyESC = null;
    float denyUiEndTime = 0.0f;
    float denyUiLifeTime = 2.0f;
    bool isCraftButtonExist = false;
    bool isPannelExpand = true;
    bool isNearCraft = false;

    private void Awake()
    {
        instance = this;
        mainCraftSelectUi = new List<GameObject>();

    }

    // Start is called before the first frame update
    void Start()
    {
        Show(makeshiftCraftUI);
        UseTab();
    }

    // Update is called once per frame
    void Update()
    {
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
        Clear();
        // 여기서 세팅

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

        Show(defaultCraftUI);
        currentFabricationUi = mainCraft.ui;
        isNearCraft = true;
    }

    public void CloseDefaultCraftUI()
    {
        CommonUI.instance.CloseTab(makeshiftCraft.ui);
        Clear();
        Show(makeshiftCraftUI);
        makeshiftCraftUI.SetActive(isPannelExpand);
        InventoryUiMain.instance.IconRefresh();
        currentFabricationUi = makeshiftCraft.ui;
        isNearCraft = false;
    }

    public void Show(GameObject UiGo)
    {
        UiGo.SetActive(true);
        currentOpenedUI.Add(UiGo);
    }

    public void Clear()
    {
        foreach (GameObject go in currentOpenedUI)
        {
            go.SetActive(false);
        }
    }

    public void UseESC()
    {
        if (defaultCraftUI.activeInHierarchy)
        {
        
            CloseDefaultCraftUI();
            return;
        }

        if (ManuUI.activeInHierarchy)
        {
            ManuUI.SetActive(false);
        }
        else
        {
            if (GuiltySystem.instance.canUseESC)
            {
                ManuUI.SetActive(true);
            }
            else
            {
                denyUiEndTime = Time.time + denyUiLifeTime;
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

        isPannelExpand = !isPannelExpand;

        gameObjectBackgroundWhiteScreen.SetActive(isPannelExpand);

        foreach (GameObject g in gameObjectListExpandedInventory)
        {
            g.SetActive(isPannelExpand);
        }
        if (isPannelExpand == false)
        {
            if (currentFabricationUi != null) CommonUI.instance.CloseTab(currentFabricationUi);
            makeshiftCraftUI.SetActive(false);

            Debug.Log(">> 닫기");
        }
        if (isPannelExpand == true && isNearCraft == false)
        {
            makeshiftCraftUI.SetActive(true);
        }

    }
}
