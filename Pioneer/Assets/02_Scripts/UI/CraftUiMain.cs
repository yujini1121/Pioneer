using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CraftUiMain : MonoBehaviour
{
    public static CraftUiMain instance;

    [Header("prefab")]
    public GameObject prefabCraftItemButton;

    [Header("UI")]
    public UnityEngine.UI.Button rightCraftButton;
    public UnityEngine.UI.Image material1iconImage;
    public UnityEngine.UI.Image material2iconImage;
    public UnityEngine.UI.Image material3iconImage;
    public TextMeshProUGUI craftName;
    public TextMeshProUGUI material1eaText;
    public TextMeshProUGUI material2eaText;
    public TextMeshProUGUI material3eaText;
    public TextMeshProUGUI craftLore;
    public GameObject pivotItem;
    public Vector3 startPos;
    public float xTerm;
    public float yTerm;

    private SItemRecipe currentSelectedRecipe;
    private TextMeshProUGUI[] materialEachText;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //textMeshProUGUI.tex

        ItemRender(); // 외부 컴포넌트에 접근하므로 반드시 어웨이크가 아닌 스타드에 있어야 합니다.
    }

    // Update is called once per frame
    void Update()
    {
        materialEachText = new TextMeshProUGUI[3]
        {
            material1eaText,
            material2eaText,
            material3eaText,
        };
    }

    private void ValueAssign()
    {

    }

    void ItemRender()
    {
        void mButtonAvailable(GameObject target, SItemRecipe pRecipe)
        {
            Image buttonImage = target.GetComponent<Image>();
            Color buttonColor = buttonImage.color;
            if (ItemRecipeManager.Instance.CanCraftInInventory(pRecipe.result.id))
            {
                buttonColor.a = 0.5f;
            }
            else
            {
                buttonColor.a = 1.0f;
            }
            buttonImage.color = buttonColor;
        }


        // 아이템을 보여줌.
        for (int index = 0; index < ItemRecipeManager.Instance.recipes.Count; ++index)
        {
            SItemRecipe recipe = ItemRecipeManager.Instance.recipes[index];
            currentSelectedRecipe = recipe;
            SItemType recipeResult = ItemTypeManager.Instance.itemTypeSearch[recipe.result.id];

            int xPos = index % 4;
            int yPos = index / 4;

            GameObject buttonObject = Instantiate(prefabCraftItemButton, pivotItem.transform);
            buttonObject.transform.position = pivotItem.transform.position + startPos + new Vector3(xTerm * xPos, yTerm * yPos);

            UnityEngine.UI.Button button = buttonObject.GetComponent<Button>();

            void mShowItemButton()
            {
                ColorBlock colorblock = button.colors;
                Color color = button.colors.selectedColor;


                switch (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id))
                {
                    case true: color.a = 1.0f; break;
                    case false: color.a = 0.5f; break;
                }

                colorblock.selectedColor = color;
                colorblock.normalColor = color;
                colorblock.highlightedColor = color;
                button.colors = colorblock;
            }

            mShowItemButton();
            button.onClick.AddListener(() => // 좌측 아이템 아이콘을 눌렸을 때 보여주기.
            {
                // 레시피를 보여주는 람다식

                // 결과 보여주기
                craftName.text = recipeResult.name;
                craftLore.text = recipeResult.infomation;

                // 레시피 보여주기
                void mShowText()
                {
                    for (int rIndex = 0; rIndex < 3; ++rIndex)
                    {
                        materialEachText[rIndex].text = "";
                    }
                    for (int rIndex = 0; rIndex < recipe.input.Length; ++rIndex)
                    {
                        int need = recipe.input[rIndex].amount;
                        int has = InventoryManager.Instance.Get(recipe.input[rIndex].id);

                        materialEachText[rIndex].text = $"{has}/{need}";
                    }
                }
                mShowText();

#warning 아이템 레시피 재료 이미지 보여주는 기능

                void mShowButton()
                {
                    ColorBlock colorblock = rightCraftButton.colors;
                    Color color = rightCraftButton.colors.selectedColor;
                    

                    switch (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id))
                    {
                        case true: color.a = 1.0f; break;
                        case false: color.a = 0.5f; break;
                    }

                    colorblock.selectedColor = color;
                    colorblock.normalColor = color;
                    colorblock.highlightedColor = color;
                    rightCraftButton.colors = colorblock;
                }
                mShowButton();

                // 우측 크래프팅 버튼 작업
#warning 우측 버튼 작업 구현할 것
                // 버튼을 눌렀을 때, 아이템 조합 가능한지 판단
                // 그뒤 아이템 차감 후 지급
                rightCraftButton.onClick.AddListener(() =>
                {
                    if (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id) == false) return;

                    InventoryManager.Instance.Add(recipe.result);
                    InventoryManager.Instance.Remove(recipe.input);

                    // 레시피 보여주기
                    mShowText();
                    mShowButton();
                    mShowItemButton();
                });



            });

            // 아이템 조합 가능한지 판단
            mButtonAvailable(buttonObject, recipe);

        }



    }

}
