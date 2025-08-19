using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class TBallista : StructureBase
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private Transform closestTarget;
    [SerializeField] private float turnSpeed;

    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackCooltime;

    // TODO: 추후, ScriptableObject의 변수들과 연동 필요함~~~
    [SerializeField] private int currentHP = 80;
    [SerializeField] private float attackDamage = 25f;


    // 메서드 시작
    void Update()
    {
        if (currentHP <= 0)
        {
            WhenDestroy();
            return;
        }

        if (isUsing)    
        { 
            Use();
            DetectEnemy(); 
            LookAt(); 
            Fire(); 
        }
        else            
        { 
            UnUse(); 
        }
    }

    private void DetectEnemy()
    {
        enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);

    }

    private void LookAt()
    {
        // 가장 가까운 적을 바라보게 하는 로직 
        // 현재는 0번이 가장 가까운 에너미라고 가정 (추후 수정)
        closestTarget = enemyColliders[0].transform;
        float minDistance = Mathf.Infinity;
    }
    
    private void Fire()
    {
        // 화살 오브젝트 생성 및 발사하는 로직
    }

    void OnDrawGizmos()
    {
        foreach (var collider in enemyColliders)
        {
            if (collider == null) continue;

            Gizmos.color = Color.green;
            Vector3 center = collider.transform.position;
            Gizmos.DrawSphere(center, 0.5f);
        }
    }
}
