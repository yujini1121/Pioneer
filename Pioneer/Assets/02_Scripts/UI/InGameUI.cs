using System;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("하위 화면 오브젝트")]
    public GameObject gameObjectBarChart;
    public GameObject gameObjectGuiltyBarChart; // 죄책감
    public GameObject gameObjectBuffEffect;
    public GameObject gameObjectItemGet;
    public GameObject gameObjectClock;
    [SerializeField] private PlayerHitScreen playerHitScreen;
    public void ShowPlayerDamage(int damage, int maxHp) => playerHitScreen?.Play(damage, maxHp);
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

    [Header("하위 화면 기능")]
    public CraftUiMain mainCraft;
    public MakeshiftCraftUiMain makeshiftCraft;
    [HideInInspector] public DefaultFabrication currentFabricationUi;
    public PlayerStatUI playerStatUi;

    public List<GameObject> currentOpenedUI = new List<GameObject>();
    public List<InGameUiChunk> uiChunkStack = new List<InGameUiChunk>();
    private List<GameObject> mainCraftSelectUi;
    private readonly Dictionary<int, InGameUiChunk> closingChunks = new Dictionary<int, InGameUiChunk>();

    private TextMeshProUGUI actionFeedback;
    private Sequence actionFeedbackTween;
    private int actionFeedbackPriority;
    private float actionFeedbackUntil;

    public void ShowActionFeedback(string message, int priority = 0)
    {
        if (!isActiveAndEnabled || string.IsNullOrEmpty(message)
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        if (Time.unscaledTime < actionFeedbackUntil && priority < actionFeedbackPriority) return;
        if (actionFeedback == null)
        {
            var template = OceanEventManager.instance != null ? OceanEventManager.instance.currentEventName : null;
            if (template == null || template.canvas == null) return;
            var notice = new GameObject("ActionFeedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            notice.transform.SetParent(template.canvas.rootCanvas.transform, false);
            actionFeedback = notice.GetComponent<TextMeshProUGUI>();
            actionFeedback.font = template.font;
            actionFeedback.fontSharedMaterial = template.fontSharedMaterial;
            actionFeedback.fontSize = 28f;
            actionFeedback.enableAutoSizing = true;
            actionFeedback.fontSizeMin = 22f;
            actionFeedback.fontSizeMax = 28f;
            actionFeedback.alignment = TextAlignmentOptions.Center;
            actionFeedback.color = new Color(1f, 0.94f, 0.8f);
            actionFeedback.raycastTarget = false;
            var rect = actionFeedback.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -125f);
            rect.sizeDelta = new Vector2(760f, 72f);
        }
        actionFeedbackTween?.Kill();
        actionFeedback.text = message;
        actionFeedback.alpha = 0f;
        actionFeedbackPriority = priority;
        actionFeedbackUntil = Time.unscaledTime + 2.8f;
        actionFeedbackTween = DOTween.Sequence().SetUpdate(true).SetLink(actionFeedback.gameObject, LinkBehaviour.KillOnDisable)
            .Append(actionFeedback.DOFade(1f, 0.15f))
            .AppendInterval(2.2f)
            .Append(actionFeedback.DOFade(0f, 0.45f));
    }

    float denyUiEndTime = 0.0f;
    float denyUiLifeTime = 2.0f;
    bool isCraftButtonExist = false;
    bool isPannelExpand = false;
    public bool IsPannelExpanded => isPannelExpand;
    bool isNearCraft = false;

    private void Awake()
    {
        instance = this;
        mainCraftSelectUi = new List<GameObject>();
    }

    private void OnDisable()
    {
        actionFeedbackTween?.Kill();
        actionFeedbackTween = null;
        if (actionFeedback != null) actionFeedback.alpha = 0f;
        var pending = new List<InGameUiChunk>(closingChunks.Values);
        closingChunks.Clear();
        foreach (var chunk in pending) chunk.CloseAction?.Invoke();
    }

    void Start()
    {
        InitializeClosedPanelState();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameResultActive) return;
        if (Input.GetKeyDown(KeyCode.J))
        {
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
        CloseUI(ID_MAKESHIFT);

        defaultCraftUI.SetActive(true);
        ApplyPanelExpandState();
        OpenUI(new List<GameObject>() { defaultCraftUI }, ID_CRAFTTABLE,
            () =>
            {
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

                UtilityFunctions.Assert(defaultCraftUiSubPivot != null);
                UtilityFunctions.Assert(ItemCategoryManager.Instance != null);
                UtilityFunctions.Assert(ItemCategoryManager.Instance.categories != null);
                UtilityFunctions.Assert(ItemCategoryManager.Instance.categories[index] != null);
                UtilityFunctions.Assert(mainCraft != null);
                UtilityFunctions.Assert(mainCraft.ui != null);
                UtilityFunctions.Assert(geometryCategoryButton != null);
                UtilityFunctions.Assert(geometryItemSelectButton != null);
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
    }

    public void CloseDefaultCraftUI()
    {
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

    private void InitializeClosedPanelState()
    {
        isPannelExpand = false;
        isNearCraft = false;
        currentFabricationUi = makeshiftCraft != null ? makeshiftCraft.ui : null;

        if (MakeshiftCraftUiMain.instance != null)
            MakeshiftCraftUiMain.instance.isOpened = false;

        ApplyPanelExpandState();

        if (gameObjectPlayerStatUiParent != null)
            gameObjectPlayerStatUiParent.SetActive(false);

        if (makeshiftCraftUI != null)
            makeshiftCraftUI.SetActive(false);

        if (defaultCraftUI != null)
            defaultCraftUI.SetActive(false);

        if (mainCraft != null && mainCraft.ui != null)
            mainCraft.ui.gameObject.SetActive(false);

        if (makeshiftCraft != null && makeshiftCraft.ui != null)
            makeshiftCraft.ui.gameObject.SetActive(false);
    }

    public void UseTab()
    {
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

            CloseUI(ID_MAKESHIFT);
            CloseUI(ID_CHAR_PANNEL);

            UtilityFunctions.Log(">> 닫기");
        }
        if (isPannelExpand == true && isNearCraft == false)
        {

            OpenUI(new List<GameObject>() { gameObjectPlayerStatUiParent }, ID_CHAR_PANNEL,
                () => {
                    gameObjectPlayerStatUiParent.SetActive(false);
                }
            );
            OpenUI(new List<GameObject>() { makeshiftCraftUI }, ID_MAKESHIFT,
                () => {
                    makeshiftCraftUI.SetActive(false);
                    UtilityFunctions.Assert(makeshiftCraftUI.activeInHierarchy == false);
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
                foreach (GameObject go in uiGameobjects) { go.SetActive(false); }
            });
    }

    public void OpenUI(List<GameObject> uiGameobjects, int id, System.Action closeAction)
    {
        if (uiGameobjects == null || IsOpened(id)) return;
        closingChunks.Remove(id);

        foreach (GameObject g in uiGameobjects)
        {
            UITweenHelper.PlayOpen(g);
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
            CloseChunk(uiChunkStack[index]);
            uiChunkStack.RemoveAt(index);
            break;
        }
    }

    public void CloseUI()
    {

        if (uiChunkStack.Count <= 0) return;

        CloseChunk(uiChunkStack[uiChunkStack.Count - 1]);
        uiChunkStack.RemoveAt(uiChunkStack.Count - 1);
    }

#region
    private void CloseChunk(InGameUiChunk chunk)
    {
        if (chunk == null)
            return;
        closingChunks[chunk.id] = chunk;

        if (chunk.UiGameobjects == null || chunk.UiGameobjects.Count == 0)
        {
            closingChunks.Remove(chunk.id);
            chunk.CloseAction?.Invoke();
            return;
        }

        int remaining = 0;
        foreach (GameObject uiObject in chunk.UiGameobjects)
        {
            if (uiObject != null && uiObject.activeInHierarchy)
                remaining++;
        }

        if (remaining == 0)
        {
            closingChunks.Remove(chunk.id);
            chunk.CloseAction?.Invoke();
            return;
        }

        bool isClosed = false;
        foreach (GameObject uiObject in chunk.UiGameobjects)
        {
            if (uiObject == null || uiObject.activeInHierarchy == false)
                continue;

            UITweenHelper.PlayClose(uiObject, () =>
            {
                if (!closingChunks.TryGetValue(chunk.id, out var pending) || pending != chunk)
                    return;
                remaining--;
                if (remaining > 0 || isClosed)
                    return;

                isClosed = true;
                closingChunks.Remove(chunk.id);
                chunk.CloseAction?.Invoke();
            });
        }
    }
#endregion
}
