using UnityEngine;


[CreateAssetMenu(fileName = "InstallableObject", menuName = "ScriptableObjects/Installables/InstallableObjects")]
public class SInstallableObjectDataSO : SItemTypeSO
{
	[Header("설치 프리팹 및 설정")]
	public GameObject prefab;                  // 설치 대상 프리팹
	public Vector3 size = Vector3.one;         // 설치 판정용 Overlap 크기

    [Header("기능 확장")]
	public float maxHp = 20f;                  // 내구도
	public float buildTime = 2f;               // 설치 시간
}
