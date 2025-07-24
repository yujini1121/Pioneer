using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeshiftCraftUiMain : MonoBehaviour, IBegin
{
    [Header("UI")]
    [SerializeField] GameObject pivot;
    [SerializeField] DefaultFabrication ui;
    [SerializeField] UnityEngine.UI.Button closeTab;
    [SerializeField] Vector2 itemButtonSize;

    // Start is called before the first frame update
    void Init()
    {
        closeTab.onClick.AddListener(() =>
        {
            ui.gameObject.SetActive(false);
        });

        for (int index = 0; index < ItemRecipeManager.Instance.recipes.Count; index++)
        {
            //Debug.Assert(CommonUI.instance != null);
            //Debug.Assert(pivot != null);
            //Debug.Assert(ItemRecipeManager.Instance != null);
            //Debug.Assert(ItemRecipeManager.Instance.recipes != null);
            //Debug.Assert(ItemRecipeManager.Instance.recipes[index] != null);
            //Debug.Assert(ui != null);
            Button button = CommonUI.instance.ShowItemButton(pivot, ItemRecipeManager.Instance.recipes[index], ui,
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
