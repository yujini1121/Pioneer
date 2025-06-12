using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : EnemyBase
{
    NavMeshAgent agent;

    [Header("타겟 레이어 설정")]
    [SerializeField] private LayerMask targetLayers;

    // 공격
    [Header("공격 쿨타임 설정")]
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private GameObject attackRangeObject;

    private bool hasTargetPosition = false; // 랜덤 위치 생성 여부 확인 변수
    private bool isAttack = false; // 공격 당했는지?
    private float closeTargetDistance; // 제일 가까운 타겟과의 거리
    private float counterAttackTimer = 0f; // 공격 당한 애한테 가는 중
    private float counterAttackDuration = 1f; // 공격 당한 애한테 가는 시간
    private float rotationSpeed = 10f;
    private float lastAttackTime = 0f;
    private bool IsOnCooldown => Time.time < lastAttackTime + attackCooldown;
    private float CooldownRemaining => Mathf.Max(0f, (lastAttackTime + attackCooldown) - Time.time);
    private Vector3 targetPosition; // 랜덤 위치를 저장할 변수
    private Collider closeTarget; // 제일 가까운 타겟
    private GameObject attacker; // 날 공격한 애
    private GameObject lastTargetObj;
    private GameObject engineObject; 

    // 공격 시각화
    private float attackVisualTimer = 0f;
    private bool attackSuccess = false;

    // 둥지 관련 변수
    [Header("둥지 관련")]
    [SerializeField] private GameObject nestPrefab; // 둥지 프리팹

    public GameObject[] spawnNestList;

    private int maxNestCount = 2;
    private int currentSpawnNest = 0;
    private float spawnNestCoolTime = 15f;
    private float nestSpawnTime = 0f;
    private int spawnNestSlot = 0;


    // Behavior Tree Runner
    private BehaviorTreeRunner BTRunner = null;

    private void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();

        BTRunner = new BehaviorTreeRunner(SettingBt());

        spawnNestList = new GameObject[maxNestCount];
    }

    private void Update()
    {
        // 행동 우선 순위 판단
        BTRunner.Operate();
    }

    /// <summary>
    /// 기초 값 세팅
    /// </summary>
    protected override void SetAttribute()
    {
        hp = 20;
        attackPower = 1;
        speed = 2.0f;
        detectionRange = 4;
        attackRange = 2;
        attackVisualTime = 1.0f;  // 선제 시간
        restTime = 2.0f;
        SetTargetObj();
    }

    private void SetTargetObj()
    {
        targetObject = GameObject.FindGameObjectWithTag("Engine");
    }

    /// <summary>
    /// 공격 받았을때
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="source"></param>
    protected override void OnDamageReaction(int damage, GameObject source)
    {
        base.OnDamageReaction(damage, source);

        attacker = source;
        isAttack = true;
    }

    private void ResetAttackVariable()
    {
        isAttack = false;
        attacker = null;
        counterAttackTimer = 0f;
    }

    #region 둥지
    /// <summary>
    /// 둥지 배열 빈 인덱스 찾기
    /// </summary>
    /// <returns></returns>
    private int FindEmptyNestSlot()
    {
        for(int i = 0; i < spawnNestList.Length; i++)
        {
            if (spawnNestList[i] == null)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 배열에서 둥지 제거
    /// </summary>
    private void PopNestList()
    {
        for(int i = 0; i < spawnNestList.Length; i++)
        {
            if(spawnNestList[i] == null && currentSpawnNest > 0)
            {
                currentSpawnNest--;
            }
        }
    }    
    #endregion

    #region 감지
    /// <summary>
    /// 현재 감지 범위 내에서 가장 가까운 위치의 에너미를 반환 중
    /// </summary>
    /// <returns></returns>
    private Collider DetectTarget()
    {
        Vector3 boxSize = new Vector3(detectionRange / 2f, 1f, detectionRange / 2f);
        Vector3 boxCenter = transform.position + Vector3.forward * 1f;

        Collider[] detectColliders = Physics.OverlapBox(boxCenter, boxSize, transform.rotation, targetLayers);

        if (detectColliders.Length == 0)
            return null;

        closeTargetDistance = Mathf.Infinity;
        closeTarget = null;

        foreach(Collider collider in detectColliders)
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            
            if(distance <  closeTargetDistance)
            {
                closeTargetDistance = distance;
                closeTarget = collider;
            }
        }

        return closeTarget;
    }
    #endregion   

    #region 공격
    // 공격 범위 시각화
    private IEnumerator VisualizeAttackRange()
    {
        isAttack = true;

        if (attackRangeObject != null)
            attackRangeObject.SetActive(true);

        yield return new WaitForSeconds(attackVisualTime);        

        attackSuccess = AttackTarget();

        if (attackSuccess)
        {
            lastAttackTime = Time.time;
            UnityEngine.Debug.Log($"[공격] 성공! 다음 공격까지 {attackCooldown}초");
        }

        if (attackRangeObject != null)
            attackRangeObject.SetActive(false);

        yield return new WaitForSeconds(CooldownRemaining);
        isAttack = false;
    }

    private bool AttackTarget()
    {
        if (targetObject == null)
        {
            SetTargetObj();  // 타겟이 null이면 엔진으로 목표 재설정
            return false;
        }

        Vector3 directionToTarget = targetObject.transform.position - transform.position;
        directionToTarget.y = 0f;  // Y축을 0으로 설정해 수평 회전만 적용
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // 회전 속도 조정


        Vector3 boxSize = new Vector3(attackRange, 1f, 1f);
        Vector3 boxCenter = transform.position + transform.forward * (attackRange / 2f);

        // box 콜라이더 안에서 감지 실행
        Collider[] hits = Physics.OverlapBox(boxCenter, boxSize, transform.rotation, targetLayers);

        bool hitTarget = false;

        foreach (var hit in hits)
        {
            switch (hit.gameObject.layer)
            {
                case int layer when layer == LayerMask.NameToLayer("Player"):
                    if (PlayerController.Instance != null)
                    {
                        PlayerController.Instance.TakeDamage(attackPower); // 메서드명 수정
                        UnityEngine.Debug.Log("[공격] 데미지 적용 완료!");
                        hitTarget = true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("[오류] PlayerController를 찾을 수 없습니다!");
                    }
                    break;
                case int layer when layer == LayerMask.NameToLayer("Mariner"):
                    // hitTarget = true;
                    break;
                default:
                    break;
            }
        }

        return hitTarget;
    }

    private bool IsTargetInAttackRange()
    {
        if (targetObject == null) return false;

        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        return distance <= attackRange;
    }    
    #endregion

    #region 이동
    private void SetNewTargetPosition()
    {
        if (targetObject != null)
        {
            // 타겟 주변 랜덤 위치 설정
            Vector3 targetPos = targetObject.transform.position;
            Vector3 randomOffset = GetRandomHorizontalOffset(2f);
            targetPosition = targetPos + randomOffset;
        }
        else
        {
            // 엔진 주변 랜덤 위치 설정 (캐시된 엔진 참조 사용)
            if (engineObject == null)
            {
                engineObject = GameObject.FindGameObjectWithTag("Engine");
            }

            if (engineObject != null)
            {
                Vector3 enginePos = engineObject.transform.position;
                Vector3 randomOffset = GetRandomHorizontalOffset(2f);
                targetPosition = enginePos + randomOffset;
            }
        }
    }

    private Vector3 GetRandomHorizontalOffset(float radius)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        randomDirection.y = 0f; // 수평 이동만
        return randomDirection;
    }

    private void HandleRotation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
    #endregion

    #region 행동
    /// <summary>
    /// 이동 중 공격 당했을때 
    /// </summary>
    /// <returns></returns>
    INode.ENodeState CounterAttack()
    {
        if (!isAttack || attacker == null)
            return INode.ENodeState.Failure;

        counterAttackTimer += Time.deltaTime;

        if (counterAttackTimer < counterAttackDuration)
        {
            transform.position = Vector3.MoveTowards(transform.position, attacker.transform.position, speed * Time.deltaTime);
            return INode.ENodeState.Running;
        }

        Vector3 boxsize = new Vector3(1f, 0.5f, 1f);
        Vector3 boxCenter = transform.position + Vector3.up * 1f;

        Collider[] hits = Physics.OverlapBox(boxCenter, boxsize / 2f, transform.rotation, targetLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == attacker)
            {
                Vector3 lookDir = attacker.transform.position;
                lookDir.y = transform.position.y; // 수평 회전만 적용
                transform.LookAt(lookDir);

                targetObject = attacker;

                ResetAttackVariable();
                return INode.ENodeState.Success;
            }
        }

        ResetAttackVariable();

        SetTargetObj();

        return INode.ENodeState.Failure;
    }

    /// <summary>
    /// 둥지 소환
    /// </summary>
    /// <returns></returns>
    INode.ENodeState SpawnNest()
    {
        if (Time.time - nestSpawnTime < spawnNestCoolTime)
        {
            return INode.ENodeState.Failure;
        }

        if (currentSpawnNest >= maxNestCount)
        {
            return INode.ENodeState.Failure;
        }

        spawnNestSlot = FindEmptyNestSlot();
        if (spawnNestSlot == -1)
        {
            return INode.ENodeState.Failure;
        }

        GameObject spawnNest = Instantiate(nestPrefab, transform.position, transform.rotation);

        currentSpawnNest++;
        spawnNestList[spawnNestSlot] = spawnNest;

        nestSpawnTime = Time.time;

        return INode.ENodeState.Success;
    }    

    /// <summary>
    /// 이동
    /// </summary>
    /// <returns></returns>
    INode.ENodeState Movement()
    {     
        targetObject = DetectTarget()?.gameObject;
        // 타겟 오브젝트 정보에 null이 들어갔다면 다시 탐색? -> 기획서에는 그렇게 적혀있으나 엔진을 목표로 둘것임.
        if(targetObject == null)
        {
            SetTargetObj();
        }

        if (targetObject == null)
        {
            return INode.ENodeState.Failure;
        }

        if (!hasTargetPosition || targetObject != lastTargetObj)
        {
            SetNewTargetPosition();
            lastTargetObj = targetObject;
            hasTargetPosition = true;
        }    
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            hasTargetPosition = false; // 새로운 위치 재설정을 위해
            return INode.ENodeState.Success;
        }

        if (agent.velocity.sqrMagnitude > 0.1f) // 이동 중일 때만 회전
        {
            Vector3 direction = agent.velocity.normalized; // agent의 속도를 따라 회전
            direction.y = 0f; // y축은 수평 회전만 적용
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // 회전 속도
        }

        agent.SetDestination(targetPosition);
        HandleRotation();

        return INode.ENodeState.Running;
    }    

    /// <summary>
    /// 공격
    /// </summary>
    /// <returns></returns>
    INode.ENodeState Attack()
    {
        if (!IsTargetInAttackRange())
        {
            return INode.ENodeState.Failure;
        }

        if (IsOnCooldown)
        {
            // UnityEngine.Debug.Log($"[공격] 쿨타임 중... 남은 시간: {CooldownRemaining:F1}초");
            return INode.ENodeState.Running; // 또는 Failure (게임 디자인에 따라)
        }

        // 공격 실행
        StartCoroutine(VisualizeAttackRange());

        if (attackSuccess)
        {
            UnityEngine.Debug.Log($"[공격] 성공! 다음 공격까지 {attackCooldown}초");
            return INode.ENodeState.Success;
        }
        else
        {
            UnityEngine.Debug.Log("[공격] 실패 - 타겟을 찾을 수 없음");
            return INode.ENodeState.Failure;
        }
    }   
    #endregion

    #region 행동 트리 설정
    INode SettingBt()
    {
        return new SelecterNode(new List<INode>
        {
            // 1순위: 공격당했을 때 반격
            new ActionNode(() => CounterAttack()),
            
            // 2순위: 공격 범위에 타겟이 있으면 공격
            new ActionNode(() => Attack()),
            
            // 3순위: 둥지 생성 (확률적)
            new ActionNode(() => SpawnNest()),
            
            // 4순위: 이동 (기본 행동)
            new ActionNode(() => Movement())
        });
    }
    #endregion

    #region 디버그 및 시각화
    private void OnDrawGizmosSelected()
    {
        // 감지 범위 - 회전된 사각형
        if (detectionRange > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 boxCenter = transform.position + Vector3.up * 0.5f;
            Vector3 boxSize = new Vector3(detectionRange, 1f, detectionRange);

            // 회전 매트릭스 적용
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = Matrix4x4.identity; // 매트릭스 리셋
        }

        // 공격 범위 - 회전된 사각형
        if (attackRange > 0)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * (1f / 2f) + Vector3.up * 0.5f;
            Vector3 boxSize = new Vector3(attackRange, 1f, 1f);

            // 회전 매트릭스 적용
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = Matrix4x4.identity; // 매트릭스 리셋
        }
    }
    #endregion
}
