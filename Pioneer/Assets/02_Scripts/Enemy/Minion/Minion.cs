using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Minion : EnemyBase
{
    [Header("미니언 전용 설정")]
    public GameObject nestPrefab;  // 둥지 프리팹
    public LayerMask targetLayer;  // 탐지할 레이어
    public LayerMask unitLayer;    // 유닛 레이어
    public LayerMask installationLayer; // 설치형 오브젝트 레이어

    [Header("행동 설정")]
    [SerializeField] private float detectionCheckInterval = 0.2f; // 탐지 체크 간격
    [SerializeField] private float nestSpawnCooldown = 15f; // 둥지 소환 쿨다운
    [SerializeField] private float counterAttackSpeed = 10f; // 반격 시 이동 속도

    // 상태 변수들
    private int currentSpawnNest = 0;  // 소환된 둥지 수
    private GameObject attacker = null;  // 자신을 공격한 오브젝트
    private bool isUnderAttack = false;  // 공격받고 있는지 여부
    private float lastSpawnTime = 0f;  // 둥지 소환 시간 추적
    private bool isCooldown = false;  // 1순위 행동 불가 상태 체크
    private bool hasDetectionRadar = false;  // 감지 레이더 활성화 여부
    private Vector3 currentDetectionSize = Vector3.zero;  // 현재 감지 범위
    private List<GameObject> spawnedNests = new List<GameObject>();  // 소환된 둥지들
    private bool isAttacking = false;  // 공격 중인지 여부
    private float lastAttackTime = 0f;  // 마지막 공격 시간
    private float lastDetectionCheck = 0f; // 마지막 탐지 체크 시간

    // 공격 시각화 관련
    private GameObject attackPreview;
    private Renderer attackPreviewRenderer;

    // 행동 트리 러너
    private BehaviorTreeRunner _BTRunner = null;

    private void Awake()
    {
        base.Awake();
        SetupAttackPreview();
        _BTRunner = new BehaviorTreeRunner(SettingBt());
    }

    private void Start()
    {
        // 감지 레이더 초기 설정
        UpdateDetectionRadar(detectionRange);
    }

    private void Update()
    {
        _BTRunner.Operate();
        CleanupDeadNests();
        CheckCooldownStatus();
    }

    private void OnDestroy()
    {
        // 메모리 정리
        if (attackPreview != null)
        {
            DestroyImmediate(attackPreview);
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        attackPower = 1;
        speed = 2.0f;
        detectionRange = 4;
        attackRange = 2;
        attackVisualTime = 1.0f;  // 선제 시간
        restTime = 2.0f;
        targetObject = GameObject.FindGameObjectWithTag("Engine");  // 기본 목표는 엔진
    }

    #region 초기화 및 정리 메서드

    private void SetupAttackPreview()
    {
        attackPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        attackPreview.name = "AttackPreview_" + gameObject.name;
        attackPreview.transform.SetParent(transform);
        attackPreview.transform.localPosition = Vector3.forward * 1f;
        attackPreview.transform.localScale = new Vector3(2f, 1f, 1f);

        attackPreviewRenderer = attackPreview.GetComponent<Renderer>();
        attackPreviewRenderer.material.color = Color.red;
        attackPreview.SetActive(false);

        // 콜라이더 제거
        Collider previewCollider = attackPreview.GetComponent<Collider>();
        if (previewCollider != null)
        {
            DestroyImmediate(previewCollider);
        }
    }

    private void CleanupDeadNests()
    {
        spawnedNests.RemoveAll(nest => nest == null);
        currentSpawnNest = spawnedNests.Count;
    }

    private void CheckCooldownStatus()
    {
        // 쿨다운 상태 체크
        if (Time.time - lastSpawnTime >= nestSpawnCooldown && isCooldown)
        {
            isCooldown = false;
            UnityEngine.Debug.Log("둥지 소환 쿨다운 해제");
        }
    }

    private void UpdateDetectionRadar(float range)
    {
        hasDetectionRadar = true;
        currentDetectionSize = new Vector3(range, 1f, range);
    }

    #endregion

    #region 행동 트리 액션들

    // 1순위 행동: 둥지 소환
    INode.ENodeState SpawnNest()
    {
        if (currentSpawnNest < 2 && !isCooldown && nestPrefab != null)
        {
            // 둥지 생성
            GameObject nest = Instantiate(nestPrefab, transform.position, transform.rotation);
            spawnedNests.Add(nest);
            currentSpawnNest++;

            lastSpawnTime = Time.time;
            isCooldown = true;

            // 감지 레이더 활성화
            UpdateDetectionRadar(detectionRange);

            UnityEngine.Debug.Log($"둥지 소환! 총 개수: {currentSpawnNest}");
            return INode.ENodeState.Success;
        }

        return INode.ENodeState.Failure;
    }

    // 2순위 행동: 이동
    INode.ENodeState Movement()
    {
        // 타겟이 없으면 실패
        if (targetObject == null)
        {
            return INode.ENodeState.Failure;
        }

        // 감지 레이더로 새로운 타겟 탐지 (성능 최적화를 위해 주기적으로만 체크)
        if (hasDetectionRadar && Time.time - lastDetectionCheck >= detectionCheckInterval)
        {
            GameObject detectedTarget = DetectInRadar();
            if (detectedTarget != null)
            {
                UpdateDetectionRadar(2f); // 감지 범위 축소
                targetObject = detectedTarget;
            }
            lastDetectionCheck = Time.time;
        }

        // 타겟을 향해 이동
        Vector3 direction = (targetObject.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.LookAt(targetObject.transform.position);

        // 공격 범위에 도달했는지 체크
        float distanceToTarget = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distanceToTarget <= attackRange)
        {
            hasDetectionRadar = false;  // 감지 레이더 비활성화
            return INode.ENodeState.Success;  // 공격으로 전환
        }

        return INode.ENodeState.Running;
    }

    // 3순위 행동: 공격
    INode.ENodeState Attack()
    {
        if (targetObject == null)
        {
            return INode.ENodeState.Failure;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distanceToTarget > attackRange)
        {
            // 타겟이 공격 범위를 벗어났으면 다시 이동
            UpdateDetectionRadar(2f);
            return INode.ENodeState.Failure;
        }

        if (!isAttacking && Time.time - lastAttackTime >= restTime)
        {
            StartCoroutine(PerformAttack());
            return INode.ENodeState.Success;
        }

        return isAttacking ? INode.ENodeState.Running : INode.ENodeState.Failure;
    }

    // 예외 행동: 반격
    INode.ENodeState CounterAttack()
    {
        if (!isUnderAttack || attacker == null)
            return INode.ENodeState.Failure;

        // 감지 레이더 설정
        if (!hasDetectionRadar)
        {
            UpdateDetectionRadar(2f);
        }

        // 공격자 위치로 빠르게 이동
        Vector3 direction = (attacker.transform.position - transform.position).normalized;
        transform.position += direction * speed * counterAttackSpeed * Time.deltaTime;
        transform.LookAt(attacker.transform.position);

        // 공격자가 감지 레이더 내에 있는지 체크
        if (IsTargetInRadar(attacker))
        {
            StartCoroutine(PerformCounterAttack());
            ResetCounterAttackState();
            return INode.ENodeState.Success;
        }

        return INode.ENodeState.Running;
    }

    #endregion

    #region 탐지 및 타겟팅

    // 감지 레이더 내 타겟 탐지
    private GameObject DetectInRadar()
    {
        Vector3 detectionCenter = transform.position;
        Collider[] detectedTargets = Physics.OverlapBox(
            detectionCenter,
            currentDetectionSize / 2,
            Quaternion.identity,
            unitLayer | targetLayer | installationLayer
        );

        if (detectedTargets.Length > 0)
        {
            return FindClosestTarget(detectedTargets);
        }
        return null;
    }

    // 타겟이 감지 레이더 내에 있는지 체크
    private bool IsTargetInRadar(GameObject target)
    {
        if (target == null || !hasDetectionRadar)
            return false;

        Vector3 detectionCenter = transform.position;
        Bounds detectionBounds = new Bounds(detectionCenter, currentDetectionSize);

        return detectionBounds.Contains(target.transform.position);
    }

    // 가장 가까운 목표 찾기
    private GameObject FindClosestTarget(Collider[] targets)
    {
        GameObject closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider target in targets)
        {
            // 자기 자신과 같은 태그는 제외
            if (target.gameObject == gameObject || target.CompareTag("Enemy"))
                continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target.gameObject;
            }
        }

        return closestTarget;
    }

    // 적을 감지하고 목표 설정
    INode.ENodeState Detect()
    {
        // 감지 범위 내에 다른 타겟이 있는지 먼저 확인
        GameObject detectedTarget = DetectInRadar();

        if (detectedTarget != null)
        {
            // 감지 범위 내에 타겟이 있으면 우선적으로 설정
            targetObject = detectedTarget;
        }
        else
        {
            // 감지 범위 내에 타겟이 없으면 기본 엔진으로 복귀
            GameObject engine = GameObject.FindGameObjectWithTag("Engine");
            if (engine != null)
            {
                targetObject = engine;
            }
        }

        return targetObject != null ? INode.ENodeState.Success : INode.ENodeState.Failure;
    }

    #endregion

    #region 공격 시스템

    // 공격 실행 코루틴
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 타겟 방향으로 바라보기
        if (targetObject != null)
        {
            transform.LookAt(targetObject.transform.position);
        }

        // 공격 범위 미리보기
        attackPreview.SetActive(true);
        yield return new WaitForSeconds(attackVisualTime);

        // 공격 실행
        attackPreview.SetActive(false);
        DealDamageInRange();

        // 경직 적용
        yield return new WaitForSeconds(restTime);

        isAttacking = false;
    }

    // 반격 실행 코루틴
    private IEnumerator PerformCounterAttack()
    {
        if (attacker == null) yield break;

        // 공격자 방향으로 바라보기
        transform.LookAt(attacker.transform.position);

        // 공격 범위 미리보기
        attackPreview.SetActive(true);
        yield return new WaitForSeconds(attackVisualTime);

        // 공격 실행
        attackPreview.SetActive(false);
        DealDamageInRange();

        // 공격속도만큼 딜레이
        yield return new WaitForSeconds(restTime);
    }

    // 공격 범위 내 피해 처리
    private void DealDamageInRange()
    {
        Vector3 attackCenter = transform.position + transform.forward * 1f;
        Vector3 attackSize = new Vector3(1f, 1f, 2f);

        Collider[] hits = Physics.OverlapBox(attackCenter, attackSize / 2, transform.rotation);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject != gameObject && !hit.CompareTag("Enemy"))
            {
                // 피해 처리 로직
                var health = hit.GetComponent<EnemyBase>();
                if (health != null)
                {
                    health.TakeDamage(attackPower);
                }

                UnityEngine.Debug.Log($"피해 입힘: {hit.name}");
            }
        }
    }

    #endregion

    #region 상태 관리

    private void ResetCounterAttackState()
    {
        isUnderAttack = false;
        attacker = null;
    }

    // 데미지 반응 오버라이드 - 반격 상태 설정
    protected override void OnDamageReaction(int damage, GameObject source)
    {
        base.OnDamageReaction(damage, source);

        if (source != null)
        {
            isUnderAttack = true;
            attacker = source;
            UnityEngine.Debug.Log($"미니언이 공격받음! 공격자: {source.name}, 반격 준비");
        }
    }

    // 사망 시 특별한 행동 - 소환된 둥지들 정리
    protected override void OnDeathBehavior()
    {
        base.OnDeathBehavior();

        // 소환된 둥지들 제거
        foreach (GameObject nest in spawnedNests)
        {
            if (nest != null)
            {
                Destroy(nest);
            }
        }
        spawnedNests.Clear();

        UnityEngine.Debug.Log("미니언 사망 - 소환된 둥지들을 모두 제거했습니다.");
    }

    #endregion

    #region 행동 트리 구성

    // Behavior Tree 구성 - 올바른 우선순위로 수정
    INode SettingBt()
    {
        return new SelecterNode(new List<INode>
        {
            // 예외 행동: 반격 (최우선)
            new SequenceNode(new List<INode>
            {
                new ActionNode(() => CounterAttack())
            }),

            // 1순위: 둥지 소환
            new SequenceNode(new List<INode>
            {
                new ActionNode(() => SpawnNest())
            }),

            // 2순위: 탐색 후 이동
            new SequenceNode(new List<INode>
            {
                new ActionNode(() =>
                {
                    Detect();
                    return Movement();
                })
            }),

            // 3순위: 공격
            new SequenceNode(new List<INode>
            {
                new ActionNode(() => Attack())
            })
        });
    }

    #endregion

    #region 디버그 및 시각화

    // 기즈모로 감지 범위 표시
    private void OnDrawGizmosSelected()
    {
        // 감지 범위
        if (hasDetectionRadar)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, currentDetectionSize);
        }

        // 공격 범위
        Gizmos.color = Color.red;
        Vector3 attackCenter = transform.position + transform.forward * 1f;
        Gizmos.DrawWireCube(attackCenter, new Vector3(2f, 1f, 1f));

        // 타겟 연결선
        if (targetObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetObject.transform.position);
        }
    }

    #endregion
}