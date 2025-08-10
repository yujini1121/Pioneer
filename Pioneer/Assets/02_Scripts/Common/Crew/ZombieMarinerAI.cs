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
        InitZombieLayers();
    }

    /// <summary>
    /// 좀비 스탯 초기화
    /// </summary>
    private void InitZombieStats()
    {
        maxHp = 40;  // 좀비 HP
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        fov = GetComponent<FOVController>();
    }

    /// <summary>
    /// 좀비 시각적 요소 초기화
    /// </summary>
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

    /// <summary>
    /// 좀비 레이어 및 타겟 설정
    /// </summary>
    private void InitZombieLayers()
    {
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        targetLayer = LayerMask.GetMask("Mariner");
    }

    public override void Init()
    {
        SetRandomDirection(); 
        stateTimer = moveDuration;

        if (fov != null)
        {
            fov.Init();
        }

        Debug.Log($"좀비 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}");
        base.Init();
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
                        attackRoutine = StartCoroutine(ZombieAttackSequence());
                    }
                }
            }

            attackCooldown = attackInterval;
        }

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

    /// <summary>
    /// 좀비만의 공격 시퀀스 
    /// </summary>
    private IEnumerator ZombieAttackSequence()
    {
        currentState = CrewState.Attacking;

        Vector3 targetOffset = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - targetOffset;
        attackPosition.y = transform.position.y;

        // 타겟에게 접근
        while (Vector3.Distance(transform.position, attackPosition) > 0.1f)
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

        // 공격 후 배회 상태로 복귀
        currentState = CrewState.Wandering;
        stateTimer = moveDuration;
        SetRandomDirection(); 
        attackRoutine = null;
    }

    /// <summary>
    /// 좀비 공격 판정 수행
    /// </summary>
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