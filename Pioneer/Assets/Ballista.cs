using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Ballista : MonoBehaviour
{
    private enum DetectType { Circle, Rectangle }

    [SerializeField] private SInstallableObjectDataSO objectData;

    [Header("부드러운 회전")]
    [Tooltip("체크 시 부드럽게 회전")][SerializeField] private bool smoothRotate;
    [Tooltip("회전 속도")][SerializeField] private float rotationSpeed;

    [Header("발리스타 옵션")]
    [Tooltip("탐지 방식")][SerializeField] private DetectType detectType;
    [Tooltip("현재 체력")][SerializeField] private float currentHP;
    [Tooltip("공격력")][SerializeField] private float attackPower;
    [Tooltip("공격 사거리")][SerializeField] private float attackRange;
    [Tooltip("공격 범위(½)")][SerializeField] private Vector3 attackHalfBound;
    [Tooltip("공격 쿨타임")][SerializeField] private float attackCooldown;
    [Tooltip("발사 속도")][SerializeField] private float attackSpeed;
    [Tooltip("화살 크기(½)")][SerializeField] private Vector3 boltHalfSize;
    [Tooltip("적 레이어")][SerializeField] private LayerMask enemyLayer;
    [Tooltip("사수 위치")][SerializeField] private Transform gunnerPos;
    [Tooltip("화살 풀")][SerializeField] private Transform boltPool;

    [Header("디버그")]
    [Tooltip("체크 시 기즈모 출력")][SerializeField] private bool drawGizmos;
    [Tooltip("사용 중")][SerializeField] private bool isUsing;
    [Tooltip("적 감지")][SerializeField] private bool enemyDetect;
    [Tooltip("감지한 적 목록")][SerializeField] private Collider[] colliders;
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

        currentHP = objectData.maxHp;
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

            //사각형 범위 탐지
            if (detectType == DetectType.Rectangle)
            {
                colliders = Physics.OverlapBox(centerVec, attackHalfBound, transform.rotation, enemyLayer, QueryTriggerInteraction.Ignore);
            }
            //구 범위 탐지
            else if (detectType == DetectType.Circle)
            {
                colliders = Physics.OverlapSphere(centerVec, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
            }

            if (colliders.Length > 0)
            {
                LookAt();
                Fire();
            }
        }
    }

    //선원 코드에서 발리스타를 어떻게 판별하느냐에 따라 로직 변경 필요
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

            poolIndex ++;
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

            //현재 화살의 머리 부분만 충돌판정이 있음. 몸통이나 꼬리 충돌시에도 맞은 판정으로 하려면 수정 필요
            if (Physics.BoxCast(prevPos, boltHalfSize, dir, out RaycastHit hit, Quaternion.LookRotation(dir), moveStep, enemyLayer))
            {
                Debug.Log("Hit: " + hit.collider.name);
                //맞은 대상 처리
                
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
            //궤적
            Gizmos.color = Color.red;
            Gizmos.DrawLine(centerVec, centerVec + transform.forward * attackRange);

            //발사한 화살
            foreach (GameObject bolt in bolts)
            {
                if (bolt.activeSelf == false) continue;

                Gizmos.color = Color.green;
                Gizmos.matrix = Matrix4x4.TRS(bolt.transform.position, bolt.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boltHalfSize * 2f);
                Gizmos.matrix = Matrix4x4.identity;
            }

            //적
            foreach (var collider in colliders)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Vector3 center = collider.transform.position;
                Gizmos.DrawSphere(center, 1.1f);
            }

            if (detectType == DetectType.Rectangle)
            {
                Gizmos.color = Color.white;
                Gizmos.matrix = Matrix4x4.TRS(centerVec, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, attackHalfBound * 2f);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (detectType == DetectType.Circle)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(centerVec, Vector3.up, attackRange);
            }
        }
    }
}