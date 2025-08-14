using System.Collections;
using UnityEngine;

public class ZombieMarinerAI : MarinerBase, IBegin
{
    // 좀비 고유 설정
    public int marinerId;

    // 좀비 시각적 요소
    public UnityEngine.Transform spriteTransform;
    public SpriteRenderer spriteRenderer;
    public GameObject attackRangeObject;

    // 공격 설정
    private float attackCooldown = 0f;
    private float attackInterval = 0.5f;

    private void Awake()
    {
        InitZombieStats();
        InitZombieVisuals();

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        targetLayer = LayerMask.GetMask("Mariner", "Player");
    }

    private void InitZombieStats()
    {
        maxHp = 40;  // 좀비 HP
        speed = 2f;
        attackDamage = 6;
        attackRange = 4f;
        attackDelayTime = 1f;

        chaseRange = 10f;  // 더 넓은 추격 범위

        fov = GetComponent<FOVController>();
    }

    private void InitZombieVisuals()
    {
        spriteTransform = transform.GetChild(0);
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        GameManager gm = FindObjectOfType<GameManager>();

        if (gm != null && gm.marinerSprites != null && gm.marinerSprites.Length > 1)
        {
            spriteTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            spriteRenderer.sprite = gm.marinerSprites[1];
        }
    }

    public override void Start()
    {
        SetRandomDirection();
        stateTimer = moveDuration;

        if (fov != null)
        {
            fov.Start();
        }

        Debug.Log($"좀비 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}");
        base.Start();
    }

    private void Update()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;

        ValidateCurrentTarget();

        if (target == null)
        {
            TryFindNewTarget();
        }

        if (isChasing && target != null)
        {
            HandleChasing();
        }
        else
        {
            HandleNormalBehavior();
        }
    }

    protected override float GetAttackCooldown()
    {
        return attackCooldown;
    }

    protected override IEnumerator GetAttackSequence()
    {
        return ZombieAttackSequence();
    }

    private IEnumerator ZombieAttackSequence()
    {
        currentState = CrewState.Attacking;

        if (target == null)
        {
            attackRoutine = null;
            isChasing = false;
            EnterWanderingState();
            yield break;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        LookAtTarget();

        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(true);
            isShowingAttackBox = true;
        }

        yield return new WaitForSeconds(attackDelayTime);

        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(false);
            isShowingAttackBox = false;
        }

        PerformZombieAttack();

        attackCooldown = attackInterval;

        if (target != null)
        {
            CommonBase targetBase = target.GetComponent<CommonBase>();
            if (targetBase != null && !targetBase.IsDead)
            {
                Debug.Log($"좀비 {marinerId}: 공격 완료, 추격 재개");
                // 추격 상태로 복귀
                EnterChasingState();
            }
            else
            {
                target = null;
                isChasing = false;
                EnterWanderingState();
            }
        }
        else
        {
            isChasing = false;
            EnterWanderingState();
        }

        attackRoutine = null;
    }

    private void PerformZombieAttack()
    {
        Vector3 attackCenter = attackRangeObject != null ?
            attackRangeObject.transform.position :
            transform.position + transform.forward * 1f;

        Vector3 attackSize = attackRangeObject != null ?
            attackRangeObject.transform.localScale / 2 :
            new Vector3(1f, 0.5f, 1f);

        Collider[] hits = Physics.OverlapBox(
            attackCenter,
            attackSize,
            transform.rotation,
            targetLayer
        );

        foreach (var hit in hits)
        {
            Debug.Log($"좀비가 {hit.name} 공격 범위 내 감지");

            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(attackDamage);
                Debug.Log($"좀비가 {hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
            }
        }
    }

    protected override void TryFindNewTarget()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(chaseRange / 2f, 0.5f, chaseRange / 2f),
            Quaternion.identity,
            targetLayer
        );

        float minDist = float.MaxValue;
        UnityEngine.Transform nearestTarget = null;

        foreach (var hit in hits)
        {
            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null && targetBase.IsDead)
                continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestTarget = hit.transform;
            }
        }

        if (nearestTarget != null)
        {
            target = nearestTarget;
            EnterChasingState();
            Debug.Log($"좀비 {marinerId}: {target.name} 추격 시작! (거리: {minDist:F1}m)");
        }
    }

    protected override void ValidateCurrentTarget()
    {
        if (target != null)
        {
            CommonBase targetBase = target.GetComponent<CommonBase>();
            if (targetBase != null && targetBase.IsDead)
            {
                Debug.Log($"좀비 {marinerId}: 타겟 {target.name}이 죽었습니다. 새로운 타겟을 찾습니다.");
                target = null;
                isChasing = false;
                EnterWanderingState();
            }
        }
    }

    protected override void ChaseTarget()
    {
        if (target == null || agent == null || !agent.isOnNavMesh) return;

        float zombieChaseUpdateInterval = 0.1f;

        if (Time.time - lastChaseUpdate >= zombieChaseUpdateInterval)
        {
            agent.SetDestination(target.position);
            lastChaseUpdate = Time.time;
        }

        LookAtTarget();
    }

    public override IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"좀비 {marinerId}: 배회 계속");
        yield return new WaitForSeconds(1f);
        EnterWanderingState();
    }


}