using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerBase : CreatureBase
{
    // 승무원 공통 설정
    public LayerMask targetLayer;

    // 상태 관리
    public enum CrewState { Wandering, Idle, Attacking }
    protected CrewState currentState = CrewState.Wandering;

    // 이동 및 대기 시간
    protected float moveDuration = 2f;
    protected float idleDuration = 4f;
    protected float stateTimer = 0f;
    private Vector3 moveDirection;

    // 공격 관리
    protected UnityEngine.Transform target;
    protected bool isShowingAttackBox = false;
    protected Coroutine attackRoutine;

    // 수리 관련 공통 변수
    [Header("수리 설정")]
    public bool isRepairing = false;
    protected DefenseObject targetRepairObject;
    protected int repairAmount = 30;
    protected bool isSecondPriorityStarted = false;

    // NavMeshAgent 공통 사용
    protected NavMeshAgent agent;

    public override void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        base.Start();
    }

    protected bool IsTargetInFOV()
    {
        if (target == null || fov == null)
            return false;

        // FOV에서 타겟 감지 수행
        fov.DetectTargets(targetLayer);
        return fov.visibleTargets.Contains(target);
    }

    protected virtual void Wander() // 배회
    {
        transform.position += moveDirection * speed * Time.deltaTime;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            EnterIdleState();
        }
    }

    protected virtual void Idle()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            EnterWanderingState();
        }
    }

    protected virtual void EnterWanderingState()
    {
        SetRandomDirection();
        currentState = CrewState.Wandering;
        stateTimer = moveDuration;
        Debug.Log($"{gameObject.name} - 랜덤 방향으로 이동 시작");
    }

    protected virtual void EnterIdleState()
    {
        currentState = CrewState.Idle;
        stateTimer = idleDuration;
        Debug.Log($"{gameObject.name} - 대기 상태로 전환");
    }

    protected void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    protected bool DetectTarget()
    {
        // attackRange 변수 사용
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(attackRange / 2f, 0.5f, attackRange / 2f),
            Quaternion.identity,
            targetLayer
        );

        float minDist = float.MaxValue;
        target = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                target = hit.transform;
            }
        }

        return target != null;
    }

    protected void LookAtTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f;

        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    protected virtual void StartRepair()
    {
        List<DefenseObject> needRepairList = MarinerManager.Instance.GetNeedsRepair();

        for (int i = 0; i < needRepairList.Count; i++)
        {
            DefenseObject obj = needRepairList[i];

            if (MarinerManager.Instance.TryOccupyRepairObject(obj, GetMarinerId()))
            {
                targetRepairObject = obj;

                if (MarinerManager.Instance.CanMarinerRepair(GetMarinerId(), targetRepairObject))
                {
                    Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 시작: {targetRepairObject.name}");
                    isRepairing = true;
                    StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
                    return;
                }
                else
                {
                    MarinerManager.Instance.ReleaseRepairObject(obj); // 점유 해제
                }
            }
        }

        // 점유할 수 있는 수리 대상이 없는 경우
        if (!isSecondPriorityStarted)
        {
            Debug.Log($"{GetCrewTypeName()} 수리 대상 없음 -> 2순위 행동 시작");
            isSecondPriorityStarted = true;
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    protected IEnumerator MoveToRepairObject(Vector3 targetPosition)
    {
        agent.SetDestination(targetPosition);

        while (!IsArrived())
        {
            yield return null;
        }

        StartCoroutine(RepairProcess());
    }


    protected virtual IEnumerator RepairProcess()
    {
        float repairDuration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 기본 수리 성공률 100% (일반 승무원용)
        bool repairSuccess = GetRepairSuccessRate() > Random.value;
        int actualRepairAmount = repairSuccess ? repairAmount : 0;

        Debug.Log($"{GetCrewTypeName()} {GetMarinerId()} 수리 {(repairSuccess ? "성공" : "실패")}: {targetRepairObject.name}/ 수리량: {actualRepairAmount}");
        targetRepairObject.Repair(actualRepairAmount);

        isRepairing = false;
        MarinerManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"{GetCrewTypeName()} 밤 도달 예외행동 시작");
            OnNightApproaching();
            yield break;
        }

        StartRepair();
        MarinerManager.Instance.ReleaseRepairObject(targetRepairObject);
    }

    public virtual IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"{GetCrewTypeName()} 2순위 행동 - 기본 구현");
        yield return new WaitForSeconds(1f);
    }

    // ===== NavMeshAgent 관련 공통 함수들 =====
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    public bool IsArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    public IEnumerator MoveToThenReset(Vector3 destination)
    {
        MoveTo(destination);

        while (!IsArrived())
        {
            yield return null;
        }

        agent.ResetPath();
        Debug.Log($"{GetCrewTypeName()} ResetPath 호출");
    }

    protected virtual float GetRepairSuccessRate()
    {
        return 1.0f; // 기본 100% 성공률 (일반 승무원)
    }

    protected virtual int GetMarinerId()
    {
        return 0; // 기본값
    }

    protected virtual string GetCrewTypeName()
    {
        return "승무원"; // 기본값
    }

    protected virtual void OnNightApproaching()
    {
        Debug.Log($"{GetCrewTypeName()} 기본 밤 처리");
    }

    protected virtual void OnDrawGizmos()
    {
        if (isShowingAttackBox)
        {
            Gizmos.color = Color.red;
            Vector3 boxCenter = transform.position + transform.forward * 1f;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 2f));
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(attackRange, 1f, attackRange));
    }
}