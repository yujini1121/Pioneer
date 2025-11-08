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

    // 공격 조준 고정
    private bool aimOverride = false;
    private Vector2 aimDir;

    // ===== Animator Hashes =====
    static readonly int H_Attack = Animator.StringToHash("Attack");
    static readonly int H_IsAttacking = Animator.StringToHash("IsAttacking");
    static readonly int H_DirX = Animator.StringToHash("DirX");
    static readonly int H_DirZ = Animator.StringToHash("DirZ");
    static readonly int H_Speed = Animator.StringToHash("Speed");
    // ★ Fishing
    static readonly int H_FishingTrigger = Animator.StringToHash("FishingTrigger");
    static readonly int H_IsFishing = Animator.StringToHash("IsFishing");

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

    // ===== Zombie 그대로 유지 =====
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
    }

    // ===== 공격 조준 =====
    public void AimAtTarget(Vector3 targetPos, Transform self)
    {
        Vector3 w = (targetPos - self.position);
        w.y = 0f;
        if (w.sqrMagnitude < 0.0001f) w = self.forward;

        Vector2 d = new Vector2(w.x, w.z).normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) d = new Vector2(Mathf.Sign(d.x), 0);
        else d = new Vector2(0, Mathf.Sign(d.y));

        aimDir = d;
        aimOverride = true;

        animator.SetFloat(H_DirX, aimDir.x);
        animator.SetFloat(H_DirZ, aimDir.y);
        animator.SetFloat(H_Speed, 0f);

        if (sprite && Mathf.Abs(aimDir.x) > Mathf.Abs(aimDir.y))
            sprite.flipX = (aimDir.x < 0);
    }

    public void ClearAim() => aimOverride = false;

    // ===== 공격 트리거 =====
    public void PlayAttackOnce()
    {
        if (animator.GetBool(H_IsAttacking)) return;
        animator.ResetTrigger(H_Attack);
        animator.SetTrigger(H_Attack);
        animator.SetBool(H_IsAttacking, true);
    }
    public void EndAttack() => animator.SetBool(H_IsAttacking, false);
    public void AttackEnd() { EndAttack(); ClearAim(); }

    // ===== ★ 낚시 시작/종료 =====
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
        aimOverride = true;

        animator.SetFloat(H_DirX, aimDir.x);
        animator.SetFloat(H_DirZ, aimDir.y);
        animator.SetFloat(H_Speed, 0f);

        if (sprite && Mathf.Abs(aimDir.x) > Mathf.Abs(aimDir.y))
            sprite.flipX = (aimDir.x < 0);

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
        if (zombieMode) return;
        if (animator == null) return;

        // 공격/낚시 중에는 이동 파라미터 갱신 금지
        if (aimOverride) return;
        if (animator.GetBool(H_IsAttacking)) return;
        if (animator.GetBool(H_IsFishing)) return;

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
