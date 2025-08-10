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
}