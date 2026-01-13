using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : EnemyBase, IBegin
{
    [Header("둥지 프리팹")]
    [SerializeField] private GameObject nestPrefab;

    // 네브 메시 
    private NavMeshAgent agent;

    // 둥지 관련 변수
    public bool isNestCreated = false;
    private float nestCool = 15f;
    private float nestCreationTime = -1f;

    // 타겟 변수들
    private GameObject revengeTarget;   // 나를 공격한 적
    // 최종 목표 : currentAttackTarget

    private float attackTimer = 0f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();

        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = 0.8f;
        }
    }

    void Update()
    {
        if (!CheckOnGround())
            return;

        float dt = Time.deltaTime;

        // 공격 쿨타임이어도 애니메이션 트리거는 계속 갱신(안 그러면 미니언이 멈춘 것처럼 보일 수 있음)
        if (attackTimer > 0f)
        {
            attackTimer -= dt;
            ChangeIdleByIndex(lastMoveDirection);
            ApplyAnimTrigger();

            // 쿨타임 끝나면 다시 이동 허용
            if (attackTimer <= 0f && agent != null) agent.isStopped = false;
            return;
        }

        fov.DetectTargets(detectMask);

        // 공격할 타겟 설정
        UpdateTarget();

        Collider[] targetsInAttackRange = DetectAttackRange();
        bool isTargetInAttackRange = currentAttackTarget != null && IsTargetInColliders(currentAttackTarget, targetsInAttackRange);

        if (CanCreateNest(isTargetInAttackRange))
        {
            CreateNest();
            ChangeIdleByIndex(lastMoveDirection);
        }
        else if (CanAttack(isTargetInAttackRange))
        {
            Attack();
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

        //Debug.DrawRay(transform.position + Vector3.up * 0.2f, lastMoveDirection, Color.cyan);
        //Debug.Log($"lastMoveDirection={lastMoveDirection} 4Dir={PlayerCore.Get4DirIndex(lastMoveDirection)}");
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        base.TakeDamage(damage, attacker);

        if (attacker != null && !IsDead)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.GameOver);

            revengeTarget = attacker;
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        maxHp = hp;
        attackDamage = 5;
        attackRange = 2f;
        speed = 2f;
        detectionRange = 5f;
        attackDelayTime = 2f;
        idleTime = 2f;

        currentAttackTarget = PlayerCore.Instance.gameObject;
        fov.viewRadius = detectionRange;
    }

    private void UpdateTarget()
    {
        if (revengeTarget != null)
        {
            CommonBase targetBase = revengeTarget.GetComponent<CommonBase>();

            if (targetBase != null && !targetBase.IsDead && fov.visibleTargets.Contains(revengeTarget.transform))
            {
                currentAttackTarget = revengeTarget;
                return;
            }
            else
            {
                revengeTarget = null;
            }
        }

        Transform closestTarget = FindClosestTargetInDetect(fov.visibleTargets);
        if (closestTarget != null)
        {
            currentAttackTarget = closestTarget.gameObject;
            return;
        }

        currentAttackTarget = null;
    }

    // =============================================================
    // 행동 조건
    // =============================================================
    private bool CanCreateNest(bool isAttackable)
    {
        return isOnGround
            && !isNestCreated
            && Time.time >= nestCreationTime
            && nestCreationTime != -1f
            && !isAttackable
            && GameManager.Instance.LimitsNest();
    }

    private bool CanAttack(bool isTargetInAttackRange)
    {
        if (currentAttackTarget == null)
            return false;

        Vector3 direction = (currentAttackTarget.transform.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        return currentAttackTarget != null
            && isTargetInAttackRange
            && attackTimer <= 0f;
    }

    private bool CanMove()
    {
        return currentAttackTarget != null;
    }

    // =============================================================
    // 둥지 생성
    // =============================================================
    void CreateNest()
    {
        Instantiate(nestPrefab, new Vector3(transform.position.x, 0, transform.position.z), Quaternion.identity);
        GameManager.Instance.checkTotalNest++;
        isNestCreated = true;
    }

    // =============================================================
    // 공격 : 이미 공격이 가능함을 전제로 함. 공격 범위 안에 들어왔다는 뜻
    // =============================================================
    private void Attack()
    {
        // 공격 시작해도 Run 애니메이션으로 이동하는 걸 막기 위함
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 공격 방향 갱신 (좌/우 2프레임)
        if (currentAttackTarget != null)
        {
            Vector3 look = currentAttackTarget.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) lastMoveDirection = look.normalized;
        }

        ChangeAttackByIndex(lastMoveDirection);

        if (currentAttackTarget == null)
            return;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_Minion);

        CommonBase targetBase = currentAttackTarget.GetComponent<CommonBase>();
        if (targetBase != null && !targetBase.IsDead)
        {
            targetBase.TakeDamage(attackDamage, this.gameObject);
        }

        attackTimer = attackDelayTime;
    }

    // =============================================================
    // 이동
    // =============================================================
    void Move()
    {
        if (currentAttackTarget == null)
            return;

        agent.isStopped = false;

        Vector3 destination = currentAttackTarget.GetComponent<Collider>().ClosestPoint(transform.position);

        if (Vector3.Distance(agent.destination, destination) > 0.5f)
        {
            agent.SetDestination(destination);
        }
    }


    // =============================================================
    // 유틸리티 메서드
    // =============================================================

    private Transform FindClosestTargetInDetect(List<Transform> targets)
    {
        if (targets.Count == 0)
            return null;

        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var t in targets)
        {
            float dist = Vector3.Distance(transform.position, t.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = t;
            }
        }
        return closest;
    }

    private bool IsTargetInColliders(GameObject target, Collider[] colliders)
    {
        foreach (var col in colliders)
        {
            if (col.gameObject == target)
            {
                return true;
            }
        }
        return false;
    }

    protected override bool CheckOnGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 2f, groundLayer))
        {
            if (!isOnGround)
            {
                nestCool = UnityEngine.Random.Range(5f, 15f);
                nestCreationTime = Time.time + nestCool;
                isOnGround = true;
            }
        }
        else
        {
            isOnGround = false;
        }

        return isOnGround;
    }

    // ---------------- 애니메이션 유틸 ----------------

    private void UpdateLocomotionAnim()
    {
        if (agent == null) return;

        Vector3 v = agent.desiredVelocity;
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
