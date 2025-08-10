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
 */

public class MinionAI : EnemyBase, IBegin
{
    [Header("감지 가능한 레이어")]
    [SerializeField] private LayerMask detectLayer;

    [Header("둥지 프리팹")]
    [SerializeField] private GameObject nestPrefab;

    private NavMeshAgent agent;

    private bool isNestCreated = false;
    private float nestCool = 15f;
    private float nestCreationTime = -1f;

    private bool isOnGround = false;

    public override void Init()
    {
        base.Init();
        SetAttribute();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {       
        fov.DetectTargets(detectLayer);

        if (CanCreateNest())
        {
            CreateNest();
        }
        else if(CanMove())
        {
            Move();
        }
        else if(CanAttack())
        {
            Attack();
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
        return  isOnGround && !isNestCreated && Time.time > nestCool && nestCreationTime != -1f;
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
        Transform closesTarget = null;
        float closesDis = float.MaxValue;

        foreach(var target in fov.visibleTargets)
        {
            float dis = Vector3.Distance(transform.position, target.position);
            if(dis < closesDis)
            {
                closesDis = dis;
                closesTarget = target;
            }
        }

        if(closesTarget != null)
        {
            Vector3 dir = closesTarget.position - transform.position;
            dir.y = 0f;

            if(dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            // 공격
        }
    }

    // 이동
    void Move()
    {
        if (targetObject != null && agent.destination != targetObject.transform.position)
        {
            agent.SetDestination(targetObject.transform.position);
        }
    }

   void StopMoving()
    {

    }

    // 대기
    void Idle()
    {
        // 가만히? 배회?
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
