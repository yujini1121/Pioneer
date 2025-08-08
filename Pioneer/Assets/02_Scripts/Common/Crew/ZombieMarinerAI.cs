using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좀비 AI - 타겟이 시야에 들어오면 접근 후 공격 시각화 및 판정
/// </summary>
public class ZombieMarinerAI : CreatureBase, IBegin
{
    public enum ZombieState { Wandering, Idle, Attacking }
    public int marinerId;
    private ZombieState currentState = ZombieState.Wandering;

    private float moveDuration = 2f;
    private float idleDuration = 4f;
    private float stateTimer = 0f;
    private Vector3 moveDirection;

    // 타겟 탐지 및 공격
    public LayerMask targetLayer;
    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;
    private Transform target;

    // 공격 시각화
    private bool isShowingAttackBox = false;

    // 스프라이트 변경
    public Transform spriteTransform;
    public SpriteRenderer spriteRenderer;

    // 공격 범위 오브젝트
    public GameObject attackRangeObject;

    private Coroutine attackRoutine;

    private void Awake()
    {
        maxHp = 40;
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        // CreatureBase의 fov 변수 사용
        fov = GetComponent<FOVController>();

        spriteTransform = transform.GetChild(0);
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        GameManager gm = FindObjectOfType<GameManager>();

        if (gm != null && gm.marinerSprites != null && gm.marinerSprites.Length > 1)
        {
            spriteTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            spriteRenderer.sprite = gm.marinerSprites[1];
        }

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        targetLayer = LayerMask.GetMask("Mariner");// fovController스크립트 이제 레이어 매개변수로 등록해야함.
    }

    private bool IsTargetInFOV()
    {
        if (target == null || fov == null)
            return false;

        // FOV에서 타겟 감지 수행
        fov.DetectTargets(targetLayer);
        return fov.visibleTargets.Contains(target);
    }

    public override void Init()
    {
        InitZombieStats();
        SetRandomDirection();
        stateTimer = moveDuration;

        // FOVController 초기화
        if (fov != null)
        {
            fov.Init();
        }

        Debug.Log("좀비 승무원 작동 중");

        base.Init();
    }

    private void InitZombieStats()
    {
        maxHp = 40; // 항상 40으로 고정
        speed = 1f; // CreatureBase의 변수 사용
        attackDamage = 6; // CreatureBase의 변수 사용
        attackRange = 3f; // CreatureBase의 변수 사용
        attackDelayTime = 1f; // CreatureBase의 변수 사용
    }

    private void Update()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0f)
        {
            if (DetectTarget())
            {
                if (IsTargetInFOV())
                {
                    LookAtTarget();

                    if (attackRoutine == null)
                    {
                        attackRoutine = StartCoroutine(AttackSequence());
                    }
                }
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
        // attackRange 변수 사용
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(attackRange / 2f, 0.5f, attackRange / 2f),
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

    private IEnumerator AttackSequence()
    {
        currentState = ZombieState.Attacking;

        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        while (Vector3.Distance(transform.position, attackPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, speed * Time.deltaTime);
            yield return null;
        }

        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(true);
        }

        // CreatureBase의 attackDelayTime 변수 사용
        yield return new WaitForSeconds(attackDelayTime);

        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(false);
        }

        // 공격 판정 
        Collider[] hits = Physics.OverlapBox(
            attackRangeObject.transform.position,
            attackRangeObject.transform.localScale / 2,
            transform.rotation,
            targetLayer
        );

        foreach (var hit in hits)
        {
            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(attackDamage);
                Debug.Log($"{hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }

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
        // CreatureBase의 attackRange 변수 사용
        Gizmos.DrawWireCube(transform.position, new Vector3(attackRange, 1f, attackRange));
    }
}