using System.Collections;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하진 않지만, 컴포넌트가 있다면 필요합니다.

[RequireComponent(typeof(Rigidbody))] // Rigidbody를 사용하므로 컴포넌트 강제
public class TitanAI : EnemyBase, IBegin
{
    private NavMeshAgent agent;


    // AI의 현재 상태를 명확하게 정의
    private enum State
    {
        MovingToTarget,
        Attacking
    }

    private State currentState;
    private Rigidbody rb;
    private GameObject mastObject;

    void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();

        // Rigidbody 설정: 평소에는 물리 영향을 받지 않도록 isKinematic = true
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임을 위해

        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;

        SetAttribute();
    }

    void Update()
    {
        // 이동 상태일 때만 타겟을 감지하고 움직입니다.
        if (currentState == State.MovingToTarget)
        {
            fov.DetectTargets(detectMask);
            MoveToTarget();

            // 시야에 타겟이 들어오면 '공격' 상태로 전환하고 공격 코루틴을 '한 번만' 호출합니다.
            if (fov.visibleTargets.Count > 0)
            {
                Transform targetToAttack = fov.visibleTargets[0];
                StartCoroutine(RushAttackSequence(targetToAttack));
            }
        }
        // 공격 상태일 때는 코루틴이 모든 것을 처리하므로 Update에서는 아무것도 하지 않습니다.
    }

    protected override void SetAttribute()
    {
        base.SetAttribute();
        maxHp = 30;
        attackDamage = 20;
        speed = 4;
        attackRange = 4;
        attackDelayTime = 4; // 전체 공격 쿨타임
        mastObject = SetMastTarget();
        currentAttackTarget = mastObject;

        fov.viewRadius = 1f;

        // 초기 상태를 '이동'으로 설정
        currentState = State.MovingToTarget;
    }

    // 장애물을 무시하고 일직선으로 이동하는 함수 (사용자 요청대로 유지)
    private void MoveToTarget()
    {
        if (currentAttackTarget != null)
        {
            // 목표 위치의 y값을 현재 y값으로 고정
            Vector3 targetPosition = new Vector3(
               currentAttackTarget.transform.position.x,
               transform.position.y,
               currentAttackTarget.transform.position.z);

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // 이 한 줄이 "NavMesh 바닥을 벗어나지 않게" 해줍니다.
            agent.Warp(nextPosition);

            // 회전 로직은 동일
            Vector3 direction = targetPosition - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            }
        }
    }

    // 공격의 전체 과정을 처리하는 코루틴
    private IEnumerator RushAttackSequence(Transform targetToAttack)
    {
        // 1. 상태를 '공격 중'으로 바꿔서 Update의 이동 로직을 막습니다.
        currentState = State.Attacking;

        Vector3 directionToTarget = (targetToAttack.position - transform.position).normalized;
        directionToTarget.y = 0; // Y축 회전 방지
        transform.rotation = Quaternion.LookRotation(directionToTarget);

        // 2. 공격 딜레이 (선딜: 돌진 전 준비 시간)
        Debug.Log("타겟 감지! 돌진을 준비합니다...");
        yield return new WaitForSeconds(1f);

        // --- 여기부터 돌진 로직이 변경됩니다 ---

        // 3. MoveTowards를 이용한 돌진
        Debug.Log("돌진!");

        float dashDuration = 0.2f; // 돌진에 걸리는 시간 (짧을수록 빠름)
        float dashSpeed = attackRange / dashDuration; // 목표 거리를 시간으로 나눠 정확한 속도 계산

        // 돌진 시작 위치와 목표 위치 계산
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * attackRange;
        targetPosition.y = transform.position.y; // Y축 고정

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            // 목표 위치를 향해 계산된 속도로 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dashSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 오차 보정을 위해 마지막에 위치를 정확히 맞춰줌
        // transform.position = targetPosition;

        // --- 돌진 로직 종료 ---

        // 4. 여기에 실제 데미지를 주는 로직 구현 (예: OverlapSphere로 주변 감지)
        Debug.Log("공격 발생! " + attackDamage + " 피해!");

        // 5. 남은 공격 딜레이 시간만큼 대기 (후딜: 공격 후 쿨타임)
        // 1초 선딜 + 0.3초 돌진 시간을 제외한 나머지 시간
        yield return new WaitForSeconds(attackDelayTime - 1.3f);

        // 6. 다시 이동 상태로 복귀
        Debug.Log("공격 완료. 다시 이동합니다.");
        currentState = State.MovingToTarget;
    }
}