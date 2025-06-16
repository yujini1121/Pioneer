using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRecipeManager : MonoBehaviour
{
    public static ItemRecipeManager Instance;

    public List<SItemRecipeSO> recipes;
    public Dictionary<int, SItemRecipeSO> recipesSearch;

    public bool CanCraftInInventory(int id)
    {
        SItemRecipeSO target = recipesSearch[id];

        for (int index = 0; index < target.input.Length; ++index)
        {
            SItemStack one = target.input[index];
            if (InventoryManager.Instance.Get(one.id) < one.amount) return false;
        }
        return true;
    }

    private void Add(SItemRecipeSO recipe)
    {
        recipes.Add(recipe);
        recipesSearch.Add(recipe.result.id, recipe);
    }

    private void Awake()
    {
        Instance = this;

        ValueAssign();
        //Demo();
        InspectorRegister();
    }

    private void Start()
    {
        
    }

    private void ValueAssign()
    {
        //recipes = new List<SItemRecipe>(); // 인스펙터 창에 설정한 값을 없애버리고 싶은 경우.
        recipesSearch = new Dictionary<int, SItemRecipeSO>();
    }

    private void InspectorRegister()
    {
        foreach (SItemRecipeSO recipe in recipes)
        {
            recipesSearch.Add(recipe.result.id, recipe);
        }
    }

    private void Demo()
    {
        SItemRecipeSO hamburger = new SItemRecipeSO()
        {
            input = new SItemStack[]
            {
                new SItemStack(100, 2),
                new SItemStack(101, 1),
            },
            result = new SItemStack(102, 1)
        };

        Add(hamburger);
    }
}
