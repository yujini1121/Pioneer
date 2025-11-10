using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Ballista : StructureBase, IBegin
{
    [Header("회전")]
    [SerializeField] private float rotationSpeed = 50f; // 항상 부드러운 회전

    [Header("발리스타 옵션")]
    [SerializeField] private float attackPower = 25f;
    [SerializeField] private float attackRange = 8f;   // 원형 탐지 고정
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackSpeed = 4f;
    [SerializeField] private Vector3 boltHalfSize = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] private Transform gunnerPos;
    [SerializeField] private Transform boltPool;

    [Header("디버그")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool enemyDetect;
    [SerializeField] private Collider[] colliders;
    [SerializeField] private Transform nearestTrans;

    private readonly List<GameObject> bolts = new List<GameObject>();
    private GameObject gunner;
    private float centerVecY;
    private int poolIndex = 0;
    private float curCooldown = 0f;

    private void Start()
    {
        var sc = GetComponent<SphereCollider>();
        centerVecY = sc ? sc.center.y : transform.position.y;

        for (int i = 0; i < boltPool.childCount; i++)
            bolts.Add(boltPool.GetChild(i).gameObject);
    }

    private void Update()
    {
        if (!isUsing) return;

        // ▼ 변경: 베이스 HP 사용
        if (CurrentHp <= 0f)
        {
            if (gunner)
            {
                gunner.transform.parent = null;
                foreach (var component in gunner.GetComponents<Behaviour>())
                {
                    if (component is MeshFilter || component is MeshRenderer || component is Transform || component == null)
                        continue;
                    component.enabled = true;
                }
                var cap = gunner.GetComponent<CapsuleCollider>();
                if (cap) cap.enabled = true;
            }
            Destroy(gameObject);
            return;
        }

        Vector3 center = transform.position; center.y = centerVecY;

        // 원형 탐지 고정
        colliders = Physics.OverlapSphere(center, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);

        enemyDetect = colliders != null && colliders.Length > 0;
        if (!enemyDetect) return;

        LookAt();
        Fire();
    }

    public void Use(GameObject _gunner)
    {
        if (isUsing) return;
        base.Use();

        gunner = _gunner;
        gunner.transform.SetParent(gunnerPos);
        gunner.transform.localPosition = Vector3.zero;
    }

    private void LookAt()
    {
        nearestTrans = colliders[0].transform;
        float minSqr = Mathf.Infinity;
        Vector3 selfPos = transform.position;

        foreach (var col in colliders)
        {
            float d = (selfPos - col.transform.position).sqrMagnitude;
            if (d < minSqr) { minSqr = d; nearestTrans = col.transform; }
        }

        Vector3 dir = nearestTrans.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void Fire()
    {
        if (curCooldown <= 0f)
        {
            curCooldown = attackCooldown;
            StartCoroutine(FireBolt(boltPool.GetChild(poolIndex).gameObject, nearestTrans));

            poolIndex = (poolIndex + 1) % boltPool.childCount;
        }
        else
        {
            curCooldown -= Time.deltaTime;
        }
    }

    private IEnumerator FireBolt(GameObject bolt, Transform target)
    {
        bolt.SetActive(true);
        bolt.transform.parent = null;

        Vector3 prevPos = bolt.transform.position;
        Vector3 dir = bolt.transform.forward;
        float traveled = 0f;

        while (traveled < attackRange)
        {
            float step = attackSpeed * Time.deltaTime;
            Vector3 nextPos = bolt.transform.position + dir * step;

            if (Physics.BoxCast(prevPos, boltHalfSize, dir, out RaycastHit hit, Quaternion.LookRotation(dir), step, enemyLayer))
            {
                Debug.Log("Hit: " + hit.collider.name);
                break;
            }

            bolt.transform.position = nextPos;
            traveled += step;
            prevPos = nextPos;

            yield return null;
        }

        bolt.transform.parent = boltPool;
        bolt.transform.localPosition = Vector3.zero;
        bolt.transform.localRotation = Quaternion.identity;
        bolt.SetActive(false);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        // 베이스: 상호작용 반경
        base.OnDrawGizmos();
        if (!drawGizmos) return;

        // 공격 사거리(원)
        Vector3 center = GetComponent<Collider>() ? GetComponent<Collider>().bounds.center : transform.position;
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, Vector3.up, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + transform.forward * attackRange);
    }
#endif
}
