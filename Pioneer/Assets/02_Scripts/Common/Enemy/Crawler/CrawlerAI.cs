using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CrawlerAI : EnemyBase, IBegin
{
    // 네브 메시
    private NavMeshAgent agent;

    // 감지된 오브젝트 가까운 순으로 정렬할 리스트
    List<Transform> sortedTarget;

    private int closeTarget = 0;
    private GameObject revengeTarget;
    private bool isAttack = false;
    private float attackTimer = 0f;

    private StunHandler stunHandler;
    private float originalSpeed;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        stunHandler = GetComponent<StunHandler>();
    }

    void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();
        if (agent != null) agent.speed = speed;

        originalSpeed = speed;

        if (OceanEventManager.instance != null && OceanEventManager.instance.currentEvent is OceanEventThunder)
        {
            ApplyThunderSpeedModifier(0.8f);
        }
    }

    void Update()
    {
        if (stunHandler != null && stunHandler.IsStunned)
            return;

        float dt = Time.deltaTime;

        // 공격 쿨타임이어도 애니메이션 트리거는 계속 갱신(안 그러면 크롤러가 멈춘 것처럼 보일 수 있음)
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

        if (fov.visibleTargets.Count == 0)
        {
            currentAttackTarget = SetMastTarget();
        }

        if (CanAttack())
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

        Debug.DrawRay(transform.position + Vector3.up * 0.2f, lastMoveDirection, Color.cyan);
        Debug.Log($"lastMoveDirection={lastMoveDirection} 4Dir={PlayerCore.Get4DirIndex(lastMoveDirection)}");

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
        return fov.visibleTargets.Any(target => detectMask == (detectMask | (1 << target.gameObject.layer)))
               || currentAttackTarget != null;
    }

    private bool CanAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, detectMask);
        return hitColliders.Length > 0 && attackTimer <= 0f;
    }

    private void Move()
    {
        if (fov.visibleTargets.Count > 0)
        {
            SortCloseObj();
            currentAttackTarget = sortedTarget[closeTarget].gameObject;
        }

        if (currentAttackTarget == null) return;

        Vector3 destination = currentAttackTarget.GetComponent<Collider>().ClosestPoint(transform.position);
        if (Vector3.Distance(agent.destination, destination) > 0.1f)
        {
            agent.SetDestination(destination);
        }
    }

    private void Attack()
    {
        //공격 시작하면 Run으로 이동하는 경로를 끊어버림 (desiredVelocity로 Run 트리거 나가는 것 방지)
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 공격 방향을 타겟 쪽을 바라보게 갱신 (에너미는 좌, 우 2프레임!!)
        if (currentAttackTarget != null)
        {
            Vector3 look = currentAttackTarget.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-6f) lastMoveDirection = look.normalized;
        }

        ChangeAttackByIndex(lastMoveDirection);

        Collider[] hitColliders = DetectAttackRange();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            GameObject currentObject = hitColliders[i].gameObject;
            CommonBase targetBase = currentObject.GetComponent<CommonBase>();

            if (targetBase == null) continue;

            if (targetBase.IsDead)
            {
                if (fov.visibleTargets.Count > 0)
                {
                    SortCloseObj();
                    currentAttackTarget = fov.visibleTargets[closeTarget].gameObject;
                }
                return;
            }

            targetBase.TakeDamage(attackDamage, this.gameObject);
        }

        attackTimer = attackDelayTime;
    }

    private void SortCloseObj()
    {
        sortedTarget = fov.visibleTargets
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