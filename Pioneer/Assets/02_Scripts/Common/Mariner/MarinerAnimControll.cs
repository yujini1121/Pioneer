using UnityEngine;
using UnityEngine.AI;

public class MarinerAnimControll : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer sprite;

    [Header("Tuning")]
    public float idleThreshold = 0.05f;
    public float damp = 0.08f;
    public bool invertX = false;
    public bool invertZ = false;

    private bool zombieMode = false;
    private bool firedTrigger = false;

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

    // ====== 좀비 전환 (기존) ======
    public void SetZombieModeTrigger()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        if (!firedTrigger)
        {
            firedTrigger = true;
            animator.ResetTrigger("TriggerZombie");
            animator.SetTrigger("TriggerZombie");
        }
        zombieMode = true; // 이동 파라미터 업데이트 중단
    }

    // ====== 공격 제어 (추가) ======
    // 공격 시작: Idle -> Attack 진입용
    public void PlayAttackOnce()
    {
        if (animator == null) return;
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");          // 전이: Trigger 하나만
        animator.SetBool("IsAttacking", true);  // 유지: 공격 중 상태
    }

    // 공격 종료: Attack -> Idle 복귀 허용
    public void EndAttack()
    {
        if (animator == null) return;
        animator.SetBool("IsAttacking", false);
    }

    public void AttackEndFromEvent()
    {
        EndAttack();
    }

    void Update()
    {
        if (zombieMode) return; // 좀비 전환 후 이동 파라미터 고정

        if (animator == null) return;

        Vector3 v = agent ? agent.velocity : Vector3.zero;
        float dirX = invertX ? -v.x : v.x;
        float dirZ = invertZ ? -v.z : v.z;

        float speed = new Vector2(dirX, dirZ).magnitude;
        Vector2 n = speed > 1e-4f ? new Vector2(dirX, dirZ).normalized : Vector2.zero;

        animator.SetFloat("Speed", speed, damp, Time.deltaTime);
        animator.SetFloat("DirX", n.x, damp, Time.deltaTime);
        animator.SetFloat("DirZ", n.y, damp, Time.deltaTime);

        if (sprite && speed >= idleThreshold && Mathf.Abs(n.x) > Mathf.Abs(n.y))
            sprite.flipX = (n.x < 0f);
    }
}

