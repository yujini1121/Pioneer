using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemTypeManager : MonoBehaviour
{
    public static ItemTypeManager Instance;

    public List<SItemType> types;
    public Dictionary<int, SItemType> itemTypeSearch;

    private void Add(SItemType type)
    {
        types.Add(type);
        itemTypeSearch.Add(type.id, type);
    }

    private void Awake()
    {
        Instance = this;

        types = new List<SItemType>();
        itemTypeSearch = new Dictionary<int, SItemType>();

        Demo();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Demo()
    {
        SItemType demoItem1 = new SItemType();
        demoItem1.id = 1;
        demoItem1.name = "cat";
        demoItem1.infomation = "cute";

        Add(demoItem1);
        SItemType bun = new SItemType()
        {
            id = 100,
            name = "bun",
            infomation = "eww2"
        };
        Add(bun);
        SItemType patty = new SItemType()
        {
            id = 101,
            name = "patty",
            infomation = "eww3"
        };
        Add(patty);
        SItemType hamburger = new SItemType()
        {
            id = 102,
            name = "burger",
            infomation = "eww"
        };
        Add(hamburger);
    }
}
