using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "InstallableObject", menuName = "ScriptableObjects/Installables/InstallableObjects")]
public class SInstallableObjectDataSO : SItemTypeSO
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable, Ballista, Trap, Lantern, Storage }

    [Header("설치 타입")]
    public CreationType installType;

    [Header("설치 프리팹 및 설정")]
    public GameObject prefab;                  // 설치 대상 프리팹
    public Vector3 size = Vector3.one;         // 설치 판정용 Overlap 크기

    [Header("기능 확장")]
    public int maxHp = 20;                     // 내구도
    public float buildTime = 2f;               // 설치 시간

    public override IEnumerator Use(CommonBase userGameObject, SItemStack itemWithState)
    {
        if (itemWithState == null || itemWithState.amount < 1)
        {
            yield break;
        }

        itemWithState.isUseCoroutineEnd = false;

        SItemStack[] installCost = new SItemStack[]
        {
            new SItemStack(itemWithState.id, 1, itemWithState.duability)
        };

        CreateObject.instance.EnterInstallMode(this, installCost);

        yield return null;
        itemWithState.isUseCoroutineEnd = true;
    }
}
