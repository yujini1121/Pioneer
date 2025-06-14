using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    static public ItemDropManager instance;

    [SerializeField] GameObject prefabDroppedItemDefault;

    public void Drop(SItemStack target, Vector3 worldPosition)
    {
        Instantiate(prefabDroppedItemDefault, worldPosition, quaternion.identity);
    }

    private void Awake()
    {
        instance = this;
    }
}
