using UnityEngine;

public class UnitBase : MonoBehaviour, IBegin
{
    [Header("Sprite 설정")]
    [SerializeField] private Transform spritePivot;    // SpriteRenderer가 달린 자식
    [SerializeField] private float flipThreshold = 0.5f;

    [Header("카메라 바라보기 설정")]
    [SerializeField] private float lookOffset = -7f;   // 카메라 위치에서 얼마나 위를 바라볼지

    private Transform cameraTransform;
    private Vector3 lastPosition;
    private Vector3 originalScale;

    public void Init()
    {
        cameraTransform = Camera.main.transform;
        lastPosition = transform.position;

        // SpritePivot의 원래 스케일 저장
        Debug.Log($">> localScale = {spritePivot.localScale}");
        originalScale = spritePivot.localScale;
    }

    private void Update()
    {
        // 1) spritePivot만 카메라 정면을 바라보게
        if (cameraTransform != null && spritePivot != null)
        {
            spritePivot.forward = cameraTransform.forward;
        }

        // 2) 이동 방향 계산
        Vector3 moveDir = (transform.position - lastPosition) / Time.deltaTime;

        // 3) 좌우 Flip (원본 스케일 유지)
        if (Mathf.Abs(moveDir.x) > flipThreshold)
        {
            Vector3 s = originalScale;
            Debug.Log($">> s = {s}");
            s.x = Mathf.Abs(originalScale.x) * (moveDir.x > 0 ? -1 : 1);
            Debug.Log($">> s.x = {s.x}");
            spritePivot.localScale = s;
            Debug.Log($">> localScale = {spritePivot.localScale}");
        }

        // 4) 현재 위치 저장
        lastPosition = transform.position;
    }
}
