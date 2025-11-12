using System;
using System.Collections;
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
    private bool isCurrentAttacking = false;

    // 타겟 변수들
    // private GameObject mast;  // 기본 목표 (돛대)
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

        Debug.Log($"MinionAI currentAttackTarget & isTargetInAttackRange : {this.gameObject.name} {currentAttackTarget.name} & {isTargetInAttackRange}");

        if (CanCreateNest(isTargetInAttackRange))
        {
            CreateNest();
        }
        else if (CanAttack(isTargetInAttackRange))
        {
            StartCoroutine(AttackCoroutine());
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
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.GameOver);

            revengeTarget = attacker;
            Debug.Log($"{name}이(가) {attacker.name}에게 공격받아 타겟을 변경합니다!");
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        maxHp = hp;
        attackDamage = 2;
        attackRange = 2f;
        speed = 2f;
        detectionRange = 4f;
        attackDelayTime = 2f;
        idleTime = 2f;
        SetMastTarget();
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

        currentAttackTarget = mast;
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
        Vector3 direction = (currentAttackTarget.transform.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        return currentAttackTarget != null
            && isTargetInAttackRange
            && Time.time >= lastAttackTime + attackDelayTime
            && (!isCurrentAttacking);
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
        Debug.Log("DespawnAllEnemies CreateNest 둥지 낳음");
        GameManager.Instance.checkTotalNest++;
        isNestCreated = true;
    }

    // =============================================================
    // 공격 : 이미 공격이 가능함을 전제로 함. 공격 범위 안에 들어왔다는 뜻
    // =============================================================   
    private IEnumerator AttackCoroutine()
    {
        isCurrentAttacking = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(attackDelayTime);
        yield return Attack();
        agent.speed = speed;
        isCurrentAttacking = false;
    }

    private IEnumerator Attack()
    {
        Debug.Log("미니언 Attack 메서드 진입");
        if (currentAttackTarget == null)
            yield break;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_Minion);

        CommonBase targetBase = currentAttackTarget.GetComponent<CommonBase>();
        // Debug.Log($"MinionAI targetBase : {this.name}, {targetBase.name}");
        if (targetBase != null && !targetBase.IsDead)
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

        if (Vector3.Distance(agent.destination, destination) > 0.5f)
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
        Debug.Log(($"MinionAI IsTargetInColliders target : {target.name}"));
        foreach (var col in colliders)
        {
            Debug.Log(($"MinionAI IsTargetInColliders col : {col.name}"));
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
}