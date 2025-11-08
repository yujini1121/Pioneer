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

    private bool zombieMode = false;
    private bool firedZombieTrigger = false;

    private bool aimOverride = false;
    private Vector2 aimDir;

    // Animator Hashes
    static readonly int H_Attack = Animator.StringToHash("Attack");
    static readonly int H_IsAttacking = Animator.StringToHash("IsAttacking");
    static readonly int H_DirX = Animator.StringToHash("DirX");
    static readonly int H_DirZ = Animator.StringToHash("DirZ");
    static readonly int H_Speed = Animator.StringToHash("Speed");

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
    }

    //기존 좀비 전환 기능 유지

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

        // 이동 파라미터 업데이트 중단
        zombieMode = true;
    }
    // 공격 방향 스냅

    public void AimAtTarget(Vector3 targetPos, Transform self)
    {
        Vector3 w = (targetPos - self.position);
        w.y = 0f;
        if (w.sqrMagnitude < 0.0001f) w = self.forward;

        Vector2 d = new Vector2(w.x, w.z).normalized;

        // Axis Snap (대각선 → 큰 축 방향으로 고정)
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
            d = new Vector2(Mathf.Sign(d.x), 0);
        else
            d = new Vector2(0, Mathf.Sign(d.y));

        aimDir = d;
        aimOverride = true;

        animator.SetFloat(H_DirX, aimDir.x);
        animator.SetFloat(H_DirZ, aimDir.y);
        animator.SetFloat(H_Speed, 0f);

        if (sprite && Mathf.Abs(aimDir.x) > Mathf.Abs(aimDir.y))
            sprite.flipX = (aimDir.x < 0);
    }

    public void ClearAim() => aimOverride = false;

    // 공격 트리거 + 상태 플래그

    public void PlayAttackOnce()
    {
        if (animator.GetBool(H_IsAttacking)) return; // 재트리거 방지

        animator.ResetTrigger(H_Attack);
        animator.SetTrigger(H_Attack);
        animator.SetBool(H_IsAttacking, true);
    }

    public void EndAttack() => animator.SetBool(H_IsAttacking, false);

    // 애니메이션 이벤트에서 호출
    public void AttackEnd()
    {
        EndAttack();
        ClearAim();
    }

    // 이동/Idle 애니메이션 업데이트
    void Update()
    {
        if (zombieMode) return;     // 좀비 전환시 이동 애니메이션 멈춤
        if (aimOverride) return;    // 공격 중에는 방향 유지
        if (animator != null && animator.GetBool(H_IsAttacking)) return;
        if (animator == null) return;

        Vector3 v = agent ? agent.velocity : Vector3.zero;
        float dirX = invertX ? -v.x : v.x;
        float dirZ = invertZ ? -v.z : v.z;

        float speed = new Vector2(dirX, dirZ).magnitude;
        Vector2 n = speed > 0.0001f ? new Vector2(dirX, dirZ).normalized : Vector2.zero;

        animator.SetFloat(H_Speed, speed, damp, Time.deltaTime);
        animator.SetFloat(H_DirX, n.x, damp, Time.deltaTime);
        animator.SetFloat(H_DirZ, n.y, damp, Time.deltaTime);

        if (sprite && speed >= idleThreshold && Mathf.Abs(n.x) > Mathf.Abs(n.y))
            sprite.flipX = (n.x < 0f);
    }
}
