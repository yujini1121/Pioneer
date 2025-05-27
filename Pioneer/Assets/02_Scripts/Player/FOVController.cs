using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FOVController : MonoBehaviour
{
    [Header("시야 범위 (원)")]
    [SerializeField] private float viewRadius = 10f;

    [Header("시야 각")]
    [SerializeField] private float viewAngle = 60f;

    [Header("시야 감지 간격")]
    [SerializeField] private float detectionInterval = 0.2f;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;

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

    }
}
