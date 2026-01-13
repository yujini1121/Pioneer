using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeshiftCraftUiMain : MonoBehaviour, IBegin
{
    [Header("UI")]
    [SerializeField] GameObject pivot;
    public DefaultFabrication ui;
    [SerializeField] Vector2 itemButtonSize;

    // Start is called before the first frame update
    void Start()
    {
        List<SItemRecipeSO> makeshift = new List<SItemRecipeSO>();
        foreach (SItemRecipeSO recipe in ItemRecipeManager.Instance.recipes)
        {
            if (recipe.isMakeshift) makeshift.Add(recipe);
        }

        for (int index = 0; index < makeshift.Count; index++)
        {
            //Debug.Assert(CommonUI.instance != null);
            //Debug.Assert(pivot != null);
            //Debug.Assert(ItemRecipeManager.Instance != null);
            //Debug.Assert(ItemRecipeManager.Instance.recipes != null);
            //Debug.Assert(ItemRecipeManager.Instance.recipes[index] != null);
            //Debug.Assert(ui != null);
            Button button = CommonUI.instance.ShowItemButton(pivot, makeshift[index], ui,
                index, 1, new Vector2(-200, -100), new Vector2(0, 0), new Vector2(100, 100));

            button.onClick.AddListener(() =>
            {
                ui.gameObject.SetActive(true);
            });
        }
         
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
