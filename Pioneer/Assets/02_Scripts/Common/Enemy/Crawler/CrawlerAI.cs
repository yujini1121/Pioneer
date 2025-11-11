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
- 돛대 타겟 디폴트 *
- 공격 받으면 공격 받은 대상으로 타겟 변경
- 플레이어한테 공격 받으면 갑자기 없어짐... 왜이래 ..
*/

public class CrawlerAI : EnemyBase, IBegin
{
    // 네브 메시 
    private NavMeshAgent agent;

    // 감지된 오브젝트 가까운 순으로 정렬할 리스트
    List<Transform> sortedTarget;

    private int closeTarget = 0;

    private GameObject revengeTarget;

    private bool isAttack = false;

    private float attackTimer = 0f;

    void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        SetAttribute();
    }

    void Update()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            return;
        }

        fov.DetectTargets(detectMask);

        if (fov.visibleTargets.Count == 0)
        {
            currentAttackTarget = SetMastTarget();
        }

        if (CanAttack())
        {
            Attack();
        }
        else if (CanMove())
        {
            Move();
        }
    }

    // 기본 세팅
    protected override void SetAttribute()
    {
        maxHp = 50;
        hp = maxHp;
        attackDamage = 10;
        speed = 1;
        fov.viewRadius = 4;
        attackRange = 2;
        attackDelayTime = 3;
        // currentAttackTarget = SetMastTarget();
    }

    private bool CanMove()
    {
        return fov.visibleTargets.Any(target => detectMask == (detectMask | (1 << target.gameObject.layer))) || currentAttackTarget != null;
    }

    private bool CanAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, detectMask);

        if (hitColliders.Length > 0 && attackTimer <= 0)
            return true;
        else
            return false;
    }

    private void Move()
    {
        // 감지 리스트에 있는 설치형 오브젝트 중 가장 가까운 오브젝트부터 공격하기 부서져서 공격이 끝나면 다음으로 가까운 애 공격하러 가기

        // fov에 감지된 오브젝트 가까운 순으로 정렬
        if (fov.visibleTargets.Count > 0)
        {
            SortCloseObj();
            currentAttackTarget = sortedTarget[closeTarget].gameObject;
        }

        Vector3 destination = currentAttackTarget.GetComponent<Collider>().ClosestPoint(transform.position);
        if (Vector3.Distance(agent.destination, destination) > 0.1f)
        {
            agent.SetDestination(destination);
        }
    }

    private void Attack()
    {
        AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_Crawler);
        // Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, detectMask);
        Collider[] hitColliders = DetectAttackRange();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            GameObject currentObject = hitColliders[i].gameObject;

            UnityEngine.Debug.Log($"[검사 시작] 이름: {currentObject.name}, 레이어: {LayerMask.LayerToName(currentObject.layer)}");

            CommonBase targetBase = currentObject.GetComponent<CommonBase>();

            if (targetBase == null)
            {
                UnityEngine.Debug.LogError($"-> 실패: '{currentObject.name}'에서 CommonBase 컴포넌트를 찾을 수 없습니다! (targetBase is null)");
            }
            else
            {
                UnityEngine.Debug.Log($"-> 성공: '{currentObject.name}'에서 CommonBase 컴포넌트를 찾았습니다.");

                if (targetBase.IsDead)
                {
                    if (fov.visibleTargets.Count > 0)
                    {
                        SortCloseObj();
                        currentAttackTarget = fov.visibleTargets[closeTarget].gameObject;
                    }
                    return;
                }
                // 플레이어, 에너미, 승무원만
                /*UnityEngine.Debug.Log("CrawlerAI 타겟 맞는 사운드 출력");
                AudioManager.instance.PlaySfx(AudioManager.SFX.Hit);*/
                targetBase.TakeDamage(attackDamage, this.gameObject);
            }
        }
        attackTimer = attackDelayTime;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BeforeAttack_Crawler);
    }

    private void SortCloseObj()
    {
        sortedTarget = fov.visibleTargets.OrderBy(target => Vector3.Distance(transform.position, target.transform.position)).ToList();
    }
}