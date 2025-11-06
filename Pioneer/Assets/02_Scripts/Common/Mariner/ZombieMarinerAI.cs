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
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (fov == null)
            fov = GetComponent<FOVController>() ?? GetComponentInChildren<FOVController>(true);

        InitZombieStats();
        //InitZombieVisuals();

        if (attackRangeObject == null)
        {
            // 자식 중 이름이 "AttackRange"인 오브젝트를 탐색
            Transform found = transform.Find("AttackRange");
            if (found != null)
            {
                attackRangeObject = found.gameObject;
                Debug.Log($"[ZombieMarinerAI] AttackRange 자동 연결 성공: {attackRangeObject.name}");
            }
            else
            {
                Debug.LogWarning("[ZombieMarinerAI] AttackRange 오브젝트를 찾지 못했습니다!");
            }
        }

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        targetLayer = LayerMask.GetMask("Mariner", "Player");
    }

    private void InitZombieStats()
    {
        maxHp = 40;
        hp = 40;
        speed = 2f;
        attackDamage = 6;
        attackRange = 4f;
        attackDelayTime = 1f;

        chaseRange = 10f; 

        fov = GetComponent<FOVController>();
    }

    /*private void InitZombieVisuals()
    {
        spriteTransform = transform.GetChild(0);
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        GameManager gm = FindObjectOfType<GameManager>();

        if (gm != null && gm.marinerSprites != null && gm.marinerSprites.Length > 1)
        {
            spriteTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            spriteRenderer.sprite = gm.marinerSprites[1];
        }
    }*/

    public override void Start()
    {
        base.Start();

        SetRandomDirection();
        stateTimer = moveDuration;

        if (fov != null)
        {
            fov.Start();
        }

        Debug.Log($"좀비 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}");
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
            if (hit.gameObject == this.gameObject) continue;
            Debug.Log($"좀비가 {hit.name} 공격 범위 내 감지");

            CommonBase targetBase = hit.GetComponent<CommonBase>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(attackDamage, this.gameObject);
                Debug.Log($"좀비가 {hit.name}에게 {attackDamage}의 데미지를 입혔습니다.");
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

    public override void WhenDestroy()
    {
        GameManager.Instance.MarinerDiedCount();
        base.WhenDestroy();
    }

    private void OnEnable()
    {
        Debug.Log("ZombieMariner에서 attack box 활성화");
        if (attackRangeObject != null)
            attackRangeObject.SetActive(false);

    }

}