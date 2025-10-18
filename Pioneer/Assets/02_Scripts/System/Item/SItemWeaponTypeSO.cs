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

    public static SItemWeaponTypeSO operator+ (SItemWeaponTypeSO l, SItemWeaponTypeSO r)
    {
        return new SItemWeaponTypeSO()
        {
            weaponDamage = l.weaponDamage + r.weaponDamage,
            weaponAnimation = l.weaponAnimation + r.weaponAnimation,
            weaponDelay = l.weaponDelay + r.weaponDelay,
            attackCycleTime = l.attackCycleTime + r.attackCycleTime,
            weaponDuability = l.weaponDuability + r.weaponDuability,
            weaponRange = l.weaponRange + r.weaponDelay
        };
    }

    public void DeepCopyFrom(SItemWeaponTypeSO othersWhichHasValue)
    {
        this.weaponDamage = othersWhichHasValue.weaponDamage;
        this.weaponAnimation = othersWhichHasValue.weaponAnimation;
        this.weaponDelay = othersWhichHasValue.weaponAnimation;
        this.attackCycleTime = othersWhichHasValue.attackCycleTime;
        this.attackPerSpeed = othersWhichHasValue.attackPerSpeed;
        this.weaponRange = othersWhichHasValue.weaponRange;
        this.weaponDuability = othersWhichHasValue.weaponDuability;
        this.duabilityRedutionPerHit = othersWhichHasValue.duabilityRedutionPerHit;
    }

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
                PlayerCore.Instance.handAttackStartDefault

                );
        }
        



        yield return base.Use(userGameObject, itemWithState);
    }
}
