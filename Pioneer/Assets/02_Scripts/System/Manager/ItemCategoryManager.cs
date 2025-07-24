using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCategoryManager : MonoBehaviour, IBegin
{
    public static ItemCategoryManager Instance;

    public List<SItemCategorySO> categories;
    public Dictionary<int, SItemCategorySO> itemCategoriesSearchInt;
    public Dictionary<ETypes, SItemCategorySO> itemCategoriesSearchEnum;

    // 현재는 쓰이지 않고, 수동으로 테스트용 임시 함수를 만들어서 카테고리를 넣고 싶은 경우 이 함수를 사용하시오.
    private void Add(SItemCategorySO category)
    {
        categories.Add(category);
        itemCategoriesSearchInt.Add(category.typeInt, category);
        itemCategoriesSearchEnum.Add(category.categoryType, category);
    }

    private void Awake()
    {
        Instance = this;

        //types = new List<SItemTypeSO>();
        itemCategoriesSearchInt = new Dictionary<int, SItemCategorySO>();
        itemCategoriesSearchEnum = new Dictionary<ETypes, SItemCategorySO>();
        InspectorRegister();

        //Demo();
    }

    // Start is called before the first frame update
    private void Init()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InspectorRegister()
    {
        foreach (SItemCategorySO one in categories)
        {
            itemCategoriesSearchInt.Add(one.typeInt, one);
            itemCategoriesSearchEnum.Add(one.categoryType, one);
        }
    }
}
