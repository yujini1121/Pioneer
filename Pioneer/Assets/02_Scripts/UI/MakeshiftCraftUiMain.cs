using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeshiftCraftUiMain : MonoBehaviour, IBegin
{
    public static MakeshiftCraftUiMain instance;

    [Header("UI")]
    [SerializeField] GameObject pivot;
    public DefaultFabrication ui;
    public List<GameObject> prevButtonGameObjectList = new List<GameObject>();
    public bool isOpened = false;
    [SerializeField] Vector2 itemButtonSize;
    List<SItemRecipeSO> makeshift;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        makeshift = new List<SItemRecipeSO>();
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
            prevButtonGameObjectList.Add(button.gameObject);
        }
         
    }

    public void UpdateRecipe()
    {
        foreach(GameObject g in prevButtonGameObjectList)
        {
            Destroy(g);
        }
        if (makeshift == null)
        {
            makeshift = new List<SItemRecipeSO>();
            foreach (SItemRecipeSO recipe in ItemRecipeManager.Instance.recipes)
            {
                if (recipe.isMakeshift) makeshift.Add(recipe);
            }
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
            prevButtonGameObjectList.Add(button.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBegin()
    {

    }
}
