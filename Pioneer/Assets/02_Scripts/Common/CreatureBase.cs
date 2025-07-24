using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureBase : CommonBase
{
    public FOVController fov;   // 시야 범위 = 감지 범위

    public float speed;
    public int attackDamage;
    public float attackRange;
    public float attackDelayTime;

    void Init()
    {

    }
}
