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

    public override IEnumerator Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        Debug.Log(">> 아이템_무기 : 사용됨");

        itemWithState.isUseCoroutineEnd = false;

        if (itemWithState.duability > 0)
        {
            Debug.Log(">> 아이템_무기 : WeaponUseUtils.AttackCoroutine");
            yield return WeaponUseUtils.AttackCoroutine(userGameObject, itemWithState, this);
        }
        else
        {
            yield return WeaponUseUtils.AttackCoroutine(userGameObject, itemWithState, 
                new SItemWeaponTypeSO()
                {
                    weaponDamage = PlayerCore.Instance.AttackDamageCalculated,
                    weaponRange = PlayerCore.Instance.attackRange,
                    weaponDelay = 0.5f,
                    weaponAnimation = 0.3f
                }
                    
                );
        }
        



        yield return base.Use(userGameObject, itemWithState);
    }
}
