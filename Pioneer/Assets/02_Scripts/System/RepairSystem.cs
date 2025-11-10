using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairSystem : MonoBehaviour
{
    public static RepairSystem instance;

    public InventoryBase slot;
    public int remainRepairCount = 0;

    private void Awake()
    {
        instance = this;
    }

    public void ClickRepair()
    {
        if (SItemStack.IsEmpty(slot.itemLists[0]))
        {
            return;
        }
        if (slot.itemLists[0].duability > 50)
        {
            Debug.Log("수리가 필요하지 않습니다.");
            return;
        }
        //if (InventoryManager.Instance.Get(40007) < 1)
        if (remainRepairCount < 1)
        {
            Debug.Log("수리 도구가 부족합니다.");
            return;
        }

        slot.itemLists[1] = slot.itemLists[0];
        slot.itemLists[1].duability += 50;
        slot.itemLists[0] = null;

        remainRepairCount--;
		//InventoryManager.Instance.Remove(new SItemStack(40007, 1));
	}

    public void Collect()
    {
        InventoryManager.Instance.Add(slot.itemLists[1]);
        slot.itemLists[1] = null;
        RepairUI.instance.IconRefresh();
    }


    // Start is called before the first frame update
    void Start()
    {
        slot = GetComponent<InventoryBase>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
