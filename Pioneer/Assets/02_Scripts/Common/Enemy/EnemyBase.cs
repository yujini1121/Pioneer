using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : CreatureBase, IBegin
{
    [Header("기본 속성")]
    protected float idleTime;
    // public GameObject targetObject;
    public GameObject currentAttackTarget;
    [SerializeField] protected float detectionRange;

    [Header("감지 대상 레이어")]
    [SerializeField] protected LayerMask detectMask;

    [Header("바닥 체크 레이어")]
    [SerializeField] protected LayerMask groundLayer;

    // 바닥 판정 상태
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
    private Vector3 defaultAnimatorScale;
    private bool hasDefaultAnimatorScale = false;
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> overridesList = new();

    protected int _curIdleIdx = -1;
    protected int _curRunIdx = -1;
    protected int _curAttackIdx = -1;

    /// <summary>
    /// 기본 속성 값을 설정합니다.
    /// </summary>
    protected virtual void SetAttribute()
    {

    }

    protected void InitializeAnimationSystem()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        if (aoc == null)
        {
            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                aoc = overrideController;
            }
            else
            {
                aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = aoc;
            }
        }

        overridesList.Clear();
        aoc.GetOverrides(overridesList);

        SyncSlotBaseClip(ref slots.curIdleClip, slots.idle);
        SyncSlotBaseClip(ref slots.curRunClip, slots.run);
        SyncSlotBaseClip(ref slots.curAttackClip, slots.attack);
    }

    /// <summary>
    /// 마스트를 공격 대상으로 설정합니다.
    /// </summary>
    protected GameObject SetMastTarget()
    {
        mast = GameObject.FindWithTag("Mast");
        return mast;
    }

    /// <summary>
    /// 공격 범위 안의 모든 콜라이더를 배열로 반환합니다.
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
    /// 바닥에 닿아있는지 검사합니다.
    /// </summary>
    /// <returns></returns>
    protected virtual bool CheckOnGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 2f, groundLayer))
        {
            if (!isOnGround)
                isOnGround = true;
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

        UpdateSpriteFacing(dir);

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

        UpdateSpriteFacing(dir);

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

        UpdateSpriteFacing(dir);

        animator.ResetTrigger("SetIdle");
        animator.ResetTrigger("SetRun");
        animator.ResetTrigger("SetAttack");
        animator.Play("Attack", 0, 0f);
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

    protected void UpdateSpriteFacing(Vector3 dir)
    {
        if (animator == null) return;

        Transform visualRoot = animator.transform;
        if (visualRoot == null) return;

        if (!hasDefaultAnimatorScale)
        {
            defaultAnimatorScale = visualRoot.localScale;
            hasDefaultAnimatorScale = true;
        }

        visualRoot.localScale = defaultAnimatorScale;
    }

    // PlayerController의 ChangeAnimationClip을 그대로 가져온 코드, 추후 정리 필요
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

        // Rebind() resets the whole state machine and can swallow short enemy attack states.
        // For enemy anims we only need the next state play to pick up the new override.
        if (animator != null)
            animator.Update(0f);
    }

    private void SyncSlotBaseClip(ref AnimationClip currentClip, List<AnimationClip> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return;

        if (ContainsOverrideKey(currentClip))
            return;

        for (int i = 0; i < overridesList.Count; i++)
        {
            AnimationClip key = overridesList[i].Key;
            if (key != null && candidates.Contains(key))
            {
                currentClip = key;
                return;
            }
        }
    }

    private bool ContainsOverrideKey(AnimationClip clip)
    {
        if (clip == null)
            return false;

        for (int i = 0; i < overridesList.Count; i++)
        {
            if (overridesList[i].Key == clip)
                return true;
        }

        return false;
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

        // OverlapBox와 동일한 크기의 박스를 그린다.
        Gizmos.DrawWireCube(Vector3.zero, halfBoxSize * 2); // halfSize * 2 = 전체 크기
    }
}
