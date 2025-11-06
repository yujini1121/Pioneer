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

    private bool zombieMode = false;     // 전이 이후 일반 파라미터 덮어쓰기 금지
    private bool firedTrigger = false;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    /// <summary>
    /// 좀비 전환: 트리거 방식
    /// </summary>
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
        zombieMode = true;
    }

    void Update()
    {
        if (zombieMode) return;

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
