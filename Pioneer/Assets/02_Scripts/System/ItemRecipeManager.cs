using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRecipeManager : MonoBehaviour
{
    public static ItemRecipeManager Instance;

    public List<SItemRecipe> recipes;
    public Dictionary<int, SItemRecipe> recipesSearch;

    public bool CanCraftInInventory(int id)
    {
        SItemRecipe target = recipesSearch[id];

        for (int index = 0; index < target.input.Length; ++index)
        {
            SItemStack one = target.input[index];
            if (InventoryManager.Instance.Get(one.id) < one.amount) return false;
        }
        return true;
    }

    private void Add(SItemRecipe recipe)
    {
        recipes.Add(recipe);
        recipesSearch.Add(recipe.result.id, recipe);
    }

    private void Awake()
    {
        Instance = this;

        ValueAssign();
        Demo();
    }

    private void ValueAssign()
    {
        recipes = new List<SItemRecipe>();
        recipesSearch = new Dictionary<int, SItemRecipe>();
    }

    private void Demo()
    {
        SItemRecipe hamburger = new SItemRecipe()
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
