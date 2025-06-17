using UnityEngine;


[CreateAssetMenu(fileName = "InstallableObject", menuName = "ScriptableObjects/Installables/InstallableObjects")]
public class SInstallableObjectDataSO : SItemTypeSO
{
	[Header("설치 프리팹 및 설정")]
	public GameObject prefab;                  // 설치 대상 프리팹
	public Vector3 size = Vector3.one;         // 설치 판정용 Overlap 크기
	public float yOffset = 0f;                 // 설치 높이 조정값
											   // (바닥인지 오브젝트인지, 오브젝트 높이가 어느정도인지에 따라 다를 수 있음)
    [Header("기능 확장")]
	public float maxHp = 20f;                  // 내구도
	public float buildTime = 2f;               // 설치 시간

    [Header("머티리얼 설정")]
    public Material defaultMaterial;			// 설치 후 적용할 머티리얼
    public Material previewInvalidMat;			// 설치 불가능 시 머티리얼
    public Material previewValidMat;			// 설치 가능 시 머티리얼
}
