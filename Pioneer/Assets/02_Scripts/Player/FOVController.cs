using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FOVController : MonoBehaviour
{
    [Header("시야 범위(원)")]
    [SerializeField] private float viewRadius = 10f;

    [Header("시야 각")]
    [SerializeField] private float viewAngle = 60f;

    [Header("시야 감지 간격")]
    [SerializeField] private float detectionInterval = 0.2f;

    [Header("최대 감지 에너미 수")]
    [SerializeField] private int maxTargets = 10;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;

    private Collider[] targetColliders;

    private void Start()
    {
        targetColliders = new Collider[maxTargets];
        StartCoroutine(DetectRatgets());
    }

    IEnumerator DetectRatgets()
    {
        while(true)
        {
            yield return new WaitForSeconds(detectionInterval);

            DetectTargets();
        }
    }

    // 원형 범위 안에서 대상 찾기
    // 시야각 내에 있는지 확인
    // 장애물 있는지 레이캐스트 검사
    private void DetectTargets()
    {
        int targets = Physics.OverlapSphereNonAlloc(transform.position, viewRadius, targetColliders, enemyMask);
        
        for(int i = 0; i < targets; i++)
        {
            Transform targetTransform = targetColliders[i].transform;
            Vector3 targetDir = (targetTransform.position - transform.position).normalized;

            if(Vector3.Angle(transform.forward, targetDir) < (viewAngle / 2))
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
                
                if (!Physics.Raycast(transform.position, targetDir, distanceToTarget, obstacleMask))
                {
                    Debug.Log("타겟 감지됨 (NonAlloc): " + targetTransform.name);
                }
            }
        }
    }

    #region 디버깅용 기즈모 그리기
    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
            angleInDegrees += transform.eulerAngles.y;

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void OnDrawGizmosSelected()
    {
        // 감지 반경 (원형 범위)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 시야각 선 두 개
        Vector3 leftBoundary = DirFromAngle(-viewAngle / 2, false);
        Vector3 rightBoundary = DirFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

        // 바라보는 정면 방향 (정확한 forward)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewRadius);
    }
    #endregion
}
