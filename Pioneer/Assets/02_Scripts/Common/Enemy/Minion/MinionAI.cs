using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/*
[탐색]
- 타겟 오브젝트 돛대 설정 및 감지
- 살아있는 생성한 둥지가 2개 미만 => 둥지 생성 (15초 대기 => 배회? 이동?)
==========================================

1. 둥지 생성 로직 작성
- 생성 가능한 둥지 수가 미니언 한 마리당 하나
- 생성된 둥지에서 생성되는 미니언 두마리, 두마리 생성후 파괴
- 둥지에서 생성된 미니언과 스포너에서 생성된 미니언 상관없이 무조건 둥지 생성
 
 ==========================================
250812
- 새로운 목표물이 감지된 경우 이전 이동 목적지를 저장하고
- 새로운 목표물이 공격 범위 안에 있다면 이동을 멈추고 공격
- 감지 범위 안인데 공격 범위 밖이면 공격할 새로운 목표물 위치로 이동
- 공격할 목표물이 없어졌으면 원래 행동(원래 목적지로 복귀)

+ Idle 배회할건지? 아니면 그 자리 정지할건지 (배회가 나을지도)
+ 함수 뗄 수 있으면 분리해서 작성
+ 둥지 생성 제대로 하는지 확인도 해야함
 */

public class MinionAI : EnemyBase, IBegin
{
    [Header("감지 가능한 레이어")]
    [SerializeField] private LayerMask detectLayer;

    [Header("둥지 프리팹")]
    [SerializeField] private GameObject nestPrefab;

    [Header("공격 콜라이더")]
    [SerializeField] private GameObject attackCollider;

    private NavMeshAgent agent;

    // 
    private bool isNestCreated = false;
    private float nestCool = 15f;
    private float nestCreationTime = -1f;

    private bool isOnGround = false;

    private Transform currentAttackTarget = null;
    private Vector3 originalDestination;

    void Start()
    {
        SetAttribute();
        agent = GetComponent<NavMeshAgent>();
        if(agent != null)
        {
            agent.speed = speed;
        }
    }

    void Update()
    {       
        fov.DetectTargets(detectLayer);

        //// 수정 해야함
        if (targetObject != null)
        {
            // NavMeshAgent 목적지 계속 갱신
            agent.SetDestination(targetObject.transform.position);
        }

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
        else
        {
            Idle();
        }
    }

    protected override void SetAttribute()
    {
        hp = 20;
        attackDamage = 1;
        speed = 2f;
        detectionRange = 4f;
        attackRange = 2f;
        attackDelayTime = 2f;
        idleTime = 2f;

        SetMastTarget();

        fov.viewRadius = attackRange;
    }

    #region 행동 조건 검사
    private bool CanCreateNest()
    {
        return  isOnGround && !isNestCreated && Time.time >= nestCreationTime && nestCreationTime != -1f;
    }

    private bool CanAttack()
    {
        return fov.visibleTargets.Count > 0;
    }

    private bool CanMove()
    {
        if(targetObject != null)
            return true;

        return false;
    }
    #endregion

    // 둥지 생성
    void CreateNest()
    {
        Instantiate(nestPrefab, transform.position, Quaternion.identity);
        isNestCreated = true;
    }

    // 공격
    void Attack()
    {  
        // 감지 범위 내에 감지 가능한 적들이 존재하는지?
        if(fov.visibleTargets.Count > 0)
        {
            // 공격 범위 안에 있는 콜라이더들 가지고 오기
            Collider[] detectColliders = DetectAttackRange(attackRange);

            if(detectColliders.Length > 0)
            {
                currentAttackTarget = FindClosestTarget(detectColliders);
            }            
        }
    }

    /// <summary>
    /// 공격 범위 내에서 가장 가까운 적 찾기
    /// </summary>
    /// <returns></returns>
    private Transform FindClosestTarget(Collider[] detectColliders)
    {
        Transform closestTarget = null;
        float closestDis = float.MaxValue;

        foreach (var target in detectColliders) // FOV 리스트가 아니라 DetectAttackRange에서 반환된 콜라이더 배열에서 찾기
        {
            float dis = Vector3.Distance(transform.position, target.transform.position);
            if (dis < closestDis)
            {
                closestDis = dis;
                closestTarget = target.transform;
            }
        }

        return closestTarget;
    }

    // 이동
    void Move()
    {
        if (targetObject != null && agent.destination != targetObject.transform.position)
        {
            agent.SetDestination(targetObject.transform.position);
        }
    }

    // 대기
    void Idle()
    {
        // 가만히? 배회? ????????????
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;

            if(nestCreationTime == -1f)
            {
                nestCreationTime = Time.time + nestCool;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = false;
        }
    }
}
