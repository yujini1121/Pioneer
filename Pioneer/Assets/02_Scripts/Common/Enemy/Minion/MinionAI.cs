using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/*
[탐색]
- 타겟 오브젝트 돛대 설정 및 감지
- 살아있는 생성한 둥지가 2개 미만 => 둥지 생성 (15초 대기 => 배회? 이동?)

*/
public class MinionAI : EnemyBase, IBegin
{
    [Header("감지 가능한 레이어")]
    [SerializeField] private LayerMask detectLayer;

    [Header("둥지 프리팹")]
    [SerializeField] private GameObject nestPrefab;

    private NavMeshAgent agent;

    private int nestCount = 0;
    private float nestCool = 15f;

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
        // 갑판 위인지 확인도 해야함
        return nestCount < 2 && Time.time > nestCool;
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

    // 대기
    void Idle()
    {

    }
}
