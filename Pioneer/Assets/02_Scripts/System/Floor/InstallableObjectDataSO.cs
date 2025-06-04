using UnityEngine;

public enum InstallableType 
{ 
	Floor, 
	Object 
}

[CreateAssetMenu(fileName = "InstallableObject", menuName = "Installables/InstallableObject")]
public class InstallableObjectDataSO : ScriptableObject // 아이템 타입 상속, 머지 이후 할 것
{
	public InstallableType type; 
	public GameObject prefab;               
	public Vector3 size = Vector3.one;
	// 바닥인지, 오브젝트인지에 따라 y축 오프셋이 다를 수 있음을 고려하는 변수
	public float yOffset = 0f;
	static readonly public Color defaultColor = Color.white;
	static readonly public Color validColor = Color.green;
	static readonly public Color invalidColor = Color.red;
}
