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
///     해당 클래스는 공통적인 로직을 퍼블릭 정적 메서드로 바꿔놓은 것입니다. 코드 반복을 피하기 위한 클래스입니다.
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

    // 솔직히 말하면 아이템을 선택했을때 조합할 수 있는지 아닌지를 가져오는것은 똑같다고 봄
    // - 제작할 수 있는가? -> 아이템 레시피 매니저
    // - 제작 창 변경

    // 아이템 제작 창을 변경해줍니다
    // DefaultFabrication ui : 제작 창 게임오브젝트의 컴포넌트 입니다.
    // SItemRecipeSO recipe : 제작하려는 레시피입니다.
    // InventoryBase inventory : 누구의 인벤토리를 기반으로 만들 것인지입니다. 일반적으로 플레이어의 인벤토리를 갖다씁니다
    // GameObject[] outsideGameObjectCraftButtonsWithImage : 이미지를 가지고 있는 게임오브젝트의 목록이며, 해당 게임오브젝트는 아이템을 만들 수 있는지 아닌지 여부를 보여주기 위함입니다. 못 만들면 반투명하게 해야 하거든요
    public void UpdateCraftWindowUi(DefaultFabrication ui, SItemRecipeSO recipe, InventoryBase inventory, GameObject[] outsideGameObjectCraftButtonsWithImage)
    {
        if (IsDebuggingCraft)
        {
            Debug.Log($">> CommonUI.UpdateCraftWindowUi(...) -> 함수 호출됨");
        }

        currentRecipe = recipe;
        SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];
        ui.craftName.text = recipeResultType.typeName;
        ui.craftLore.text = recipeResultType.infomation;

        for (int rIndex = 0; rIndex < 3; rIndex++)
        {
            ui.materialPivots[rIndex].SetActive(false);
            //ui.materialEachText[rIndex].text = "";
            ui.materialEachText[rIndex].enabled = false;
            //ui.materialIconImage[rIndex].sprite = instance.imageEmpty;
            ui.materialIconImage[rIndex].enabled = false;
        } 

        Vector3 mPositionPivot = Vector3.zero;
        switch (recipe.input.Length)
        {
            case 1: mPositionPivot = new Vector3(0, 100, 0); break;
            case 2: mPositionPivot = new Vector3(-112.5f, 100, 0); break;
            case 3: mPositionPivot = new Vector3(-225f, 100, 0); break;
            default: break;
        }

        Vector3 delta = new Vector3(225, 0, 0);
        for (int rIndex = 0; rIndex < recipe.input.Length; rIndex++)
        {
            ui.materialPivots[rIndex].SetActive(true);
            ui.materialPivots[rIndex].GetComponent<RectTransform>().anchoredPosition
                = mPositionPivot + rIndex * delta;

            ui.materialEachText[rIndex].enabled = true;
            ui.materialIconImage[rIndex].enabled = true;

            int need = recipe.input[rIndex].amount;
            int has = inventory.Get(recipe.input[rIndex].id);

            ui.materialEachText[rIndex].text = $"{has}/{need}";
            ui.materialIconImage[rIndex].sprite = ItemTypeManager.Instance.itemTypeSearch[recipe.input[rIndex].id].image;
        }

        mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

        // 제작 시간 표시
        ui.timeLeft.text = $"{recipe.time}s";
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;

        // 크래프트 버튼 로직 배치
        ui.craftButton.onClick.RemoveAllListeners();
        ui.craftButton.onClick.AddListener(() =>
        {
            if (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id) == false) return;
            if (currentCraftCoroutine != null)
            {
                StopCoroutine(currentCraftCoroutine);
            }

            // 제작 시간 타임 보여줌 + 제작 완료 시 또 제작할 수 있는지 업데이트
            // 다만 건설 아이템인경우 다른 로직이 쓰임
            // Debug.Log($">> CommonUI.UpdateCraftWindowUi(DefaultFabrication ui, SItemRecipeSO recipe, InventoryBase inventory, GameObject[] outsideGameObjectCraftButtonsWithImage) : IsCurrentCrafting = {IsCurrentCrafting}");
            if (IsCurrentCrafting)
            {
                StopCraft(ui);
                ui.timeLeft.text = $"{recipe.time}s";
            }
            else if (recipe.resultBuildingOrNull == null)
            {
                currentCraftCoroutine = StartCoroutine(CraftCoroutine(recipe, outsideGameObjectCraftButtonsWithImage, ui));

            }
            // 건물 건축인 경우
            else
            {
                // 제작 창을 닫음
                // 헤당 위치로 이동
                // 시간 소모
                // 방해 없으면 계속 개발

                // 여기서 건축물 선택
                CloseTab(ui);
                InGameUI.instance.UseTab();

                CreateObject.instance.EnterInstallMode(recipe.resultBuildingOrNull, recipe.input);
                //asdasdads

            }
        });
    }

#warning TODO : 구조물 배치 로직
    // 카테고리 UI
    // GameObject parent : 버튼들의 부모 게임오브젝트입니다
    // SItemCategorySO category : 카테고리 스크립터블 오브젝트입니다
    // DefaultFabrication ui : 제작 창 게임오브젝트의 컴포넌트 입니다.
    // ArgumentGeometry geometryCategoryButton : 카테고리 버튼의 기하학적 배치 기능을 위한 매개변수입니다
    // ArgumentGeometry geometryCraftSelectCategory, : 제작 선택의 카테고리 항목의 기하학적 배치 기능을 위한 매개변수입니다
    // ArgumentGeometry geometryCraftSelectButton : 제작 선택 버튼의 기하학적 배치 기능을 위한 매개변수입니다
    // List<GameObject> prevCraftSelectButton : 이전 제작 선택 UI을 지우기 위한 매개변수입니다. 해당 참조로 새롭게 만들어진 제작 선택 게임오브젝트들이 원소로 들어옵니다
    public Button ShowCategoryButton(GameObject parent, SItemCategorySO category, DefaultFabrication ui,
        ArgumentGeometry geometryCategoryButton,
        ArgumentGeometry geometryCraftSelectCategory,
        ArgumentGeometry geometryCraftSelectButton,
        List<GameObject> prevCraftSelectButton)
    {
		if (IsDebuggingCraft)
		{
			Debug.Log($">> CommonUI.ShowCategoryButton(...) -> 함수 호출됨");
		}

		// 1. 카테고리 이미지 버튼

		// 레시피는 스텐다드매니저 뜯어봐서 해당 항목의 모든 카테고리속 레시피를 가져옴
		// 버튼을 누르면, ShowItemButton와 프리펩 호출함

		// 버튼 배치
		GameObject categoryButtonObject = Instantiate(prefabItemCategoryButton, parent.transform);
        RectTransform rectTransform = categoryButtonObject.GetComponent<RectTransform>();
        //rectTransform.sizeDelta = size;
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

        // 버튼 로직 배치
        Button categoryButton = categoryButtonObject.GetComponent<Button>();
        categoryButton.onClick.AddListener(() =>
        {
            // 2. 제작 선택 버튼들
            // 해당 버튼을 누르면 제작 선택 UI가 뜸
            // 기존 제작 선택을 싹 제거함
            foreach (GameObject prevUi in prevCraftSelectButton) Destroy(prevUi);
            ui.gameObject.SetActive(false);

            // 제작 선택 카테고리 항목
            GameObject craftSelectCategory = Instantiate(prefabCraftSelectTopButton, parent.transform);
            craftSelectCategory.transform.parent = geometryCraftSelectCategory.parent.transform;
            craftSelectCategory.transform.localPosition = geometryCraftSelectCategory.start2D;
            prevCraftSelectButton.Add(craftSelectCategory);
            CraftItemSelectTop craftSelectCategoryUi = craftSelectCategory.GetComponent<CraftItemSelectTop>();
            craftSelectCategoryUi.categoryImage.sprite = category.categorySprite;
            craftSelectCategoryUi.categoryName.text = category.categoryName;

            // 제작 선택 버튼 소환
            for (int index = 0; index < category.recipes.Count; index++)
            {
                GameObject m_one = Instantiate(prefabCraftSelectItemButton, parent.transform);

                prevCraftSelectButton.Add(m_one);
                // 레시피 가져오기
                SItemRecipeSO recipe = category.recipes[index];
                SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];
                // 버튼 배치
                SetPosition(
                    m_one,
                    geometryCraftSelectButton.parent,
                    index,
                    1,
                    -new Vector2(0, m_one.GetComponent<RectTransform>().sizeDelta.y),
                    geometryCraftSelectButton.start2D);
                CraftItemSelectSingle m_oneUi = m_one.GetComponent<CraftItemSelectSingle>();

                m_oneUi.image.sprite = ItemTypeManager.Instance.itemTypeSearch[category.recipes[index].result.id].image;
                m_oneUi.itemName.text = ItemTypeManager.Instance.itemTypeSearch[category.recipes[index].result.id].typeName;

                // 버튼 로직 배치
                //Button craftSelectItemButtons = categoryButtonObject.GetComponent<Button>();
                m_oneUi.button.onClick.AddListener(() =>
                {
					Debug.Log($">> CommonUI.ShowCategoryButton(...) -> 버튼 클릭됨!");


					ui.gameObject.SetActive(true);
                    UpdateCraftWindowUi(ui, recipe, InventoryManager.Instance, new GameObject[] { m_one });
                });
            }

        });
        return categoryButton;
    }

    // 간이 제작 UI
    public Button ShowItemButton(GameObject parent, SItemRecipeSO recipe, DefaultFabrication ui,
        int index, int rowCount, Vector2 delta, Vector2 start, Vector2 size)
    {
        SItemTypeSO recipeResultType = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];

        // 버튼 배치
        GameObject itemButtonGameObject = Instantiate(instance.prefabItemButton, parent.transform);
        RectTransform rectTransform = itemButtonGameObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        SetPosition(itemButtonGameObject, parent, index, rowCount, delta, start);

        // 버튼 가용성 표시
        mSetButtonAvailable(itemButtonGameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

        // 버튼 이미지 배치
        itemButtonGameObject.GetComponent<UnityEngine.UI.Image>().sprite =
            ItemTypeManager.Instance.itemTypeSearch[recipe.result.id].image;

        // 버튼 로직 배치
        Debug.Assert(itemButtonGameObject != null);
        Debug.Assert(itemButtonGameObject.GetComponent<Button>() != null);
        Button itemButton = itemButtonGameObject.GetComponent<Button>();


        itemButton.onClick.AddListener(() => // 버튼 클릭 시
        {
            ui.gameObject.SetActive(true);
            UpdateCraftWindowUi(ui, recipe, InventoryManager.Instance, new GameObject[] { itemButtonGameObject });
        });
        return itemButton;
    }




    // 아이템 버튼





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

        target.transform.position = parent.transform.position + new Vector3(start.x, start.y, 0.0f) + new Vector3(delta.x * xPos, delta.y * yPos);
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator CraftCoroutine(SItemRecipeSO recipe, GameObject[] itemButtonGameObject, DefaultFabrication ui)
    {
        if (isDebugging_CraftCoroutine)
        {
            Debug.Log($">> CommonUI.CraftCoroutine(...) -> 함수 호출됨");
        }

        // 입력 시간만큼 진행
        // 성공시 리턴
        IsCurrentCrafting = true;
        float leftTime = recipe.time;
        ui.craftButtonWord.text = DefaultFabrication.CraftEnd;

        while (leftTime > 0.0f)
        {
            ui.timeLeft.text = $"{leftTime}s";
            leftTime -= Time.deltaTime;
            yield return null;
        }
        Craft(recipe, itemButtonGameObject, ui);

		ui.timeLeft.text = $"제작 완료";
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;
        InventoryUiMain.instance.IconRefresh();
        IsCurrentCrafting = false;
        // 요기에 경험치 추가 로직
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
                AudioManager.instance.PlaySfx(AudioManager.SFX.GreatSuccessCrafting);
        }
        else
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.SuccessCrafting);
        }


        if (isSuccess)
        {
            if (IsDebuggingCraftCoroutine)
            {
                Debug.Log($">> CommonUI.Craft(...) : 대성공 발생했습니다!");
            }

            // 대성공 발생
            PlayerCore.Instance.creatureEffect.Effects[7].Play();
            // 아이템 하나 더 추가
            InventoryManager.Instance.Add(recipe.result);

            // 아이템 페이백
            foreach (SItemStack one in recipe.input)
            {
                SItemStack newRef = one.Copy();

                newRef.amount *= 4;
                newRef.amount /= 10;

                InventoryManager.Instance.Add(newRef); // 40 퍼선트 페이백
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
            mSetButtonAvailable(itemButtonGameObject[buttonIndex].GetComponent<UnityEngine.UI.Image>(), recipe);
        }
        mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);
    }

    public void StopCraft(DefaultFabrication ui)
    {
        StopCoroutine(currentCraftCoroutine);
        currentCraftCoroutine = null;
        IsCurrentCrafting = false;
        ui.craftButtonWord.text = DefaultFabrication.CraftStart;
        ui.timeLeft.text = $"{currentRecipe.time}s";
    }

    public void CloseTab(DefaultFabrication ui)
    {
        if(IsCurrentCrafting) StopCraft(ui);
        ui.gameObject.SetActive(false);
    }
}
