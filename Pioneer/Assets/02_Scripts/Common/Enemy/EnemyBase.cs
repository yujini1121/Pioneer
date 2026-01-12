using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : CreatureBase, IBegin
{
    [Header("기본 속성")]
    protected float idleTime;
    // public GameObject targetObject;
    public GameObject currentAttackTarget;
    [SerializeField] protected float detectionRange;

    [Header("감지할 적 레이어")]
    [SerializeField] protected LayerMask detectMask;

    [Header("배 바닥 레이어")]
    [SerializeField] protected LayerMask groundLayer;

    // 바닥 확인 변수
    protected bool isOnGround = false;

    [Header("Attack Box 중심 오프셋 조정")]
    [SerializeField] private Vector3 attackBoxCenterOffset;


    [Header("마스트 게임오브젝트")]
    public GameObject mast;

    // ===== Animation (공통) =====
    [Header("애니메이션")]
    [SerializeField] protected AnimationSlot slots;
    [SerializeField] protected Animator animator;
    [SerializeField] protected string nextAnimTrigger = "SetIdle";
    [SerializeField] protected Vector3 lastMoveDirection = Vector3.back;

    private AnimatorOverrideController aoc;
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> overridesList = new();

    protected int _curIdleIdx = -1;
    protected int _curRunIdx = -1;
    protected int _curAttackIdx = -1;

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
        mast = GameObject.FindWithTag("Mast");
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

    #region 애니메이션
    protected void ChangeIdleByIndex(Vector3 dir)
    {
        int idx = PlayerCore.Get4DirIndex(dir);
        if (idx < 0) return;
        if (slots == null || slots.idle == null || idx >= slots.idle.Count) return;

        if (idx != _curIdleIdx)
        {
            ChangeAnimationClip(slots.curIdleClip, slots.idle[idx]);
            _curIdleIdx = idx;
        }

        nextAnimTrigger = "SetIdle";
    }

    protected void ChangeRunByIndex(Vector3 dir)
    {
        int idx = PlayerCore.Get4DirIndex(dir);
        if (idx < 0) return;
        if (slots == null || slots.run == null || idx >= slots.run.Count) return;

        if (idx != _curRunIdx)
        {
            ChangeAnimationClip(slots.curRunClip, slots.run[idx]);
            _curRunIdx = idx;
        }

        nextAnimTrigger = "SetRun";
    }

    protected void ChangeAttackByIndex(Vector3 dir)
    {
        int idx = PlayerCore.Get2DirIndex(dir);
        if (idx < 0) return;
        if (slots == null || slots.attack == null || idx >= slots.attack.Count) return;

        if (idx != _curAttackIdx)
        {
            ChangeAnimationClip(slots.curAttackClip, slots.attack[idx]);
            _curAttackIdx = idx;
        }

        animator.Play("Attack");
        nextAnimTrigger = "SetAttack";
    }

    protected void ApplyAnimTrigger()
    {
        if (animator == null) return;

        animator.ResetTrigger("SetIdle");
        animator.ResetTrigger("SetRun");
        animator.ResetTrigger("SetAttack");
        animator.SetTrigger(nextAnimTrigger);
    }

    // PlayerController의 ChangeAnimationClip 그대로 복붙, 정리 필요
    public void ChangeAnimationClip(AnimationClip oldAnim, AnimationClip newAnim)
    {
        if (aoc == null || oldAnim == null || newAnim == null) return;

        overridesList.Clear();
        aoc.GetOverrides(overridesList);

        for (int i = 0; i < overridesList.Count; i++)
        {
            var key = overridesList[i].Key;
            if (key != null && key == oldAnim)
            {
                if (overridesList[i].Value == newAnim) return;
                overridesList[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, newAnim);
                break;
            }
        }

        aoc.ApplyOverrides(overridesList);
        animator.Rebind();
        animator.Update(0f);
    }
    #endregion

    /// <summary>
    /// Debug
    /// </summary>
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