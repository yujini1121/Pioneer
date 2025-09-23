using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemType", menuName = "ScriptableObjects/Items/ItemType", order = 1)]
public class SItemTypeSO : ScriptableObject, IItemUse<CommonBase, SItemStack>
{
    public int id;
    public int maxStack;
    public string typeName;
    public EDataType categories;
    public string infomation;
    public Sprite image;

    public SItemTypeSO()
    {

    }

    public virtual IEnumerable Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        itemWithState.isUseCoroutineEnd = false;
        yield return null;
        itemWithState.isUseCoroutineEnd = true;
    }
}
