using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
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

    private bool isAttack = false;

    void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;

        SetAttribute();
    }

    void Update()
    {
        if (currentState == State.MovingToTarget)
        {
            fov.DetectTargets(detectMask);
            MoveToTarget();

            if (fov.visibleTargets.Count > 0)
            {
                Transform targetToAttack = fov.visibleTargets[0];
                StartCoroutine(RushAttackSequence(targetToAttack));
            }
        }
    }

    protected override void SetAttribute()
    {
        base.SetAttribute();
        maxHp = 30;
        attackDamage = 20;
        speed = 4;
        attackRange = 4;
        attackDelayTime = 4;
        mastObject = SetMastTarget();
        currentAttackTarget = mastObject;

        fov.viewRadius = 1f;

        currentState = State.MovingToTarget;
    }

    private void MoveToTarget()
    {
        if (currentAttackTarget != null)
        {
            Vector3 targetPosition = new Vector3(
               currentAttackTarget.transform.position.x,
               transform.position.y,
               currentAttackTarget.transform.position.z);

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // 바닥 나가지 말라고 추가한 로직
            agent.Warp(nextPosition);

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
        currentState = State.Attacking;

        Vector3 directionToTarget = (targetToAttack.position - transform.position).normalized;
        directionToTarget.y = 0;
        transform.rotation = Quaternion.LookRotation(directionToTarget);

        yield return new WaitForSeconds(1f);

        float dashDuration = 0.2f; // 돌진에 걸리는 시간 (짧을수록 빠름)
        float dashSpeed = attackRange / dashDuration;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + transform.forward * attackRange;
        targetPosition.y = transform.position.y;

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dashSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_Titan);

        // 여기에 데미지를 주는 로직
        Debug.Log("공격 발생! " + attackDamage + " 피해!");
        Collider[] hitColliders = DetectAttackRange();
        if(hitColliders != null)
        {
            foreach(Collider collider in hitColliders)
            {
                if (collider.gameObject == this.gameObject)
                {
                    continue;
                }

                CommonBase hitColCommonBase = collider.GetComponent<CommonBase>();
                if (hitColCommonBase != null)
                {
                    hitColCommonBase.TakeDamage(attackDamage, this.gameObject);
                }
                else
                    continue;
            }
        }

        yield return new WaitForSeconds(attackDelayTime - 1.3f);

        currentState = State.MovingToTarget;
    }
}