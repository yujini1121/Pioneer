using System.Collections.Generic;
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

    // 공격 관련 변수
    private float lastAttackTime = 0f;

    // 타겟 변수들
    private GameObject mastTarget;  // 기본 목표 (돛대)
    private GameObject revengeTarget;   // 나를 공격한 적
    // 최종 목표 : currentAttackTarget

    void Start()
    {
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

        fov.DetectTargets(detectMask);

        // 공격할 타겟 설정
        UpdateTarget();

        Collider[] targetsInAttackRange = DetectAttackRange();
        bool isTargetInAttackRange = currentAttackTarget != null && IsTargetInColliders(currentAttackTarget, targetsInAttackRange);

        if (CanCreateNest(isTargetInAttackRange))
        {
            CreateNest();
        }
        else if (CanAttack(isTargetInAttackRange))
        {
            Attack();
        }
        else if (CanMove())
        {
            Move();
        }
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        base.TakeDamage(damage, attacker);

        if (attacker != null && !IsDead)
        {
            revengeTarget = attacker;
            Debug.Log($"{name}이(가) {attacker.name}에게 공격받아 타겟을 변경합니다!");
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        attackDamage = 1;
        speed = 2f;
        detectionRange = 4f;
        attackDelayTime = 2f;
        idleTime = 2f;
        mastTarget = SetMastTarget();
        fov.viewRadius = detectionRange;
    }

    private void UpdateTarget()
    {
        if(revengeTarget != null)
        {
            CommonBase targetBase = revengeTarget.GetComponent<CommonBase>();
            
            if(targetBase != null && !targetBase.IsDead && fov.visibleTargets.Contains(revengeTarget.transform))
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
        if(closestTarget != null)
        {
            currentAttackTarget = closestTarget.gameObject;
            return;
        }

        currentAttackTarget = mastTarget;
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
            && !isAttackable;
    }

    private bool CanAttack(bool isTargetInAttackRange)
    {
        return currentAttackTarget != null && isTargetInAttackRange && Time.time >= lastAttackTime + attackDelayTime; ;
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
        Instantiate(nestPrefab, transform.position, Quaternion.identity);
        isNestCreated = true;
    }

    // =============================================================
    // 공격
    // =============================================================   
    void Attack()
    {
        if (currentAttackTarget == null)
            return;

        agent.isStopped = true;
        transform.LookAt(currentAttackTarget.transform);

        CommonBase targetBase = currentAttackTarget.GetComponent<CommonBase>();
        if(targetBase != null && !targetBase.IsDead)
        {
            targetBase.TakeDamage(attackDamage, this.gameObject);
            lastAttackTime = Time.time;
        }
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

        if(Vector3.Distance(agent.destination, destination) > 0.5f)
        {
            agent.SetDestination(destination);
        }
    }


    // =============================================================
    // 유틸리티 메서드
    // =============================================================

    /// <summary>
    /// 가장 가까운 타겟을 찾음
    /// </summary>
    /// <param name="targets"></param>
    /// <returns></returns>
    private Transform FindClosestTargetInDetect(List<Transform> targets)
    {
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

    /// <summary>
    /// 배 플렛폼 위인지 검사
    /// </summary>
    /// <returns></returns>
    protected override bool CheckOnGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 2f, groundLayer))
        {
            if (!isOnGround)
            {
                nestCool = Random.Range(5f, 15f);
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
}