using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : EnemyBase, IBegin
{
    [Header("감지 가능한 레이어 설정")]
    [SerializeField] LayerMask detectMask;

    [Header("공격 범위 오브젝트")]
    [SerializeField] GameObject attackBox;

    public int nestCount = 0;
    public GameObject nestPrefab;

    private float nestCoolTime = 15f;
    private float nextNestCool;
    private NavMeshAgent agent;
    private GameObject fallback;

    void Init()
    {
        //SetAttribute();

        agent = GetComponent<NavMeshAgent>();

        fallback = GameObject.FindGameObjectWithTag("Engine");
    }

    void Update()
    {
        DetectTarget();

        if (CanCreateNest())
        {
            CreateNest();
        }
        else if (CanAttack())
        {
            Attack();
        }
        else if (CanMove())
        {
            Move();
        }
        else
        {
            Idle();
        }
    }

    //protected override void SetAttribute()
    //{
    //    hp = 20;
    //    attackPower = 1;
    //    speed = 2.0f;
    //    detectionRange = 4;
    //    attackRange = 2;
    //    attackVisualTime = 1.0f;
    //    restTime = 2.0f;
    //    targetObject = GameObject.FindGameObjectWithTag("Engine");
    //}

    private bool CanCreateNest()
    {
        return nestCount < 2 && Time.time > nextNestCool;
    }

    private bool CanAttack()
    {
        if (targetObject == null)
            return false;

        Vector3 boxCenter = transform.position + transform.forward * (attackRange / 2f);
        Vector3 boxSize = new Vector3(attackRange / 2f, 1f, attackRange / 2f);
        Collider[] targets = Physics.OverlapBox(boxCenter, boxSize, transform.rotation, detectMask);

        foreach (Collider target in targets)
        {
            if (target.gameObject == targetObject)
                return true;
        }

        return false;
    }

    private bool CanMove()
    {
        if (targetObject != null)
        {
            return true;
        }
        return false;
    }

    private void CreateNest()
    {
        Instantiate(nestPrefab, transform.position, Quaternion.identity);
        nestCount++;
        nextNestCool = Time.time + nestCoolTime;
    }

    private void DetectTarget()
    {
        Vector3 boxSize = new Vector3(detectionRange / 2f, 1f, detectionRange / 2f);
        float closeDistance = float.MaxValue;
        GameObject closeTarget = null;

        Collider[] targets = Physics.OverlapBox(transform.position, boxSize, Quaternion.identity, detectMask);

        foreach (Collider target in targets)
        {
            float targetDistance = Vector3.Distance(transform.position, target.transform.position);

            if (targetDistance < closeDistance)
            {
                closeDistance = targetDistance;
                closeTarget = target.gameObject;
            }
        }

        if (closeTarget != null)
        {
            targetObject = closeTarget;
            agent.SetDestination(targetObject.transform.position);
        }
        else
        {
            if (fallback != null)
            {
                targetObject = fallback;
            }
        }
    }

    IEnumerator AttackVisualRoutine()
    {
        Vector3 attackBoxPos = transform.position + transform.forward * (attackRange / 2f);
        attackBox.transform.position = attackBoxPos;
        attackBox.SetActive(true);

        yield return new WaitForSeconds(1f);

        attackBox.SetActive(false);
    }

    private void Attack()
    {
        Vector3 dir = targetObject.transform.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        StartCoroutine(AttackVisualRoutine());
        // 데미지 처리
    }

    private void Move()
    {
        if (targetObject != null && agent.destination != targetObject.transform.position)
        {
            agent.SetDestination(targetObject.transform.position);
        }
    }

    private void Idle()
    {

    }
}
