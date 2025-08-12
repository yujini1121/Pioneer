using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TBallista : StructureBase
{
    [SerializeField] private bool isUsing = false;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackCooltime;

    [SerializeField] private float rotationSpeed;


    // TODO: 추후, ScriptableObject의 변수들과 연동 필요함~~~
    private int currentHP = 80;
    private float attackDamage = 25f;


    // 메서드 시작
    void Update()
    {
        
    }

    void Use()
    {
        if (isUsing) return;
        isUsing = true;
    }

    bool Detect()
    {
        enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
        return true;
    }

    void LookAt()
    {
        // 바라보게 하는 로직
    }
    
    void Fire()
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
