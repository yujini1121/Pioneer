using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[System.Serializable]
[CreateAssetMenu(fileName = "ItemConsumeType", menuName = "ScriptableObjects/Items/ItemConsumeType", order = 1)]
public class SItemConsumeTypeSO : SItemTypeSO
{
    public int ConsumeEffect;
    public int EffectTarget;
    public int Max_Use_Count;

    public int itemID;

    public override IEnumerator Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        UtilityFunctions.Log(">> 아이템_소모 : 사용됨");

        itemWithState.isUseCoroutineEnd = false;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.ConsumeOne(itemWithState))
        {
            itemWithState.isUseCoroutineEnd = true;
            yield break;
        }

        switch (itemWithState.id)
        {
            case 40009:
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.Drink);
                break;

            case 30002:
            case 30007:
            case 30008:
            case 30011:
            case 30016:
            case 40004:
            case 40005:
            case 40006:
            case 40010:
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.EatingFood);
                break;

            case 40001:
            case 40002:
            case 40003:
            case 40007:
            case 40008:
            case 40011:
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.UseComsumpitem);
                break;
        }

        ConsumeEffectInfo.Apply(ConsumeEffect, PlayerCore.Instance);
        if (ConsumeEffect == 813) PlayerCore.Instance.StartDrunk();

        yield return base.Use(userGameObject, itemWithState);




        InventoryUiMain.instance.IconRefresh();
    }

}
