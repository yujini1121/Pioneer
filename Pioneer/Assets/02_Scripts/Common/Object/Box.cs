using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : StructureBase, IInventory
{
    public InventoryBase boxInventory;

    public InventoryBase GetInventory()
    {
        return boxInventory;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
