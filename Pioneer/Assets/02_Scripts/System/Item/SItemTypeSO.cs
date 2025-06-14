using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemType", menuName = "ScriptableObjects/Items/ItemType", order = 1)]
public class SItemTypeSO : ScriptableObject
{
    public int id;
    public string typeName;
    public EDataType categories;
    public string infomation;
    public Sprite image;

    public SItemTypeSO()
    {

    }
}
