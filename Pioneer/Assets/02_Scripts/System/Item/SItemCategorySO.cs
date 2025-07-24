using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemCategory", menuName = "ScriptableObjects/Items/ItemCategory", order = 1)]
public class SItemCategorySO : ScriptableObject
{
    public int typeInt; // 만약 카테고리 순서를 바꿔야 하는 사태가 벌어질 때마다 이런 값을 수정합니다.
    public ETypes categoryType;
    public Sprite categorySprite;
}
