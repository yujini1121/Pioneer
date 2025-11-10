using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    static public ItemDropManager instance;

    [SerializeField] GameObject prefabDroppedItemDefault;
    [SerializeField] float pickUpTime = 2f;

    public void Drop(SItemStack target, Vector3 worldPosition)
    {
        GameObject droppedItem = Instantiate(prefabDroppedItemDefault, worldPosition, quaternion.identity);
        droppedItem.GetComponent<DroppedItem>().SetItem(target, pickUpTime);
    }

    private void Awake()
    {
        instance = this;
    }
}
