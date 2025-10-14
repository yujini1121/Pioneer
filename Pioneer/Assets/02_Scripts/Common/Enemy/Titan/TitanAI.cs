using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TitanAI : EnemyBase, IBegin
{
    private NavMeshAgent agent;
    private GameObject mastTarget;

    [Header("타이탄 전용 속성")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float lungeDuration = 0.2f;

    private bool isAttacking = false;

    #region Unity 기본 함수 (Start, Update)

    void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        mastTarget = SetMastTarget();

        SetAttribute();
        currentAttackTarget = mastTarget; // 초기 목표는 돛대
    }

    void Update()
    {
        if (isAttacking || IsDead) return;

        UpdateTarget();

        if (CanAttack())
        {
            StartCoroutine(AttackRoutine());
        }
        else
        {
            Move();
        }
    }

    #endregion

    #region AI 행동 함수 (Move, Attack, Detect)

    /// <summary>
    ///  AI의 현재 목표를 결정하는 새로운 함수
    /// </summary>
    private void UpdateTarget()
    {
        // 타겟이 파괴되었거나 비활성화되었는지 확인
        if (currentAttackTarget == null || !currentAttackTarget.activeInHierarchy)
        {
            currentAttackTarget = mastTarget;
        }

        fov.DetectTargets(detectMask);

        GameObject closestObstacle = null;
        float minDistance = float.MaxValue;

        // 시야에 보이는 모든 타겟 중 가장 가까운 장애물을 찾음
        foreach (Transform target in fov.visibleTargets)
        {
            if (target.gameObject != mastTarget)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestObstacle = target.gameObject;
                }
            }
        }

        // 가장 가까운 장애물이 있으면 그놈을 목표로, 없으면 돛대를 목표로 설정
        if (closestObstacle != null)
        {
            currentAttackTarget = closestObstacle;
        }
        else
        {
            currentAttackTarget = mastTarget;
        }
    }

    /// <summary>
    ///  isCharging이 사라져 단순해진 이동 함수
    /// </summary>
    private void Move()
    {
        agent.speed = speed; // 항상 일반 속도로 이동
        if (currentAttackTarget != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentAttackTarget.transform.position);
        }
    }

    private bool CanAttack()
    {
        if (currentAttackTarget != null)
        {
            return Vector3.Distance(transform.position, currentAttackTarget.transform.position) <= attackRange;
        }
        return false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.updateRotation = false;

        Vector3 lookPos = currentAttackTarget.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        Debug.Log($"[{currentAttackTarget.name}]에게 공격!");
        yield return StartCoroutine(LungeVisualRoutine());

        yield return new WaitForSeconds(attackDelayTime);

        ResetToDefaultState();
    }

    private IEnumerator LungeVisualRoutine()
    {
        Vector3 spriteOriginalPos = spriteTransform.localPosition;
        Vector3 lungeEndPos = spriteOriginalPos + new Vector3(0, 0, attackRange);
        float elapsedTime = 0f;

        while (elapsedTime < lungeDuration)
        {
            spriteTransform.localPosition = Vector3.Lerp(spriteOriginalPos, lungeEndPos, elapsedTime / lungeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        spriteTransform.localPosition = lungeEndPos;

        DealDamage();

        elapsedTime = 0f;
        while (elapsedTime < lungeDuration)
        {
            spriteTransform.localPosition = Vector3.Lerp(lungeEndPos, spriteOriginalPos, elapsedTime / lungeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        spriteTransform.localPosition = spriteOriginalPos;
    }

    #endregion

    #region 보조 함수 (Attribute, Reset, Damage)

    protected override void SetAttribute()
    {
        base.SetAttribute();
        maxHp = 30;
        hp = maxHp;
        speed = 4f;
        attackRange = 2f;
        attackDamage = 20;
        attackDelayTime = 4f;
        if (fov != null) fov.viewRadius = attackRange;
    }

    private void ResetToDefaultState()
    {
        isAttacking = false;
        currentAttackTarget = mastTarget;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
    }

    private void DealDamage()
    {
        Collider[] hitColliders = DetectAttackRange();
        foreach (var hit in hitColliders)
        {
            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null && !targetBase.IsDead)
            {
                targetBase.TakeDamage(attackDamage, gameObject);
            }
        }
    }

    #endregion
}