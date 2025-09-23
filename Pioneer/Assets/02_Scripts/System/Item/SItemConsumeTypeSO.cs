using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemConsumeType", menuName = "ScriptableObjects/Items/ItemConsumeType", order = 1)]
public class SItemConsumeTypeSO : SItemTypeSO
{
    public int ConsumeEffect;
    public int EffectTarget;
    public int Max_Use_Count;
}
