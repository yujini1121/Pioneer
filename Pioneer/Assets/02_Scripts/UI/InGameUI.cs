using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public GameObject defaultCraftUI;
    public GameObject makeshiftCraftUI;

    public List<GameObject> currentOpenedUI = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        Show(makeshiftCraftUI);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDefaultCraftUI()
    {
        Clear();
        Show(defaultCraftUI);
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
}
