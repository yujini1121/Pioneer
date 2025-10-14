using UnityEngine;

public class EnemyBase : CreatureBase, IBegin
{
    [Header("기본 속성")]
    protected float idleTime;
    // public GameObject targetObject;
    public GameObject currentAttackTarget;
    protected float detectionRange;

    [Header("감지할 적 레이어")]
    [SerializeField] protected LayerMask detectMask;

    [Header("배 바닥 레이어")]
    [SerializeField] protected LayerMask groundLayer;

    // 바닥 확인 변수
    protected bool isOnGround = false;

    // 공격 박스 중심 오프셋 조정
    [SerializeField] private Vector3 attackBoxCenterOffset;

    /// <summary>
    /// 속성 변수에 값 할당
    /// </summary>
    protected virtual void SetAttribute()
    {

    }

    /// <summary>
    /// 돛대 타겟으로 설정
    /// </summary>
    protected GameObject SetMastTarget()
    {
        GameObject mast = GameObject.FindGameObjectWithTag("Mast");
        return mast;
    }

    /// <summary>
    /// 공격 범위 내 모든 콜라이더를 찾아 배열로 반환
    /// </summary>
    protected Collider[] DetectAttackRange()
    {
        Vector3 boxCenter = transform.position
            + transform.right * attackBoxCenterOffset.x
            + transform.forward * attackBoxCenterOffset.z
            + transform.up * attackBoxCenterOffset.y;
        Vector3 halfBoxSize = new Vector3(0.25f, 0.25f, attackRange / 2f);

        // Debug.Log($"DetectMask: {detectMask}, BoxCenter: {boxCenter}, HalfSize: {halfBoxSize}");

        return Physics.OverlapBox(boxCenter, halfBoxSize, transform.rotation, detectMask);
    }

    /// <summary>
    /// 배 플렛폼 위인지 검사
    /// </summary>
    /// <returns></returns>
    protected virtual bool CheckOnGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 2f, groundLayer))
        {
            if (!isOnGround)
            {
                isOnGround = true;
            }
        }
        else
        {
            isOnGround = false;
        }

        return isOnGround;
    }

    //================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // DetectAttackRange()와 동일하게 중심 계산
        // float debugAttackRange = 5f; // 확인용, 실제 테스트할 공격 범위
        Vector3 boxCenter = transform.position
            + transform.right * attackBoxCenterOffset.x
            + transform.forward * attackBoxCenterOffset.z
            + transform.up * attackBoxCenterOffset.y;

        Vector3 halfBoxSize = new Vector3(0.25f, 0.25f, attackRange / 2f);

        // 회전 적용
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;

        // OverlapBox와 동일한 크기의 박스 그리기
        Gizmos.DrawWireCube(Vector3.zero, halfBoxSize * 2); // halfSize * 2 = 전체 크기
    }
}