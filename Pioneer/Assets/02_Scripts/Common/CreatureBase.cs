using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CreatureBase : CommonBase
{
    public FOVController fov;   // 시야 컨트롤러 = 타겟 탐지용

    public float speed;
    public int attackDamage; // default value
    public float attackRange;
    public float attackDelayTime;

    // A small positional reaction; AI paths, attack timers and stun ownership stay intact.
    private NavMeshAgent knockbackAgent;
    private Rigidbody knockbackBody;
    private Vector3 knockbackDirection;
    private float knockbackElapsed;
    private float knockbackDistance;
    private const float KnockbackDuration = 0.12f;
    public bool IsKnockbackActive { get; private set; }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        bool wasAlive = !IsDead;
        base.TakeDamage(damage, attacker);
        if (wasAlive && damage > 0 && !IsDead) BeginHitKnockback(attacker);
        if (IsDead) IsKnockbackActive = false;
    }

    private void BeginHitKnockback(GameObject source)
    {
        if (source == null || !isActiveAndEnabled || Time.timeScale <= 0f
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        if (knockbackAgent == null) knockbackAgent = GetComponent<NavMeshAgent>();
        if (knockbackBody == null) knockbackBody = GetComponent<Rigidbody>();
        if (!CanApplyKnockback()) return;

        CreatureBase sourceCreature = source.GetComponentInParent<CreatureBase>();
        Vector3 origin = sourceCreature != null ? sourceCreature.transform.position : source.transform.position;
        Vector3 direction = transform.position - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        knockbackDirection = direction.normalized;
        knockbackDistance = this is PlayerCore ? 0.18f : (this is TitanAI ? 0.15f : 0.28f);
        knockbackElapsed = 0f;
        IsKnockbackActive = true;
        knockbackAgent.velocity = Vector3.zero;
        if (this is PlayerCore player) player.StopHorizontalMovement();
    }

    private bool CanApplyKnockback()
    {
        WindAirborne wind = GetComponent<WindAirborne>();
        return knockbackAgent != null && knockbackAgent.isActiveAndEnabled
            && knockbackAgent.isOnNavMesh && knockbackAgent.updatePosition
            && !knockbackAgent.isOnOffMeshLink && (wind == null || !wind.IsAirborne);
    }

    private void LateUpdate()
    {
        TickHitKnockback(Time.deltaTime);
    }

    private void TickHitKnockback(float deltaTime)
    {
        if (!IsKnockbackActive) return;
        if (IsDead || !isActiveAndEnabled || !CanApplyKnockback()
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive))
        {
            IsKnockbackActive = false;
            return;
        }
        if (deltaTime <= 0f) return;

        float previous = Mathf.Clamp01(knockbackElapsed / KnockbackDuration);
        knockbackElapsed = Mathf.Min(KnockbackDuration, knockbackElapsed + deltaTime);
        float current = knockbackElapsed / KnockbackDuration;
        // Ease out without a tween or a new coroutine on every hit.
        float step = ((1f - previous) * (1f - previous) - (1f - current) * (1f - current)) * knockbackDistance;
        Vector3 position = this is PlayerCore && knockbackBody != null ? knockbackBody.position : transform.position;
        var filter = new NavMeshQueryFilter { agentTypeID = knockbackAgent.agentTypeID, areaMask = knockbackAgent.areaMask };
        if (!NavMesh.SamplePosition(position, out NavMeshHit start, Mathf.Max(0.5f, Mathf.Abs(knockbackAgent.baseOffset) + 0.5f), filter)
            || new Vector2(start.position.x - position.x, start.position.z - position.z).sqrMagnitude > 0.01f)
        {
            IsKnockbackActive = false;
            return;
        }
        Vector3 destination = start.position + knockbackDirection * step;
        if (NavMesh.Raycast(start.position, destination, out NavMeshHit edge, filter))
        {
            float allowed = Mathf.Max(0f, Vector3.Distance(start.position, edge.position) - 0.02f);
            destination = start.position + knockbackDirection * Mathf.Min(step, allowed);
        }
        Vector3 offset = destination - start.position;
        offset.y = 0f;
        if (this is PlayerCore player && knockbackBody != null && !knockbackBody.isKinematic)
        {
            player.StopHorizontalMovement();
            knockbackBody.position = position + offset;
            knockbackAgent.nextPosition = knockbackBody.position;
        }
        else
        {
            // A zero manual velocity suspends forward integration for this short reaction.
            // No path or isStopped flag is changed; the agent resumes its existing path.
            knockbackAgent.velocity = Vector3.zero;
            knockbackAgent.Move(offset);
        }
        if (knockbackElapsed >= KnockbackDuration) IsKnockbackActive = false;
    }

    protected virtual void OnDisable()
    {
        IsKnockbackActive = false;
    }

    public void Start()
    {
        UtilityFunctions.Log($">> 게임오브젝트 {gameObject.name}의 CreatureBase.Start 호출됨");

        fov = GetComponent<FOVController>();
    }
}
