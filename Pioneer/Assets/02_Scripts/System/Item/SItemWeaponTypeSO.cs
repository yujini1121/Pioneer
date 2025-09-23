using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemWeaponType", menuName = "ScriptableObjects/Items/ItemWeaponType", order = 1)]
public class SItemWeaponTypeSO : SItemTypeSO
{
    public float weaponDamage;
    public float weaponAnimation;
    public float weaponDelay;
    public float attackCycleTime;
    public float attackPerSpeed;
    public float weaponRange;
    public int weaponDuability;
    public int duabilityRedutionPerHit;

    public override IEnumerable Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        itemWithState.isUseCoroutineEnd = false;

        yield return WeaponUseUtils.AttackCoroutine(userGameObject, itemWithState, this);

        yield return base.Use(userGameObject, itemWithState);
    }
}
