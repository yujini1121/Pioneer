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
        Debug.Log(">> 아이템_소모 : 사용됨");

        itemWithState.isUseCoroutineEnd = false;

        SItemStack removeItem = new SItemStack(itemWithState.id, 1);

        InventoryManager.Instance.Remove(removeItem);

        switch (itemWithState.id)
        {
            case 40009:
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.Drink);
                break;

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

        switch (ConsumeEffect)
        {
            //체력 상승
            case 801: //
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 15);
                var ps = CreatureEffect.Instance.Effects[3]; // Heal 이펙트
                CreatureEffect.Instance.PlayEffectFollow(ps, PlayerCore.Instance.transform, new Vector3(0f, 0f, 0f));
                break; 
            case 802: //
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 40);
                var ps1 = CreatureEffect.Instance.Effects[3]; // Heal 이펙트
                CreatureEffect.Instance.PlayEffectFollow(ps1, PlayerCore.Instance.transform, new Vector3(0f, 0f, 0f));
                break;
            case 803:
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 70);
                var ps2 = CreatureEffect.Instance.Effects[3]; // Heal 이펙트
    CreatureEffect.Instance.PlayEffectFollow(ps2, PlayerCore.Instance.transform, new Vector3(0f, 0f, 0f));
                break;
            //배고픔 해소
            case 804://
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 10);
                break;
            case 805://
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 20);
                break;
            case 806://
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 40);
                break;
            case 807://
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 70);
                break;
            //정신력 상승?
            case 808:
                PlayerCore.Instance.UpdateMental(10);
                break;
            case 809://
                PlayerCore.Instance.UpdateMental(30);
                break;
            case 810://
                PlayerCore.Instance.UpdateMental(60);
                break;
            // 이후 로직은 버프 디버프 시스템 만들어서 적용하는게 좋겠습니다.
            case 811: break;
            case 812: break;
            case 813://
                PlayerCore.Instance.StartDrunk();
                break;
            // 근데 이건 사실상 여부의 로직을 통해서 세팅하므로 외부 아이템 클릭을 통해 작동되지 않음
            case 814: break;//
            default:
                break;
        }

        yield return base.Use(userGameObject, itemWithState);

        


        InventoryUiMain.instance.IconRefresh();
    }

}
