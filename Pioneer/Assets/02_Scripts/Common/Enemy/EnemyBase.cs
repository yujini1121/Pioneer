using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyBase : CreatureBase, IBegin
{
    [Header("기본 속성")]
    protected GameObject targetObject;
    protected float detectionRange;
    protected LayerMask detectMask;

    // 돛대 타겟으로 설정
    protected void SetMastTarget()
    {
        targetObject = GameObject.FindGameObjectWithTag("Engine");
    }
}