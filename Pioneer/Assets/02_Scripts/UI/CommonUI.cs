using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommonUI : MonoBehaviour, IBegin
{
    public static CommonUI instance;

    [SerializeField] GameObject prefabItemButton;
    [SerializeField] GameObject prefabItemCategoryButton;
    [SerializeField] Sprite imageEmpty;
    Coroutine currentCraftCoroutine;

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
            // 결과 보여주는 로직
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
                int has = InventoryManager.Instance.Get(recipe.input[rIndex].id);

                ui.materialEachText[rIndex].text = $"{has}/{need}";
                ui.materialIconImage[rIndex].sprite = ItemTypeManager.Instance.itemTypeSearch[recipe.input[rIndex].id].image;
            }

            mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

            // 제작 시간 표시
            ui.timeLeft.text = $"{recipe.time}s";

            // 크래프트 버튼 로직 배치
            ui.craftButton.onClick.RemoveAllListeners();
            ui.craftButton.onClick.AddListener(() =>
            {
                if (ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id) == false) return;
                if (currentCraftCoroutine != null)
                {
                    StopCoroutine(currentCraftCoroutine);
                }
                //InventoryManager.Instance.Add(recipe.result);
                //InventoryManager.Instance.Remove(recipe.input);

                //for (int rIndex = 0; rIndex < recipe.input.Length; rIndex++)
                //{
                //    int need = recipe.input[rIndex].amount;
                //    int has = InventoryManager.Instance.Get(recipe.input[rIndex].id);

                //    ui.materialEachText[rIndex].text = $"{has}/{need}";
                //}

                //mSetButtonAvailable(itemButtonGameObject.GetComponent<UnityEngine.UI.Image>(), recipe);
                //mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);

                currentCraftCoroutine = StartCoroutine(CraftCoroutine(recipe, itemButtonGameObject, ui));
            });
        });
        return itemButton;
    }

    public Button ShowCategoryButton(GameObject patent, DefaultFabrication ui, 
        int index, Vector2 delta, Vector2 buttonSize)
    {
        // 레시피는 스텐다드매니저 뜯어봐서 해당 항목의 모든 카테고리속 레시피를 가져옴

        // 버튼을 누르면, ShowItemButton와 프리펩 호출함

        throw new NotImplementedException();
    }

    private static void mSetButtonAvailable(Image buttonImage, SItemRecipeSO pRecipe)
    {
        Color buttonColor = buttonImage.color;
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
    void Init()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator CraftCoroutine(SItemRecipeSO recipe, GameObject itemButtonGameObject, DefaultFabrication ui)
    {
        // 입력 시간만큼 진행
        // 성공시 리턴

        float leftTime = recipe.time;

        while (leftTime > 0.0f)
        {
            ui.timeLeft.text = $"{leftTime}s";
            leftTime -= Time.deltaTime;
            yield return null;
        }
        Craft(recipe, itemButtonGameObject, ui);
        ui.timeLeft.text = $"제작 완료";
        InventoryUiMain.instance.IconRefresh();
    }

    public void Craft(SItemRecipeSO recipe, GameObject itemButtonGameObject, DefaultFabrication ui)
    {
        InventoryManager.Instance.Add(recipe.result);
        InventoryManager.Instance.Remove(recipe.input);

        for (int rIndex = 0; rIndex < recipe.input.Length; rIndex++)
        {
            int need = recipe.input[rIndex].amount;
            int has = InventoryManager.Instance.Get(recipe.input[rIndex].id);

            ui.materialEachText[rIndex].text = $"{has}/{need}";
        }

        mSetButtonAvailable(itemButtonGameObject.GetComponent<UnityEngine.UI.Image>(), recipe);
        mSetButtonAvailable(ui.craftButton.gameObject.GetComponent<UnityEngine.UI.Image>(), recipe);
    }
}
