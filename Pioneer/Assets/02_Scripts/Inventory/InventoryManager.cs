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

    public void Add(SItemStack item)
    {
        if (item.amount < 1) return;

        if (fastSearch.ContainsKey(item.id) == false)
        {
            itemLists.Add(item);
            fastSearch.Add(item.id, item);
            return;
        }
        fastSearch[item.id].amount += item.amount;
    }

    public void Remove(params SItemStack[] removeTargets)
    {
        for (int index = 0; index < removeTargets.Length; index++)
        {
            fastSearch[removeTargets[index].id].amount -= removeTargets[index].amount;
        }
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
        Add(new SItemStack(100, 5));
        Add(new SItemStack(101, 5));
    }
}
