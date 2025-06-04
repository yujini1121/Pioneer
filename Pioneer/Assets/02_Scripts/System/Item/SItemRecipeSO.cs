using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemRecipe", menuName = "ScriptableObjects/Items/ItemRecipe", order = 1)]
public class SItemRecipeSO : ScriptableObject
{
    public SItemStack result;
    public SItemStack[] input;
    public bool isMakeshift;
}
