using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좀비 AI - 타겟이 시야에 들어오면 접근 후 공격 시각화 및 판정
/// </summary>
public class ZombieMarinerAI : MonoBehaviour
{
    public enum ZombieState { Wandering, Idle, Attacking }
    private ZombieState currentState = ZombieState.Wandering;

    private float speed = 1f;
    private float hp = 40f;
    private float moveDuration = 2f;
    private float idleDuration = 4f;
    private float stateTimer = 0f;
    private Vector3 moveDirection;

    // 타겟 탐지 및 공격
    public float detectionRange = 3f;
    public LayerMask targetLayer;
    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;
    private Transform target;

    // 공격 시각화
    private bool isShowingAttackBox = false;
    private float attackVisualDuration = 1f;
    private Coroutine attackRoutine;

    private void Start()
    {
        InitZombieStats();
        SetRandomDirection();
        stateTimer = moveDuration;
        Debug.Log("좀비 승무원 작동 중");
    }

    private void InitZombieStats()
    {
        if (hp > 40f)
        {
            Debug.Log("좀비 AI HP 자동 조정");
            hp = 40f;
        }
    }

    private void Update()
    {
        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0f)
        {
            if (DetectTarget())
            {
                LookAtTarget();

                if (attackRoutine == null)
                    attackRoutine = StartCoroutine(AttackSequence());
            }

            attackCooldown = attackInterval;
        }

        switch (currentState)
        {
            case ZombieState.Wandering:
                Wander();
                break;
            case ZombieState.Idle:
                Idle();
                break;
            case ZombieState.Attacking:
                // 공격 루틴 내에서 처리됨
                break;
        }
    }

    private void Wander()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Debug.Log("좀비 AI 이동 후 대기 상태");
            EnterIdleState();
        }
    }

    private void Idle()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Debug.Log("좀비 AI 대기에서 다시 이동 상태");
            EnterWanderingState();
        }
    }

    private void EnterWanderingState()
    {
        SetRandomDirection();
        currentState = ZombieState.Wandering;
        stateTimer = moveDuration;
        Debug.Log("랜덤 방향으로 이동 시작");
    }

    private void EnterIdleState()
    {
        currentState = ZombieState.Idle;
        stateTimer = idleDuration;
        Debug.Log("좀비 AI 대기 상태로 전환");
    }

    private void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    private bool DetectTarget()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(1.5f, 0.5f, 1.5f),
            Quaternion.identity,
            targetLayer
        );

        float minDist = float.MaxValue;
        target = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                target = hit.transform;
            }
        }

        return target != null;
    }

    private void LookAtTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    /// <summary>
    /// 빨간색 박스 공격 범위 생성
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackSequence()
    {
        currentState = ZombieState.Attacking;

        // 접근할 위치 계산 (1m 떨어진 지점)
        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        // 이동
        while (Vector3.Distance(transform.position, attackPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, speed * Time.deltaTime);
            yield return null;
        }

        // 1초간 공격 박스 시각화
        isShowingAttackBox = true;
        yield return new WaitForSeconds(attackVisualDuration);
        isShowingAttackBox = false;

        // 공격 판정 (디버그용)
        Vector3 boxCenter = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapBox(boxCenter, new Vector3(1f, 0.5f, 1f), transform.rotation, targetLayer);

        foreach (var hit in hits)
        {
            Debug.Log($"{hit.name} 공격 범위 내");

            MarinerStatus marinerStatus = hit.GetComponent<MarinerStatus>();
            if (marinerStatus != null)
            {
                int damage = marinerStatus.attackPower;
                marinerStatus.currentHP -= damage;
                Debug.Log($"{hit.name}에게 {damage}의 데미지를 입혔습니다.");

                marinerStatus.UpdateStatus();
            }
        }

        // 상태 복귀
        currentState = ZombieState.Wandering;
        stateTimer = moveDuration;
        SetRandomDirection();
        attackRoutine = null;
    }

    private void OnDrawGizmos()
    {
        if (isShowingAttackBox)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * 1f;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 2f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(3f, 1f, 3f));
    }
}
