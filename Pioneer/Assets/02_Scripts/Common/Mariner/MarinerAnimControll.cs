using UnityEngine;
using UnityEngine.AI;

public class MarinerAnimControll : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer sprite;

    [Header("Move Tuning")]
    public float idleThreshold = 0.05f;
    public float damp = 0.08f;
    public bool invertX = false;
    public bool invertZ = false;

    [Header("Idle Pose Sprites")]
    public Sprite defaultIdleFront;
    public Sprite defaultIdleBack;
    public Sprite defaultIdleLeft;
    public Sprite defaultIdleRight;
    public Sprite zombieIdleFront;
    public Sprite zombieIdleBack;
    public Sprite zombieIdleLeft;
    public Sprite zombieIdleRight;

    private bool zombieMode = false;
    private bool firedZombieTrigger = false;
    private Vector2 lastMoveDir = new Vector2(0f, -1f);
    private bool forceIdlePose = false;

    // 공격 조준 고정
    private bool aimOverride = false;
    private Vector2 aimDir;

    //Animator Hashes
    static readonly int H_Attack = Animator.StringToHash("Attack");
    static readonly int H_IsAttacking = Animator.StringToHash("IsAttacking");
    static readonly int H_DirX = Animator.StringToHash("DirX");
    static readonly int H_DirZ = Animator.StringToHash("DirZ");
    static readonly int H_Speed = Animator.StringToHash("Speed");
    // ★ Fishing
    static readonly int H_FishingTrigger = Animator.StringToHash("FishingTrigger");
    static readonly int H_IsFishing = Animator.StringToHash("IsFishing");

    static readonly int H_IsZombie = Animator.StringToHash("isZombie");
    static readonly int H_ZombieAttack = Animator.StringToHash("ZombieAttack");
    static readonly int H_ZombieIsAttacking = Animator.StringToHash("ZombieIsAttacking");

    static readonly int S_DefaultRunFront = Animator.StringToHash("DefaultMariner_Idle_Front");
    static readonly int S_DefaultRunBack = Animator.StringToHash("Mariner_Idle_Back");
    static readonly int S_DefaultRunLeft = Animator.StringToHash("Mariner_Idle_Side_L");
    static readonly int S_DefaultRunRight = Animator.StringToHash("Mariner_Idle_Side_R");
    static readonly int S_ZombieRunFront = Animator.StringToHash("ZombieMariner_Idle_Front");
    static readonly int S_ZombieRunBack = Animator.StringToHash("ZombieMariner_Idle_Back");
    static readonly int S_ZombieRunLeft = Animator.StringToHash("ZombieMariner_Idle_Side_L");
    static readonly int S_ZombieRunRight = Animator.StringToHash("ZombieMariner_Idle_Side_R");

    private enum Facing
    {
        Front,
        Back,
        Left,
        Right
    }

    private Facing currentMoveFacing = Facing.Front;
    private bool wasMoving = false;

    public void SetZombieMode()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        zombieMode = true;
        wasMoving = false;
        animator.SetBool(H_IsZombie, true);
    }

    public void PlayZombieAttackOnce()
    {
        if (animator == null) return;
        if (animator.GetBool(H_ZombieIsAttacking)) return;

        wasMoving = false;
        forceIdlePose = false;
        animator.ResetTrigger(H_ZombieAttack);
        animator.SetTrigger(H_ZombieAttack);
        animator.SetBool(H_ZombieIsAttacking, true);
    }

    public void EndZombieAttack()
    {
        if (animator == null) return;
        animator.SetBool(H_ZombieIsAttacking, false);
    }
    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        if (sprite != null)
            sprite.flipX = false;
    }

    //Zombie 그대로 유지 
    public void SetZombieModeTrigger()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        if (!firedZombieTrigger)
        {
            firedZombieTrigger = true;
            animator.ResetTrigger("TriggerZombie");
            animator.SetTrigger("TriggerZombie");
        }
        zombieMode = true;
        wasMoving = false;
    }

    //공격 조준
    public void AimAtTarget(Vector3 targetPos, Transform self)
    {
        Vector3 w = (targetPos - self.position);
        w.y = 0f;
        if (w.sqrMagnitude < 0.0001f) w = self.forward;

        Vector2 d = new Vector2(w.x, w.z).normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) d = new Vector2(Mathf.Sign(d.x), 0);
        else d = new Vector2(0, Mathf.Sign(d.y));

        aimDir = d;
        lastMoveDir = Snap4Direction(d);
        aimOverride = true;
        forceIdlePose = false;
        wasMoving = false;

        SetDirection(aimDir.x, aimDir.y, 0f);
        animator.SetFloat(H_Speed, 0f);
    }

    public void ClearAim() => aimOverride = false;

    //공격 트리거
    public void PlayAttackOnce()
    {
        if (animator.GetBool(H_IsAttacking)) return;
        forceIdlePose = false;
        wasMoving = false;
        animator.ResetTrigger(H_Attack);
        animator.SetTrigger(H_Attack);
        animator.SetBool(H_IsAttacking, true);
    }
    public void EndAttack() => animator.SetBool(H_IsAttacking, false);
    public void AttackEnd() { EndAttack(); ClearAim(); }

    // 낚시 시작/종료
    public void StartFishing(Vector3 lookPoint, Transform self)
    {
        if (animator == null) return;

        // 바라볼 방향 스냅(L/R/Front/Back)
        Vector3 w = (lookPoint - self.position); w.y = 0f;
        if (w.sqrMagnitude < 0.0001f) w = self.right; // 기본 오른쪽
        Vector2 d = new Vector2(w.x, w.z).normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) d = new Vector2(Mathf.Sign(d.x), 0);
        else d = new Vector2(0, Mathf.Sign(d.y));

        aimDir = d;
        lastMoveDir = Snap4Direction(d);
        aimOverride = true;
        forceIdlePose = false;
        wasMoving = false;

        SetDirection(aimDir.x, aimDir.y, 0f);
        animator.SetFloat(H_Speed, 0f);

        // 상태 진입
        animator.ResetTrigger(H_FishingTrigger);
        animator.SetTrigger(H_FishingTrigger);
        animator.SetBool(H_IsFishing, true);
    }

    public void StopFishing()
    {
        if (animator == null) return;
        animator.SetBool(H_IsFishing, false); // 종료 조건 해제 → Idle로 복귀
        ClearAim();
    }
    public void EndFishingFromEvent()  // 애니메이션 이벤트에서 호출
    {
        StopFishing();
    }

    void Update()
    {
        if (animator == null) return;

        // 공격/낚시 중에는 이동 파라미터 갱신 금지
        if (aimOverride) return;
        if (animator.GetBool(H_IsAttacking)) return;
        if (animator.GetBool(H_IsFishing)) return;

        Vector3 v = agent ? agent.velocity : Vector3.zero;
        float dirX = invertX ? -v.x : v.x;
        float dirZ = invertZ ? -v.z : v.z;

        float speed = new Vector2(dirX, dirZ).magnitude;
        if (speed < idleThreshold)
        {
            animator.SetFloat(H_Speed, 0f);
            SetDirection(lastMoveDir.x, lastMoveDir.y, 0f);
            forceIdlePose = true;
            wasMoving = false;
            return;
        }

        Vector2 n = Snap4Direction(new Vector2(dirX, dirZ));
        Facing facing = ToFacing(n);
        lastMoveDir = n;
        forceIdlePose = false;
        animator.SetFloat(H_Speed, speed, damp, Time.deltaTime);
        SetDirection(n.x, n.y, damp);
        PlayMoveState(facing);
    }

    void LateUpdate()
    {
        if (!forceIdlePose || sprite == null || animator == null) return;
        if (animator.GetBool(H_IsAttacking) || animator.GetBool(H_IsFishing)) return;

        Sprite idleSprite = GetIdleSprite(lastMoveDir);
        if (idleSprite != null)
            sprite.sprite = idleSprite;

        sprite.flipX = false;
    }

    private void SetDirection(float dirX, float dirZ, float dampTime)
    {
        if (dampTime > 0f)
        {
            animator.SetFloat(H_DirX, dirX, dampTime, Time.deltaTime);
            animator.SetFloat(H_DirZ, dirZ, dampTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(H_DirX, dirX);
            animator.SetFloat(H_DirZ, dirZ);
        }

        if (sprite != null)
            sprite.flipX = false;
    }

    private static Vector2 Snap4Direction(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return new Vector2(0f, -1f);

        dir.Normalize();
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2(Mathf.Sign(dir.x), 0f);

        return new Vector2(0f, Mathf.Sign(dir.y));
    }

    private static Facing ToFacing(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) && Mathf.Abs(dir.x) > 0f)
            return dir.x < 0f ? Facing.Left : Facing.Right;

        return dir.y > 0f ? Facing.Back : Facing.Front;
    }

    private void PlayMoveState(Facing facing)
    {
        if (wasMoving && currentMoveFacing == facing)
            return;

        currentMoveFacing = facing;
        wasMoving = true;
        animator.CrossFadeInFixedTime(GetMoveStateHash(facing), 0.05f);
    }

    private int GetMoveStateHash(Facing facing)
    {
        if (zombieMode)
        {
            switch (facing)
            {
                case Facing.Back:
                    return S_ZombieRunBack;
                case Facing.Left:
                    return S_ZombieRunLeft;
                case Facing.Right:
                    return S_ZombieRunRight;
                default:
                    return S_ZombieRunFront;
            }
        }

        switch (facing)
        {
            case Facing.Back:
                return S_DefaultRunBack;
            case Facing.Left:
                return S_DefaultRunLeft;
            case Facing.Right:
                return S_DefaultRunRight;
            default:
                return S_DefaultRunFront;
        }
    }

    private Sprite GetIdleSprite(Vector2 dir)
    {
        bool useZombie = zombieMode;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) && Mathf.Abs(dir.x) > 0f)
        {
            if (dir.x < 0f)
                return useZombie && zombieIdleLeft != null ? zombieIdleLeft : defaultIdleLeft;

            return useZombie && zombieIdleRight != null ? zombieIdleRight : defaultIdleRight;
        }

        if (dir.y > 0f)
            return useZombie && zombieIdleBack != null ? zombieIdleBack : defaultIdleBack;

        return useZombie && zombieIdleFront != null ? zombieIdleFront : defaultIdleFront;
    }
}
