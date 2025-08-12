using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyBase : CreatureBase, IBegin
{
    [Header("기본 속성")]
    protected float idleTime;
    protected GameObject targetObject;
    protected float detectionRange;
    protected LayerMask detectMask;

    [SerializeField] private Vector3 attackBoxCenterOffset;

    /// <summary>
    /// 속성 변수에 값 할당
    /// </summary>
    protected virtual void SetAttribute()
    {

    }

    /// <summary>
    /// 돛대 타겟으로 설정
    /// </summary>
    protected void SetMastTarget()
    {
        targetObject = GameObject.FindGameObjectWithTag("Engine");
    }

    /// <summary>
    /// 공격 범위 내 모든 콜라이더를 찾아 배열로 반환
    /// </summary>
    protected Collider[] DetectAttackRange(float attackRange)
    {
        Vector3 boxCenter = transform.position + transform.forward * attackBoxCenterOffset.z + transform.up * attackBoxCenterOffset.y;
        Vector3 halfBoxSize = new Vector3(1f, 1f, attackRange / 2);
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            9
        
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfBoxSize, transform.rotation, detectMask);

        return hitColliders;
    }
}