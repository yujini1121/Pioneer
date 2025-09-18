using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureBase : CommonBase
{
    public FOVController fov;   // �þ� ���� = ���� ����

    public float speed;
    public int attackDamage; // default value
    public float attackRange;
    public float attackDelayTime;

    public void Start()
    {
        Debug.Log($">> 게임오브젝트{gameObject.name}의 CreatureBase.Start 호출됨");

        fov = GetComponent<FOVController>();
    }
}
