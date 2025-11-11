using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    public ItemRandomDropElement[] dropElements;

    public SItemStack GetDroppedItems()
    {
        float sum = 0.0f;
        foreach (ItemRandomDropElement element in dropElements)
        {
            sum += element.weight;
        }

        float one = Random.Range(0.0f, sum);

        for (int index = 0; index < dropElements.Length - 1; index++)
        {
            if (one < dropElements[index].weight)
            {
                return dropElements[index].item;
            }
            one -= dropElements[index].weight;
        }

        return dropElements[dropElements.Length - 1].item;
    }
}
