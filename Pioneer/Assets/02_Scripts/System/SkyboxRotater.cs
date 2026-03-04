using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private Material material; // 런타임 전용(원본 복제함) 머티리얼 참조하기 
    [SerializeField] private float degreesPerSecond = 2f;

    static readonly int RotationID = Shader.PropertyToID("_Rotation");

    void Update()
    {
        float rot = material.GetFloat(RotationID);
        rot = (rot + degreesPerSecond * Time.deltaTime) % 360f;
        material.SetFloat(RotationID, rot);
    }
}