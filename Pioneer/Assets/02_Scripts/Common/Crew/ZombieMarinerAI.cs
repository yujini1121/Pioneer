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

    // 추격 시스템 관련 변수
    private float chaseRange = 8f;  // 추격 시작 범위
    private bool isChasing = false; // 추격 중인지 확인

    private void Awake()
    {
        InitZombieStats();
        InitZombieVisuals();
        InitZombieLayers();
    }

    private void InitZombieStats()
    {
        maxHp = 40;  // 좀비 HP
        speed = 3f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

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

    private void InitZombieLayers()
    {
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        targetLayer = LayerMask.GetMask("Mariner", "Player");
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

        // 현재 타겟이 있는지 확인하고 유효성 검사
        ValidateCurrentTarget();

        // 타겟이 없다면 새로운 타겟 탐색
        if (target == null && !isChasing)
        {
            TryFindNewTarget();
        }

        // 타겟이 있다면 추격 모드
        if (target != null && isChasing)
        {
            HandleChasing();
        }
        else
        {
            HandleNormalBehavior();
        }
    }

    private void ValidateCurrentTarget()
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

    private void TryFindNewTarget()
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
            isChasing = true;
            Debug.Log($"좀비 {marinerId}: {target.name} 추격 시작!");
        }
    }

    private void HandleChasing()
    {
        if (target == null)
        {
            isChasing = false;
            EnterWanderingState(); 
            return;
        }

        LookAtTarget();

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange && attackCooldown <= 0f)
        {
            if (IsTargetInFOV() && attackRoutine == null)
            {
                attackRoutine = StartCoroutine(ZombieAttackSequence());
            }
        }
        else
        {
            ChaseTarget();
        }
    }

    private void HandleNormalBehavior()
    {
        switch (currentState)
        {
            case CrewState.Wandering:
                Wander(); 
                break;
            case CrewState.Idle:
                Idle(); 
                break;
            case CrewState.Attacking:
                break;
        }
    }

    private void ChaseTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; 

        transform.position += direction * speed * Time.deltaTime;
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

        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        // 타겟에게 접근
        while (Vector3.Distance(transform.position, attackPosition) > 0.1f && target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, speed * Time.deltaTime);
            yield return null;
        }

        // 공격 범위 오브젝트 활성화
        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(true);
            isShowingAttackBox = true;
        }

        // 공격 딜레이
        yield return new WaitForSeconds(attackDelayTime);

        // 공격 범위 오브젝트 비활성화
        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(false);
            isShowingAttackBox = false; 
        }

        // 좀비 공격 판정 
        PerformZombieAttack();

        // 공격 쿨다운 설정
        attackCooldown = attackInterval;

        if (target != null)
        {
            CommonBase targetBase = target.GetComponent<CommonBase>();
            if (targetBase != null && !targetBase.IsDead)
            {
                // 추격 계속
                Debug.Log($"좀비 {marinerId}: 공격 완료, 추격 재개");
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

}