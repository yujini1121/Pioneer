using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public class TBallista : StructureBase
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private Transform closestTarget;
    [SerializeField] private float turnSpeed;
    [SerializeField] private GameObject bolt;
    [SerializeField] private GameObject boltFirePoint;
    [SerializeField] private GameObject controlPoint;

    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float curCooldown = 0f;

    [SerializeField] private Transform nearest;

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
            Select(); 
            LookAt(); 
            Fire(); 
        }
        else            
        { 
            UnUse(); 
        }
    }

    private void Select()
    {
        enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
        //foreach (var col in enemyColliders) { Debug.Log(col.name); }

        if (enemyColliders.Length == 0) { nearest = null; return; }
  
        float minDistance = Mathf.Infinity;
        foreach (var col in enemyColliders)
        {
            float distance = Vector3.SqrMagnitude(transform.position - col.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = col.transform;
            }
        }

        //Debug.Log(nearest.name);
    }

    private void LookAt()
    {
        // 가장 가까운 적을 바라보게 하는 로직 

        if (nearest == null) return;

        Vector3 dir = (nearest.position - transform.position).normalized;
        Quaternion targetDir = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDir, turnSpeed * Time.deltaTime);
    }
    
    private void Fire()
    {
        // 화살 오브젝트 생성 및 발사하는 로직

        if (nearest == null) return;

        if (curCooldown > 0f) { curCooldown -= Time.deltaTime; return; }

        curCooldown = attackCooldown;

        //bolt = GetArrowFromPool();
        //bolt.transform.position = firePoint.position;

    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 감지 범위 및 공격 범위 표시
        Handles.color = Color.white;
        Handles.DrawWireDisc(transform.position, Vector3.up, attackRange);

        // 감지된 적 표시
        foreach (var collider in enemyColliders)
        {
            if (collider == null) continue;

            Gizmos.color = Color.green;
            Vector3 center = collider.transform.position;
            Gizmos.DrawSphere(center, 0.5f);
        }
    }
#endif

}
