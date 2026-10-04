using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CrawlerAI : EnemyBase, IBegin
{
    // 네비 메시
    private NavMeshAgent agent;

    // 감지된 오브젝트를 가까운 순서로 정렬한 리스트
    List<Transform> sortedTarget;

    private int closeTarget = 0;
    private GameObject revengeTarget;
    private bool isAttack = false;
    private float attackTimer = 0f;
    private const float AttackHitDelay = 0.2f;
    private const float AttackRecoveryDelay = 0.8f;

    private StunHandler stunHandler;
    private float originalSpeed;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        stunHandler = GetComponent<StunHandler>();
        if (TryGetComponent(out Rigidbody body))
        {
            body.useGravity = false;
            body.isKinematic = true;
        }
    }

    void Start()
    {
        base.Start();
        InitializeAnimationSystem();
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();
        ApplySpawnScaling();
        if (agent != null) agent.speed = speed;

        originalSpeed = speed;

        if (OceanEventManager.instance != null && OceanEventManager.instance.currentEvent is OceanEventThunder)
        {
            ApplyThunderSpeedModifier(0.8f);
        }
    }

    void Update()
    {
        if (IsDead || Time.timeScale <= 0f || fov == null) return;
        if (stunHandler != null && stunHandler.IsStunned)
            return;

        float dt = Time.deltaTime;
        // 공격 쿨타임에도 애니메이션 트리거는 계속 갱신해서 멈춘 것처럼 보이지 않게 유지
        // 공격 쿨타임 중에도 애니메이션 트리거를 갱신해야 멈춘 것처럼 보이지 않는다.
        if (attackTimer > 0f)
        {
            attackTimer -= dt;
            if (!isAttack)
            {
                ChangeIdleByIndex(lastMoveDirection);
                ApplyAnimTrigger();
            }

            // 쿨타임이 끝나면 다시 이동 허용
            if (attackTimer <= 0f && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = false;
            return;
        }

        fov.DetectTargets(detectMask);

        if (fov.visibleTargets.Count == 0)
        {
            currentAttackTarget = SetMastTarget();
        }

        if (CanAttack())
        {
            Attack();
            return;
        }
        else if (CanMove())
        {
            Move();
            UpdateLocomotionAnim();
        }
        else
        {
            ChangeIdleByIndex(lastMoveDirection);
        }

        ApplyAnimTrigger();
    }

    // 기본 세팅
    protected override void SetAttribute()
    {
        maxHp = 50;
        hp = maxHp;
        attackDamage = 10;
        speed = 1;
        fov.viewRadius = 4;
        attackRange = 2;
        attackDelayTime = 3;
    }

    private bool CanMove()
    {
        return fov.visibleTargets.Any(target => target != null && detectMask == (detectMask | (1 << target.gameObject.layer)))
               || currentAttackTarget != null;
    }

    private bool CanAttack()
    {
        if (attackTimer > 0f || isAttack) return false;

        Vector3 origin = GetAttackOrigin();
        Collider[] nearby = Physics.OverlapSphere(origin, attackRange, detectMask, QueryTriggerInteraction.Ignore);
        CommonBase nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider hit in nearby)
        {
            CommonBase candidate = hit.GetComponentInParent<CommonBase>();
            if (candidate == null || candidate == this || candidate.IsDead) continue;
            float distance = (hit.ClosestPoint(origin) - origin).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }
        if (nearest == null) return false;

        currentAttackTarget = nearest.gameObject;
        Vector3 direction = nearest.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f) lastMoveDirection = direction.normalized;
        return DetectCrawlerAttackRange().Any(hit => hit.GetComponentInParent<CommonBase>() == nearest);
    }

    private Vector3 GetAttackOrigin()
    {
        Vector3 origin = transform.position;
        if (TryGetComponent(out CapsuleCollider body)) origin.y = body.bounds.min.y + 1f;
        return origin;
    }

    private Collider[] DetectCrawlerAttackRange()
    {
        Vector3 direction = lastMoveDirection;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        Vector3 center = GetAttackOrigin() + direction * (attackRange * 0.5f);
        return Physics.OverlapBox(center, new Vector3(0.65f, 1.25f, attackRange * 0.5f),
            Quaternion.LookRotation(direction), detectMask, QueryTriggerInteraction.Ignore);
    }

    private void Move()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        if (fov.visibleTargets.Count > 0)
        {
            SortCloseObj();
            currentAttackTarget = sortedTarget.Count > closeTarget ? sortedTarget[closeTarget].gameObject : null;
        }

        if (currentAttackTarget == null) return;

        Collider targetCollider = currentAttackTarget.GetComponent<Collider>();
        if (targetCollider == null) return;
        Vector3 destination = targetCollider.ClosestPoint(transform.position);
        if (Vector3.Distance(agent.destination, destination) > 0.1f)
        {
            agent.SetDestination(destination);
        }
    }

    private void Attack()
    {
        if (isAttack)
            return;

        isAttack = true;

        // 공격 시작 시 Run 쪽으로 섞여 들어가는 것을 방지
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 공격 방향을 타겟 쪽으로 갱신 (좌우 2방향 사용)
        if (currentAttackTarget != null)
        {
            Vector3 look = currentAttackTarget.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) lastMoveDirection = look.normalized;
        }

        ChangeAttackByIndex(lastMoveDirection);
        AudioManager.instance?.PlaySfx(AudioManager.SFX.BeforeAttack_Crawler);
        StartCoroutine(AttackSequence());
        attackTimer = attackDelayTime;
    }

    private IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(AttackHitDelay);

        if (IsDead || (stunHandler != null && stunHandler.IsStunned)
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive))
        {
            isAttack = false;
            yield break;
        }
        AudioManager.instance?.PlaySfx(AudioManager.SFX.AfterAttack_Crawler);
        Collider[] hitColliders = DetectCrawlerAttackRange();
        var damaged = new HashSet<CommonBase>();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            GameObject currentObject = hitColliders[i].gameObject;
            CommonBase targetBase = currentObject.GetComponentInParent<CommonBase>();

            if (targetBase == null || !damaged.Add(targetBase)) continue;

            if (targetBase.IsDead)
            {
                if (fov.visibleTargets.Count > 0)
                {
                    SortCloseObj();
                    currentAttackTarget = sortedTarget.Count > 0 ? sortedTarget[0].gameObject : null;
                }
                continue;
            }

            targetBase.TakeDamage(attackDamage, this.gameObject);
        }

        yield return new WaitForSeconds(AttackRecoveryDelay);
        isAttack = false;
    }

    private void SortCloseObj()
    {
        sortedTarget = fov.visibleTargets
            .Where(target => target != null)
            .OrderBy(target => Vector3.Distance(transform.position, target.transform.position))
            .ToList();
    }

    public void ApplyThunderSpeedModifier(float multiplier)
    {
        speed = originalSpeed * multiplier;

        if (agent != null)
            agent.speed = speed;
    }

    public void ResetThunderSpeedModifier()
    {
        speed = originalSpeed;

        if (agent != null)
            agent.speed = speed;
    }

    // ---------------- 애니메이션 보조 ----------------

    private void UpdateLocomotionAnim()
    {
        if (agent == null) return;

        Vector3 v = agent.velocity;
        v.y = 0f;

        if (v.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = v.normalized;
            ChangeRunByIndex(lastMoveDirection);
        }
        else
        {
            ChangeIdleByIndex(lastMoveDirection);
        }
    }
}
