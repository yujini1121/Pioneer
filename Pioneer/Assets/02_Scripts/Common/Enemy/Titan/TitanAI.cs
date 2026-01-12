using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class TitanAI : EnemyBase, IBegin
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    private GameObject mastObject;

    private bool isAttack = false;
    private float attackTimer = 0f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        base.Start();

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;

        SetAttribute();
        if (agent != null) agent.speed = speed;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (attackTimer > 0f)
        {
            attackTimer -= dt;

            if (!isAttack)
            {
                ChangeIdleByIndex(lastMoveDirection);
                ApplyAnimTrigger();
            }

            if (attackTimer <= 0f && agent != null) agent.isStopped = false;
            return;
        }

        fov.DetectTargets(detectMask);

        if (currentAttackTarget == null)
        {
            mastObject = SetMastTarget();
            currentAttackTarget = mastObject;
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
    }

    protected override void SetAttribute()
    {
        maxHp = 30;
        hp = maxHp;
        attackDamage = 20;
        speed = 4;
        attackRange = 4;
        attackDelayTime = 4;

        mastObject = SetMastTarget();
        currentAttackTarget = mastObject;

        fov.viewRadius = 1f;
    }

    private bool CanMove()
    {
        return currentAttackTarget != null;
    }

    private bool CanAttack()
    {
        return fov.visibleTargets.Count > 0 && attackTimer <= 0f;
    }

    private void Move()
    {
        if (currentAttackTarget == null || agent == null) return;

        Vector3 targetPosition = new Vector3(
            currentAttackTarget.transform.position.x,
            transform.position.y,
            currentAttackTarget.transform.position.z);

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        agent.Warp(nextPosition);

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
    }

    private void Attack()
    {
        isAttack = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Transform targetToAttack = fov.visibleTargets[0];

        Vector3 directionToTarget = (targetToAttack.position - transform.position).normalized;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude > 1e-6f)
        {
            lastMoveDirection = directionToTarget;
        }

        ChangeAttackByIndex(lastMoveDirection);

        StartCoroutine(RushAttackSequence(targetToAttack));

        attackTimer = attackDelayTime;
    }

    private IEnumerator RushAttackSequence(Transform targetToAttack)
    {
        Vector3 directionToTarget = (targetToAttack.position - transform.position).normalized;
        directionToTarget.y = 0f;
        transform.rotation = Quaternion.LookRotation(directionToTarget);

        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            yield return null;

        const float dashStartNormalized = 0.15f;
        while (animator != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < dashStartNormalized)
            yield return null;

        float dashDuration = 0.2f;
        float dashSpeed = attackRange / dashDuration;

        Vector3 targetPosition = transform.position + transform.forward * attackRange;
        targetPosition.y = transform.position.y;

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dashSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_Titan);

        Collider[] hitColliders = DetectAttackRange();
        if (hitColliders != null)
        {
            foreach (Collider collider in hitColliders)
            {
                if (collider.gameObject == this.gameObject)
                    continue;

                CommonBase hitColCommonBase = collider.GetComponent<CommonBase>();
                if (hitColCommonBase != null)
                {
                    hitColCommonBase.TakeDamage(attackDamage, this.gameObject);
                }
            }
        }

        isAttack = false;
    }

    private void UpdateLocomotionAnim()
    {
        if (currentAttackTarget == null)
        {
            ChangeIdleByIndex(lastMoveDirection);
            return;
        }

        Vector3 v = currentAttackTarget.transform.position - transform.position;
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
