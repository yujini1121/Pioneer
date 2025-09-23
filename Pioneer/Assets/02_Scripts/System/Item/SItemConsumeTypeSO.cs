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

    public override IEnumerable Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        itemWithState.isUseCoroutineEnd = false;

        SItemStack removeItem = new SItemStack(itemWithState.id, 1);

        InventoryManager.Instance.Remove(removeItem);

        switch (ConsumeEffect)
        {
            case 801:
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 15);
                break;
            case 802:
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 40);
                break;
            case 803:
                PlayerCore.Instance.hp = Mathf.Min
                    (PlayerCore.Instance.maxHp, PlayerCore.Instance.hp + 70);
                break;
            case 804:
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 10);
                break;
            case 805:
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 20);
                break;
            case 806:
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 40);
                break;
            case 807:
                PlayerCore.Instance.currentFullness = Mathf.Min
                    (PlayerCore.Instance.maxFullness, PlayerCore.Instance.currentFullness + 70);
                break;
            case 808:
                PlayerCore.Instance.currentMental = Mathf.Min
                    (PlayerCore.Instance.maxMental, PlayerCore.Instance.currentMental + 10);
                break;
            case 809:
                PlayerCore.Instance.currentMental = Mathf.Min
                    (PlayerCore.Instance.maxMental, PlayerCore.Instance.currentMental + 30);
                break;
            case 810:
                PlayerCore.Instance.currentMental = Mathf.Min
                    (PlayerCore.Instance.maxMental, PlayerCore.Instance.currentMental + 60);
                break;
            // 이후 로직은 버프 디버프 시스템 만들어서 적용하는게 좋겠습니다.
            case 811: break;
            case 812: break;
            case 813: break;
            // 근데 이건 사실상 여부의 로직을 통해서 세팅하므로 외부 아이템 클릭을 통해 작동되지 않음
            case 814: break;
            default:
                break;
        }

        yield return base.Use(userGameObject, itemWithState);
    }

}
