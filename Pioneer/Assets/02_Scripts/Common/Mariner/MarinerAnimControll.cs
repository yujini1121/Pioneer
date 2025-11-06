using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MarinerAnimControll : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer sprite; // 선택항목

    [Header("Tuning")]
    public float idleThreshold = 0.05f;
    public float damp = 0.08f;
    public bool invertX = false;
    public bool invertZ = false;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // 애니메이터만은 반드시 캐싱 (감염/좀비 전환 후에도 동일 계층에서 찾게)
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        // 스프라이트는 선택사항(없어도 동작)
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(true);

        if (!animator)
            Debug.LogError($"[MarinerAnimControll] Animator 못 찾음: {name}");
    }

    void Update()
    {
        if (!animator) return;

        Vector3 v = agent ? agent.velocity : Vector3.zero;
        float dirX = invertX ? -v.x : v.x;
        float dirZ = invertZ ? -v.z : v.z;

        float speed = new Vector2(dirX, dirZ).magnitude;
        Vector2 n = speed > 1e-4f ? new Vector2(dirX, dirZ).normalized : Vector2.zero;

        animator.SetFloat("Speed", speed, damp, Time.deltaTime);
        animator.SetFloat("DirX", n.x, damp, Time.deltaTime);
        animator.SetFloat("DirZ", n.y, damp, Time.deltaTime);

        // flip은 스프라이트가 있을 때만
        if (sprite && speed >= idleThreshold && Mathf.Abs(n.x) > Mathf.Abs(n.y))
            sprite.flipX = (n.x < 0f);
    }
}
