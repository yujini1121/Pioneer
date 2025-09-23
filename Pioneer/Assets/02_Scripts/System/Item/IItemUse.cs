using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemUse<Tuser, TItem> where TItem : SItemStack
{
    public IEnumerable Use(Tuser userGameObject, TItem itemWithState);
}
