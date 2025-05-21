using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<SItemStack> itemLists;
    public Dictionary<int, SItemStack> fastSearch;

    public int Get(int id)
    {
        if (fastSearch.ContainsKey(id) == false) return 0;
        return fastSearch[id].amount;
    }

    private void mAdd(SItemStack item)
    {
        itemLists.Add(item);
        fastSearch.Add(item.id, item);
    }

    private void Awake()
    {
        Instance = this;

        itemLists = new List<SItemStack>();
        fastSearch = new Dictionary<int, SItemStack>();

        Demo();
    }

    private void Demo()
    {
        mAdd(new SItemStack(100, 5));
        mAdd(new SItemStack(101, 5));
    }
}
