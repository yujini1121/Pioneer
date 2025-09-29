using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/*
{ 속성 }
hp 50
공격력 10
이동속도 1
감지 범위 2
공격 범위 4
선제 시간 2
경직 3

1. 탐색
    - 현재 자신과 가까운 설치형 오브젝트들을 순서대로 공격하고 감지 범위 안에 더 이상 설치형 오브젝트들이 없을 경우 돛대를 파괴하러감
    - 타겟이 만약 null이면 경직 3초 후 다시 탐색
2. 이동
    - 타겟으로 이동 중 공격 받으면 공격 받은 대상을 공격하러 감
3. 공격
    - 반지름 4정도의 범위를 공격 범위로 하고 공격 범위 내 있는 모든 설치형 오브젝트의 피를 10 깎음
    - 공격 후 경직 3초

==============================================
- 돛대 타겟 디폴트
- 공격 받으면 공격 받은 대상으로 타겟 변경
*/

public class CrawlerAI : EnemyBase, IBegin
{
    // 네브 메시 
    private NavMeshAgent agent;

    // 감지된 오브젝트 가까운 순으로 정렬할 리스트
    List<Transform> sortedTarget;

    private int closeTarget = 0;

    void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();
    }

    void Update()
    {
        fov.DetectTargets(detectMask);

        bool isCanMove = CanMove();
        if (isCanMove == false)
        {
            SetMastTarget();
        }

        if (isCanMove)
        {
            Move();
        }
        else if(CanAttack())
        {
            Attack();
        }
    }

    // 기본 세팅
    protected override void SetAttribute()
    {
        maxHp = 50;
        hp = maxHp;
        attackDamage = 10;
        speed = 1;
        fov.viewRadius = 6;
        attackRange = 4;
        attackDelayTime = 3; 
    }
    
    private bool CanMove()
    {
        return fov.visibleTargets.Any(target => detectMask == (detectMask | (1 << target.gameObject.layer)));
    }

    private bool CanAttack()
    {
        return true;
    }

    private void Move()
    {
        // 감지 리스트에 있는 설치형 오브젝트 중 가장 가까운 오브젝트부터 공격하기 부서져서 공격이 끝나면 다음으로 가까운 애 공격하러 가기

        // fov에 감지된 오브젝트 가까운 순으로 정렬
        SortCloseObj();
        currentAttackTarget = sortedTarget[closeTarget].gameObject;
        Vector3 destination = currentAttackTarget.GetComponent<Collider>().ClosestPoint(transform.position);
        if (sortedTarget.Count > 0)
        {
            if (Vector3.Distance(agent.destination, destination) > 0.5f)
            {
                agent.SetDestination(destination);
            }
        }
    }

    private void Attack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, detectMask);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            CommonBase targetBase = hitColliders[i].GetComponent<CommonBase>();
            targetBase.TakeDamage(attackDamage, this.gameObject);
        }
    }

    private void SortCloseObj()
    {
        sortedTarget = fov.visibleTargets.OrderBy(target => Vector3.Distance(transform.position, target.transform.position)).ToList();
    }
}
