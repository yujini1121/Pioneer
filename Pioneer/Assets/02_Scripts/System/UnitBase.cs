using UnityEngine;

public class UnitBase : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CommonBase commonBase;

    void Start()
    {
        // SpriteRenderer 찾기
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 부모에서 CommonBase 찾기
        commonBase = GetComponentInParent<CommonBase>();

        if (commonBase != null && spriteRenderer != null)
        {
            // CommonBase에 SpriteRenderer 전달
            commonBase.spriteRenderer = spriteRenderer;
            commonBase.InitializeHitEffect();
        }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;

        // 카메라의 전방을 XZ 평면으로 투영 → Y축만 도는 빌보드
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) return;
        fwd.Normalize();
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 0f, 0f);
    }
}