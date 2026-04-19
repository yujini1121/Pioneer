using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureBase : CommonBase
{
    public FOVController fov;   // 시야 컨트롤러 = 타겟 탐지용

    public float speed;
    public int attackDamage; // default value
    public float attackRange;
    public float attackDelayTime;

    public void Start()
    {
        Debug.Log($">> 게임오브젝트 {gameObject.name}의 CreatureBase.Start 호출됨");

        fov = GetComponent<FOVController>();
    }
}
