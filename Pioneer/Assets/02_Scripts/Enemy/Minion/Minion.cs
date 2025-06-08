using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minion : EnemyBase
{
    protected override void SetAttribute()
    {
        hp = 20;
        attackPower = 1;
        speed = 2.0f;
        detectionRange = 4;
        attackRange = 2;
        attackVisualTime = 2.0f;
        restTime = 2.0f;
    }
}
