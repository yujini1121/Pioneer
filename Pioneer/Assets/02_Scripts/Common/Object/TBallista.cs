using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TBallista : StructureBase
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackCooltime;

    [SerializeField] private float turnSpeed;


    // TODO: 추후, ScriptableObject의 변수들과 연동 필요함~~~
    private int currentHP = 80;
    private float attackDamage = 25f;


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

    private bool DetectEnemy()
    {
        enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
        return true;
    }

    private void LookAt()
    {
        // 바라보게 하는 로직
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
