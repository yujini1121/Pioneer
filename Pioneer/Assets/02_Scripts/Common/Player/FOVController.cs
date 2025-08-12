using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 250805 : 시야 감지가 필요한 각 스크립트에서 DetectTargets 함수에 감지할 레이어를 매개변수로 전달하여 호출하여 사용하도록 수정

public class FOVController : MonoBehaviour, IBegin
{
    [Header("시야 범위(원)")]
    public float viewRadius = 10f;

    [Header("시야 각")]
    [Range(0, 360)]
    public float viewAngle = 360f;

    [Header("시야 감지 간격")]
    private float detectionInterval = 0.2f;

    [Header("장애물 레이어 설정")]
    private LayerMask obstacleMask;

    public List<Transform> visibleTargets = new List<Transform>();

    public virtual void Start()
    {
        obstacleMask = LayerMask.GetMask("Obstacle"); // 레이어 이름 수정 필요
    }

    /// <summary>
    /// 1.원형 범위 안에서 대상 찾기
    /// 2. 시야각 내에 있는지 확인
    /// 3. 장애물 있는지 레이캐스트 검사
    /// </summary>
    /// <param name="targetLayer">탐지할 오브젝트의 레이어</param>
    public void DetectTargets(LayerMask targetLayer)
    {
        visibleTargets.Clear();
        Collider[] targetsInRange = Physics.OverlapSphere(transform.position, viewRadius, targetLayer);

        for (int i = 0; i < targetsInRange.Length; i++)
        {
            Transform target = targetsInRange[i].transform;
            Vector3 dirTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirTarget, distanceToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
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
