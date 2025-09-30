using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemUse<Tuser, TItem> where TItem : SItemStack
{
    public IEnumerator Use(Tuser userGameObject, TItem itemWithState);
}
