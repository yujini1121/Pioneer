using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
250814
* 문제 1 : 공격을 한 번 하면 애가 동작이 다 멈추는거 같음
- 문제 2 : 제대로 공격이 안 들어감 => 현재 테스트 씬의 플레이어가 CommonBase를 상속 받지않아 정확한 확인 불가
- 문제 3 : 둥지 로직 구현 안 함
- 문제 4 : 바다에 있을때 네브메시를 끄고 배 위에 올라왔을때 네브메시를 키기
- 문제 5 : 배 위인지 확인하는 코드 모든 에너미가 써야할 것 같아서 EnemyBase로 옮기기

+ 코드가 너무 더러움 다시 깔끔하게 구현해보기
=========================================================================================================
250815
- 공격도중 에너미가 죽었을때 돛대로 타겟 변경이 안됨 + 돛대로 이동도 안 함
- 공격 딜레이 적용 안됨
- 감지 범위 내에 여러 타겟이 있어도 하나가 죽으면 다른 범위 내 타겟을 인식하는게 아니라 바로 돛대로 향하는 문제가 있음
*/
public class MinionAI : EnemyBase, IBegin
{
    [Header("둥지 프리팹")]
    [SerializeField] private GameObject nestPrefab;

    [Header("배 바닥 레이어")]
    [SerializeField] private LayerMask groundLayer;

    // 네브 메시 
    private NavMeshAgent agent;

    // 둥지 관련 변수
    private bool isNestCreated = false;
    private float nestCool = 15f;
    private float nestCreationTime = -1f;

    // 현재 타겟 관련 변수
    // private Transform currentAttackTarget = null;

    // 바닥 확인 변수
    private bool isOnGround = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();
        if(agent != null)
        {
            agent.speed = speed;
        }
    }

    void Update()
    {      
        // 레이어 변수 수정
        fov.DetectTargets(detectMask);
        CheckOnGround();

        if (CanCreateNest())
        {
            CreateNest();
        }
        else if (CanAttack())
        {
            Attack();
        }
        else if(CanMove())
        {
            Move();
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        attackDamage = 1;
        speed = 2f;
        detectionRange = 4f;
        attackDelayTime = 2f;
        idleTime = 2f;
        SetMastTarget();
        fov.viewRadius = attackRange;
    }

    #region 둥지 생성
    private bool CanCreateNest()
    {
        return isOnGround 
            && !isNestCreated 
            && Time.time >= nestCreationTime 
            && nestCreationTime != -1f
            && !CanAttack();
    }

    // 둥지 생성
    void CreateNest()
    {
        Instantiate(nestPrefab, transform.position, Quaternion.identity);
        isNestCreated = true;
    }
    #endregion

    #region 공격
    private bool CanAttack()
    {
        return DetectAttackRange().Length > 0;
    }

    void Attack()
    {  
        if(fov.visibleTargets.Count > 0)
        {
            // 공격 범위 안에 있는 콜라이더들 가지고 오기
            Collider[] detectColliders = DetectAttackRange();

            // 디버깅용 코드
            Debug.Log($"DetectAttackRange에서 {detectColliders.Length}개의 콜라이더 감지됨");
            for (int i = 0; i < detectColliders.Length; i++)
            {
                Debug.Log($"[{i}] 이름: {detectColliders[i].gameObject.name}, 태그: {detectColliders[i].gameObject.tag}");
            }
            // 여기까지

            if (detectColliders.Length > 0)
            {
                currentAttackTarget = FindClosestTarget(detectColliders);
                Debug.Log($"가까운 타겟 : {currentAttackTarget}");

                if (currentAttackTarget != null)
                {
                    Debug.Log("가장 가까운 애 찾음");
                    agent.isStopped = true;
                    Debug.Log("가장 가까운 애 찾음2");
                    CommonBase targetBase = currentAttackTarget.GetComponent<CommonBase>();
                    Debug.Log($"currentAttackTarget : {currentAttackTarget.gameObject.name}");
                    if (targetBase != null)
                    {
                        Debug.Log("가장 가까운 애 찾음4");
                        targetBase.TakeDamage(attackDamage);
                        if(targetBase.IsDead == true)
                        {
                            SetMastTarget();
                            agent.SetDestination(currentAttackTarget.transform.position);
                            agent.isStopped = false;
                        }
                        Debug.Log($"공격 대상: {currentAttackTarget.name}, 현재 HP: {targetBase.CurrentHp}");
                    }
                }
                else
                {
                    Debug.Log("가장 가까운 애 찾음88");
                    agent.isStopped = false;
                }
            }
        }
    }

    /// <summary>
    /// 공격 범위 내에서 가장 가까운 적 찾기
    /// </summary>
    /// <returns></returns>
    private GameObject FindClosestTarget(Collider[] detectColliders)
    {
        GameObject closestTarget = null;
        float closestDis = float.MaxValue;

        foreach (var target in detectColliders)
        {
            float dis = Vector3.Distance(transform.position, target.transform.position);
            if (dis < closestDis)
            {
                closestDis = dis;
                closestTarget = target.gameObject;
            }
        }

        return closestTarget;
    }
    #endregion

    #region 이동
    private bool CanMove()
    {
        return currentAttackTarget != null || fov.visibleTargets.Count > 0;
    }

    void Move()
    {
        if (agent.isStopped) return;

        Transform moveTarget = currentAttackTarget != null ? currentAttackTarget.transform : null;
        if (fov.visibleTargets.Count > 0)
        {
            moveTarget = FindClosestTargetFromList(fov.visibleTargets);
        }

        if (moveTarget != null && agent != null)
        {
            Collider col = moveTarget.GetComponent<Collider>();
            Vector3 destination = col != null ? col.ClosestPoint(transform.position) : moveTarget.position;

            if (Vector3.Distance(agent.destination, destination) > 0.5f)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
            }
        }
    }

    private Transform FindClosestTargetFromList(List<Transform> targets)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var t in targets)
        {
            float dist = Vector3.Distance(transform.position, t.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = t;
            }
        }
        return closest;
    }
    #endregion

    // 배 플렛폼 위인지 검사
    private bool CheckOnGround()
    {
        // RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, 2f, groundLayer))
        {
            if(!isOnGround)
            {
                nestCool = Random.Range(5f, 15f);
                nestCreationTime = Time.time + nestCool;
                isOnGround = true;
            }
            Debug.Log("배 위다");
        }
        else
        {
            isOnGround = false;
            Debug.Log("배 위 아님");
        }

        return isOnGround;
    }
}