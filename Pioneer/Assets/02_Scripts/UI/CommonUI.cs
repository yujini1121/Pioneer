using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 UI에서 공통으로 쓰는 메서드를 모아둔 클래스입니다.
/// 중복 코드를 줄이기 위한 공용 UI 스크립트입니다.
/// </summary>
public class CommonUI : MonoBehaviour, IBegin
{
    public static CommonUI instance;

    [SerializeField] GameObject prefabItemButton;
    [SerializeField] GameObject prefabCraftSelectTopButton;
    [SerializeField] GameObject prefabCraftSelectItemButton;
    [SerializeField] GameObject prefabItemCategoryButton;
    [SerializeField] Sprite imageEmpty;
    [Header("DEBUG")]
    [SerializeField] bool isDebugging;
    [SerializeField] bool isDebugging_Craft;
    [SerializeField] bool isDebugging_CraftCoroutine;
    private bool IsDebuggingCraft => isDebugging && isDebugging_Craft;
    private bool IsDebuggingCraftCoroutine => isDebugging && isDebugging_CraftCoroutine;
    Coroutine currentCraftCoroutine;
    SItemRecipeSO currentRecipe;

    bool m_sCurrentCrafting = false;
    public bool IsCurrentCrafting
    {
        get
        {
            return m_sCurrentCrafting;
        }
        private set
        {
            if (isDebugging)
            {
                Debug.Log($">> CommonUI.IsCurrentCrafting.set : m_sCurrentCrafting : {m_sCurrentCrafting} -> {value}");
            }
            m_sCurrentCrafting = value;
        }
    }

    // 제작 버튼을 누르면 재료가 충분한지, 결과물을 만들 수 있는지 확인한 뒤 제작을 진행합니다.
    // - 제작 가능 여부 판단
    // - 제작 창 갱신

    // 제작 창 UI를 갱신합니다.
    // DefaultFabrication ui : 제작 창 게임오브젝트의 스크립트입니다.
    // SItemRecipeSO recipe : 제작하려는 레시피입니다.
    // InventoryBase inventory : 재료를 가진 인벤토리입니다. 일반적으로 플레이어 인벤토리를 넘깁니다.
    // GameObject[] outsideGameObjectCraftButtonsWithImage : 제작 가능 여부를 함께 갱신해야 하는 외부 버튼 목록입니다.
    public void UpdateCraftWindowUi(DefaultFabrication ui, SItemRecipeSO recipe, InventoryBase inventory, GameObject[] outsideGameObjectCraftButtonsWithImage)
    {
        if (IsDebuggingCraft)
        {
            Debug.Log($">> CommonUI.UpdateCraftWindowUi(...) -> 함수 호출");
        }

        currentRecipe = recipe;
        SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];
        ui.craftName.text = recipeResultType.typeName;
        ui.craftLore.text = recipeResultType.infomation;

        for (int rIndex = 0; rIndex < 3; rIndex++)
        {
            ui.materialPivots[rIndex].SetActive(false);
            ui.materialEachText[rIndex].enabled = false;
            ui.materialIconImage[rIndex].enabled = false;
        }

        Vector3 mPositionPivot = Vector3.zero;
        switch (recipe.input.Length)
        {
            case 1: mPositionPivot = new Vector3(0, 50, 0); break;
            case 2: mPositionPivot = new Vector3(-103f, 50, 0); break;
            case 3: mPositionPivot = new Vector3(-216f, 50, 0); break;
            default: break;
        }

        Vector3 delta = new Vector3(216, 0, 0);
        for (int rIndex = 0; rIndex < recipe.input.Length; rIndex++)
        {
            ui.materialPivots[rIndex].SetActive(true);
            ui.materialPivots[rIndex].GetComponent<RectTransform>().anchoredPosition = mPositionPivot + rIndex * delta;

            ui.materialEachText[rIndex].enabled = true;
            ui.materialIconImage[rIndex].enabled = true;

            int need = recipe.input[rIndex].amount;
            int has = inventory.Get(recipe.input[rIndex].id);

            ui.materialEachText[rIndex].text = $"{has}/{need}";
            ui.materialIconImage[rIndex].sprite = ItemTypeManager.Instance.itemTypeSearch[recipe.input[rIndex].id].image;
        }

        mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

        // 제작 시간 표시
        ui.timeLeft.text = $"{recipe.time:0.0}s";
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;

        // 제작 버튼 이벤트 연결
        ui.craftButton.onClick.RemoveAllListeners();
        ui.craftButton.onClick.AddListener(() =>
        {
            if (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id) == false) return;
            if (currentCraftCoroutine != null)
            {
                StopCoroutine(currentCraftCoroutine);
            }

            // 제작 시간 타이머를 시작하고 제작 완료 후 버튼 상태를 갱신
            // 다른 제작 코루틴이 돌고 있으면 먼저 중지
            if (IsCurrentCrafting)
            {
                StopCraft(ui);
                ui.timeLeft.text = $"{recipe.time:0.0}s";
            }
            else
            {
                currentCraftCoroutine = StartCoroutine(CraftCoroutine(recipe, outsideGameObjectCraftButtonsWithImage, ui));
            }
        });
    }

#warning TODO : 추후 위치 정리 필요
    // 카테고리 UI
    // GameObject parent : 버튼들의 부모 게임오브젝트입니다.
    // SItemCategorySO category : 카테고리 스크립터블 오브젝트입니다.
    // DefaultFabrication ui : 제작 창 게임오브젝트의 스크립트입니다.
    // ArgumentGeometry geometryCategoryButton : 카테고리 버튼의 위치 정보
    // ArgumentGeometry geometryCraftSelectCategory : 상단 선택 카테고리의 위치 정보
    // ArgumentGeometry geometryCraftSelectButton : 세부 선택 버튼의 위치 정보
    // List<GameObject> prevCraftSelectButton : 이전에 생성된 선택 UI 목록입니다. 새 카테고리를 누를 때 정리합니다.
    public Button ShowCategoryButton(GameObject parent, SItemCategorySO category, DefaultFabrication ui,
        ArgumentGeometry geometryCategoryButton,
        ArgumentGeometry geometryCraftSelectCategory,
        ArgumentGeometry geometryCraftSelectButton,
        List<GameObject> prevCraftSelectButton)
    {
        if (IsDebuggingCraft)
        {
            Debug.Log($">> CommonUI.ShowCategoryButton(...) -> 함수 호출");
        }

        // 1. 카테고리 아이콘 버튼
        // 카테고리 버튼을 누르면 해당 카테고리에 속한 레시피 목록을 표시
        // 버튼 생성 후 ShowItemButton 흐름으로 연결

        // 버튼 위치
        GameObject categoryButtonObject = Instantiate(prefabItemCategoryButton, parent.transform);
        RectTransform rectTransform = categoryButtonObject.GetComponent<RectTransform>();
        SetPosition(
            categoryButtonObject,
            geometryCategoryButton.parent,
            geometryCategoryButton.index,
            geometryCategoryButton.rowCount,
            geometryCategoryButton.delta2D,
            geometryCategoryButton.start2D);
        rectTransform.sizeDelta = geometryCategoryButton.size;

        // 버튼 이미지 배치
        categoryButtonObject.GetComponent<UnityEngine.UI.Image>().sprite = category.categorySprite;

        // 버튼 클릭 이벤트
        Button categoryButton = categoryButtonObject.GetComponent<Button>();
        categoryButton.onClick.AddListener(() =>
        {
            // 2. 세부 선택 버튼 생성
            // 기존에 띄워둔 세부 UI 제거
            // 제작 창은 잠시 숨김
            foreach (GameObject prevUi in prevCraftSelectButton) Destroy(prevUi);
            prevCraftSelectButton.Clear();
            ui.gameObject.SetActive(false);

            // 상단 선택 카테고리 생성
            GameObject craftSelectCategory = Instantiate(prefabCraftSelectTopButton);
            RectTransform craftSelectCategoryRect = craftSelectCategory.GetComponent<RectTransform>();
            craftSelectCategoryRect.SetParent(geometryCraftSelectCategory.parent.transform, false);
            craftSelectCategoryRect.localScale = Vector3.one;
            craftSelectCategoryRect.localPosition = geometryCraftSelectCategory.start2D;
            craftSelectCategory.SetActive(true);
            craftSelectCategory.transform.SetAsLastSibling();
            prevCraftSelectButton.Add(craftSelectCategory);
            Debug.Log($">> CommonUI.ShowCategoryButton(...) / created top ui / active={craftSelectCategory.activeSelf} / localPos={craftSelectCategoryRect.localPosition}");
            CraftItemSelectTop craftSelectCategoryUi = craftSelectCategory.GetComponent<CraftItemSelectTop>();
            craftSelectCategoryUi.categoryImage.sprite = category.categorySprite;
            craftSelectCategoryUi.categoryName.text = category.categoryName;

            // 세부 선택 버튼 순회
            int visibleIndex = 0;
            for (int index = 0; index < category.recipes.Count; index++)
            {
                // 레시피 정보 가져오기
                SItemRecipeSO recipe = category.recipes[index];
                if (recipe == null || recipe.result.id == 50004) continue;

                GameObject m_one = Instantiate(prefabCraftSelectItemButton);
                prevCraftSelectButton.Add(m_one);

                SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];

                // 버튼 위치
                SetPosition(
                    m_one,
                    geometryCraftSelectButton.parent,
                    visibleIndex,
                    1,
                    -new Vector2(0, m_one.GetComponent<RectTransform>().sizeDelta.y),
                    geometryCraftSelectButton.start2D);
                m_one.SetActive(true);
                m_one.transform.SetAsLastSibling();
                Debug.Log($">> CommonUI.ShowCategoryButton(...) / created single ui / index={visibleIndex} / active={m_one.activeSelf} / localPos={m_one.GetComponent<RectTransform>().localPosition}");
                CraftItemSelectSingle m_oneUi = m_one.GetComponent<CraftItemSelectSingle>();

                m_oneUi.image.sprite = recipeResultType.image;
                m_oneUi.itemName.text = recipeResultType.typeName;

                // 버튼 클릭 이벤트
                m_oneUi.button.onClick.AddListener(() =>
                {
                    Debug.Log($">> CommonUI.ShowCategoryButton(...) -> 버튼 클릭!");

                    ui.gameObject.SetActive(true);
                    UpdateCraftWindowUi(ui, recipe, InventoryManager.Instance, new GameObject[] { m_one });
                });

                visibleIndex++;
            }
        });
        return categoryButton;
    }

    // 세부 선택 UI
    public Button ShowItemButton(GameObject parent, SItemRecipeSO recipe, DefaultFabrication ui,
        int index, int rowCount, Vector2 delta, Vector2 start, Vector2 size)
    {
        SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];

        // 버튼 위치
        GameObject itemButtonGameObject = Instantiate(instance.prefabItemButton, parent.transform);
        RectTransform rectTransform = itemButtonGameObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        SetPosition(itemButtonGameObject, parent, index, rowCount, delta, start);

        // 버튼 사용 가능 여부 표시
        mSetButtonAvailable(itemButtonGameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

        // 버튼 이미지 배치
        itemButtonGameObject.GetComponent<UnityEngine.UI.Image>().sprite = recipeResultType.image;

        // 버튼 클릭 이벤트
        Debug.Assert(itemButtonGameObject != null);
        Debug.Assert(itemButtonGameObject.GetComponent<Button>() != null);
        Button itemButton = itemButtonGameObject.GetComponent<Button>();

        itemButton.onClick.AddListener(() =>
        {
            ui.gameObject.SetActive(true);
            UpdateCraftWindowUi(ui, recipe, InventoryManager.Instance, new GameObject[] { itemButtonGameObject });
        });
        return itemButton;
    }

    // 선택 버튼
    public Button ShowSelectButton()
    {
        return null;
    }

    private static void mSetButtonAvailable(Image buttonImage, SItemRecipeSO pRecipe)
    {
        UnityEngine.Color buttonColor = buttonImage.color;
        if (ItemRecipeManager.Instance.CanCraftInInventory(pRecipe.result.id))
        {
            buttonColor.a = 1.0f;
        }
        else
        {
            buttonColor.a = 0.5f;
        }
        buttonImage.color = buttonColor;
    }

    private static void SetPosition(GameObject target, GameObject parent, int index, int rowCount, Vector2 delta, Vector2 start)
    {
        int xPos = index % rowCount;
        int yPos = index / rowCount;
        Vector3 localPosition = new Vector3(start.x, start.y, 0.0f) + new Vector3(delta.x * xPos, delta.y * yPos);

        if (target.TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            rectTransform.SetParent(parent.transform, false);
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = localPosition;
            return;
        }

        target.transform.SetParent(parent.transform, false);
        target.transform.localPosition = localPosition;
    }

    public void PickUpUpdate()
    {
        InventoryUiMain.instance?.IconRefresh();
        PlayerStatUI.Instance?.UpdateBasicStatUI();

        if (MakeshiftCraftUiMain.instance != null && MakeshiftCraftUiMain.instance.isOpened)
        {
            MakeshiftCraftUiMain.instance.UpdateRecipe();
        }
    }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
    }

    void Update()
    {
    }

    private IEnumerator CraftCoroutine(SItemRecipeSO recipe, GameObject[] itemButtonGameObject, DefaultFabrication ui)
    {
        if (isDebugging_CraftCoroutine)
        {
            Debug.Log($">> CommonUI.CraftCoroutine(...) -> 함수 호출");
        }

        // 입력된 시간만큼 대기
        // 제작 상태 시작
        IsCurrentCrafting = true;
        float leftTime = recipe.time;
        ui.craftButtonWord.text = DefaultFabrication.CraftEnd;

        while (leftTime > 0.0f)
        {
            ui.timeLeft.text = $"{leftTime:0.0}s";
            leftTime -= Time.deltaTime;
            yield return null;
        }
        Craft(recipe, itemButtonGameObject, ui);

        ui.timeLeft.text = $"제작 완료";
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;
        InventoryUiMain.instance.IconRefresh();
        IsCurrentCrafting = false;

        // 여기에 경험치 추가 처리
        PlayerStatsLevel.Instance.AddExp(GrowStatType.Crafting, currentRecipe.exp);
    }

    public void Craft(SItemRecipeSO recipe, GameObject[] itemButtonGameObject, DefaultFabrication ui)
    {
        InventoryManager.Instance.Add(recipe.result);
        InventoryManager.Instance.Remove(recipe.input);

        bool isSuccess = UnityEngine.Random.Range(0, 1.0f) < PlayerStatsLevel.Instance.CraftingChance();

        if (isSuccess)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.GreatSuccessCrafting2);

            if (CreatureEffect.Instance != null)
            {
                ParticleSystem ps = CreatureEffect.Instance.Effects[2];
                CreatureEffect.Instance.PlayEffect(ps, PlayerCore.Instance.transform.position);
            }
            if (CreatureEffect.Instance != null)
            {
                ParticleSystem ps = CreatureEffect.Instance.Effects[6]; // 추가 효과
                CreatureEffect.Instance.PlayEffect(ps, PlayerCore.Instance.transform.position + new Vector3(0f, 1f, 0f));
            }
        }
        else
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.SuccessCrafting2);

            if (CreatureEffect.Instance != null)
            {
                ParticleSystem ps = CreatureEffect.Instance.Effects[2];
                CreatureEffect.Instance.PlayEffect(ps, PlayerCore.Instance.transform.position);
            }
        }

        if (isSuccess)
        {
            if (IsDebuggingCraftCoroutine)
            {
                Debug.Log($">> CommonUI.Craft(...) : 대성공 발생");
            }

            // 대성공 발생
            InventoryManager.Instance.Add(recipe.result);

            // 재료 일부 반환
            foreach (SItemStack one in recipe.input)
            {
                SItemStack newRef = one.Copy();
                newRef.amount *= 4;
                newRef.amount /= 10;
                InventoryManager.Instance.Add(newRef); // 40 퍼센트 페이백
            }
        }

        for (int rIndex = 0; rIndex < recipe.input.Length; rIndex++)
        {
            int need = recipe.input[rIndex].amount;
            int has = InventoryManager.Instance.Get(recipe.input[rIndex].id);
            ui.materialEachText[rIndex].text = $"{has}/{need}";
        }

        for (int buttonIndex = 0; buttonIndex < itemButtonGameObject.Length; buttonIndex++)
        {
            if (itemButtonGameObject[buttonIndex] == null) continue;

            UnityEngine.UI.Image buttonImage = itemButtonGameObject[buttonIndex].GetComponent<UnityEngine.UI.Image>();
            if (buttonImage == null) continue;

            mSetButtonAvailable(buttonImage, recipe);
        }
        mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);
    }

    public void StopCraft(DefaultFabrication ui)
    {
        if (currentCraftCoroutine == null) return;

        StopCoroutine(currentCraftCoroutine);
        currentCraftCoroutine = null;
        IsCurrentCrafting = false;
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;
        ui.timeLeft.text = $"{currentRecipe.time:0.0}s";
    }

    public void CloseTab(DefaultFabrication ui)
    {
        if (IsCurrentCrafting) StopCraft(ui);
        ui.gameObject.SetActive(false);
    }
}




