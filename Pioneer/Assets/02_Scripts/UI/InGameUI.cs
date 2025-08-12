using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InGameUI : MonoBehaviour, IBegin
{
    static public InGameUI instance;

    public GameObject defaultCraftUI;
    public GameObject makeshiftCraftUI;
    public GameObject ManuUI;
    public GameObject ManuDenyUI;

    public List<GameObject> currentOpenedUI = new List<GameObject>();

    //Coroutine coroutineDenyESC = null;
    float denyUiEndTime = 0.0f;
    float denyUiLifeTime = 2.0f;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Show(makeshiftCraftUI);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UseESC();
        }
        
        if (Time.time < denyUiEndTime)
        {
            ManuDenyUI.SetActive(true);
        }
        else
        {
            ManuDenyUI.SetActive(false);
        }
    }

    public void ShowDefaultCraftUI()
    {
        Clear();
    
        Show(defaultCraftUI);
    }

    public void CloseDefaultCraftUI()
    {
        Clear();
        Show(makeshiftCraftUI);
        InventoryUiMain.instance.IconRefresh();
    }

    public void Show(GameObject UiGo)
    {
        UiGo.SetActive(true);
        currentOpenedUI.Add(UiGo);
    }

    public void Clear()
    {
        foreach (GameObject go in currentOpenedUI)
        {
            go.SetActive(false);
        }
    }

    public void UseESC()
    {
        if (defaultCraftUI.activeInHierarchy)
        {
        
            CloseDefaultCraftUI();
            return;
        }

        if (ManuUI.activeInHierarchy)
        {
            ManuUI.SetActive(false);
        }
        else
        {
            if (GuiltySystem.instance.canUseESC)
            {
                ManuUI.SetActive(true);
            }
            else
            {
                denyUiEndTime = Time.time + denyUiLifeTime;
            }
        }
    }
}
