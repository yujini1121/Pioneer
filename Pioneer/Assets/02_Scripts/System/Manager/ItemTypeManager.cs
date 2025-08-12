using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemTypeManager : MonoBehaviour, IBegin
{
    public static ItemTypeManager Instance;

    public List<SItemTypeSO> types;
    public Dictionary<int, SItemTypeSO> itemTypeSearch;

    private void Add(SItemTypeSO type)
    {
        types.Add(type);
        itemTypeSearch.Add(type.id, type);
    }

    private void Awake()
    {
        Instance = this;

        //types = new List<SItemTypeSO>();
        itemTypeSearch = new Dictionary<int, SItemTypeSO>();
        InspectorRegister();

        //Demo();
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Demo()
    {
        SItemTypeSO demoItem1 = new SItemTypeSO();
        demoItem1.id = 1;
        demoItem1.typeName = "cat";
        demoItem1.infomation = "cute";

        Add(demoItem1);
        SItemTypeSO bun = new SItemTypeSO()
        {
            id = 100,
            typeName = "bun",
            infomation = "eww2"
        };
        Add(bun);
        SItemTypeSO patty = new SItemTypeSO()
        {
            id = 101,
            typeName = "patty",
            infomation = "eww3"
        };
        Add(patty);
        SItemTypeSO hamburger = new SItemTypeSO()
        {
            id = 102,
            typeName = "burger",
            infomation = "eww"
        };
        Add(hamburger);
    }

    private void InspectorRegister()
    {
        foreach (SItemTypeSO one in types)
        {
            itemTypeSearch.Add(one.id, one);
        }
    }
}
