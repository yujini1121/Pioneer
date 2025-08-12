using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Ballista : MonoBehaviour, IBegin
{
    private enum DetectType { Circle, Rectangle }

    [SerializeField] private SInstallableObjectDataSO objectData;


    [Header("발리스타 옵션")]
    [Tooltip("공격 범위(½)")][SerializeField] private Vector3 attackHalfBound;
    [Tooltip("적 레이어")][SerializeField] private LayerMask enemyLayer;
    [Tooltip("사수 위치")][SerializeField] private Transform gunnerPos;
    [Tooltip("화살 풀")][SerializeField] private Transform boltPool;

    [Header("디버그")]
    [Tooltip("체크 시 기즈모 출력")][SerializeField] private bool drawGizmos;
    [Tooltip("적 감지")][SerializeField] private bool enemyDetect;
    [Tooltip("가장 가까운 적")][SerializeField] private Transform nearestTrans;

    private List<GameObject> bolts = new List<GameObject>();
    private Vector3 centerVec;
    private GameObject gunner;
    private float centerVecY;
    private int poolIndex = 0;
    private float curCooldown;

    private void Start()
    {
        centerVecY = GetComponent<SphereCollider>().center.y;

        for (int i = 0; i < boltPool.childCount; i++)
        {
            bolts.Add(boltPool.GetChild(i).gameObject);
        }

        //currentHP = objectData.maxHp;
    }

    private void Update()
    {
        centerVec = transform.position;
        centerVec.y = centerVecY;

        if (isUsing)
        {
            if (currentHP <= 0)
            {
                gunner.transform.parent = null;

                foreach (Behaviour component in gunner.GetComponents<Behaviour>())
                {
                    if (component is MeshFilter || component is MeshRenderer || component is Transform || component == null)
                        continue;

                    component.enabled = true;
                }
                gunner.GetComponent<CapsuleCollider>().enabled = true;

                Destroy(gameObject);
            }

            if (detectType == DetectType.Rectangle)
            {
                colliders = Physics.OverlapBox(centerVec, attackHalfBound, transform.rotation, enemyLayer, QueryTriggerInteraction.Ignore);
            }
            else if (detectType == DetectType.Circle)
            {
                colliders = Physics.OverlapSphere(centerVec, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
            }

            enemyDetect = colliders.Length > 0;
            if (enemyDetect)
            {
                LookAt();
                Fire();
            }
        }
    }

    public void Use(GameObject _gunner)
    {
        if (isUsing) return;

        isUsing = true;
        gunner = _gunner;
        gunner.transform.SetParent(gunnerPos);
        gunner.transform.localPosition = Vector3.zero;
    }

    private void LookAt()
    {
        nearestTrans = colliders[0].transform;
        float minDistance = Mathf.Infinity;
        foreach (var col in colliders)
        {
            float distance = Vector3.SqrMagnitude(transform.position - col.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTrans = col.transform;
            }
        }

        if (smoothRotate)
        {
            Vector3 direction = nearestTrans.position - transform.position;
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 direction = nearestTrans.position - transform.position;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Fire()
    {
        if (curCooldown < 0)
        {
            curCooldown = attackCooldown;
            StartCoroutine(FireBolt(boltPool.GetChild(poolIndex).gameObject, nearestTrans));

            poolIndex++;
            poolIndex %= boltPool.childCount;
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

        Vector3 targetPos = bolt.transform.position + bolt.transform.forward * attackRange;
        Vector3 prevPos = bolt.transform.position;
        Vector3 dir = bolt.transform.forward;
        float traveled = 0f;

        while (traveled < attackRange)
        {
            float moveStep = attackSpeed * Time.deltaTime;
            Vector3 nextPos = bolt.transform.position + dir * moveStep;

            if (Physics.BoxCast(prevPos, boltHalfSize, dir, out RaycastHit hit, Quaternion.LookRotation(dir), moveStep, enemyLayer))
            {
                Debug.Log("Hit: " + hit.collider.name);
                break;
            }

            bolt.transform.position = nextPos;
            traveled += moveStep;
            prevPos = nextPos;

            yield return null;
        }

        bolt.transform.parent = boltPool;
        bolt.transform.localPosition = Vector3.zero;
        bolt.transform.localRotation = Quaternion.identity;
        bolt.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (drawGizmos && isUsing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(centerVec, centerVec + transform.forward * attackRange);

            foreach (GameObject bolt in bolts)
            {
                if (!bolt.activeSelf) continue;

                Gizmos.color = Color.green;
                Gizmos.matrix = Matrix4x4.TRS(bolt.transform.position, bolt.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boltHalfSize * 2f);
                Gizmos.matrix = Matrix4x4.identity;
            }

            foreach (var collider in colliders)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawSphere(collider.transform.position, 1.1f);
            }

            if (detectType == DetectType.Rectangle)
            {
                Gizmos.color = Color.white;
                Gizmos.matrix = Matrix4x4.TRS(centerVec, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, attackHalfBound * 2f);
                Gizmos.matrix = Matrix4x4.identity;
            }
#if UNITY_EDITOR
            else if (detectType == DetectType.Circle)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(centerVec, Vector3.up, attackRange);
            }
#endif
        }
    }
}
