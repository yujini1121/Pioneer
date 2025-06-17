using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [Header("Sprite 설정")]
    [SerializeField] private Transform spritePivot;    // SpriteRenderer가 달린 자식
    [SerializeField] private float flipThreshold = 0.5f;

    [Header("카메라 바라보기 설정")]
    [SerializeField] private float lookOffset = -7f;   // 카메라 위치에서 얼마나 위를 바라볼지

    #region Bounce 설정 다시 하고 싶으면 키기~~~ 1
    // [Header("Bounce 설정")]
    // [SerializeField] private float bounceHeight = 0.25f;
    // [SerializeField] private float bounceSpeed  = 5f;
    // private float baseY;
    #endregion

    private Transform cameraTransform;
    private Vector3 lastPosition;
    private Vector3 originalScale;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        lastPosition = transform.position;

        // SpritePivot의 원래 스케일 저장
        originalScale = spritePivot.localScale;

        #region Bounce 설정 다시 하고 싶으면 키기~~~ 2
        // baseY = spritePivot.localPosition.y;
        #endregion
    }

    private void Update()
    {
        // 1) 카메라 + 오프셋 바라보기 (Pitch + Yaw)
        if (cameraTransform != null)
        {
            Vector3 targetPos = cameraTransform.position + Vector3.up * lookOffset;
            transform.LookAt(targetPos);
        }

        // 2) 이동 방향 계산
        Vector3 moveDir = (transform.position - lastPosition) / Time.deltaTime;

        // 3) 좌우 Flip (원본 스케일 유지)
        if (Mathf.Abs(moveDir.x) > flipThreshold)
        {
            Vector3 s = originalScale;
            s.x = Mathf.Abs(originalScale.x) * (moveDir.x > 0 ? 1 : -1);
            spritePivot.localScale = s;
        }

        #region Bounce 설정 다시 하고 싶으면 키기~~~ 3
        /*
        // 이동 중일 때만 위아래로 통통 튀는 효과
        Vector3 spritePos = spritePivot.localPosition;
        if (Mathf.Abs(moveDir.x) > flipThreshold)
        {
            float jump = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
            spritePos.y = baseY + jump * bounceHeight;
        }
        else
        {
            spritePos.y = baseY;
        }
        spritePivot.localPosition = spritePos;
        */
        #endregion

        // 4) 현재 위치 저장
        lastPosition = transform.position;
    }
}
