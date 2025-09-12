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

    public virtual void Start()
    {
        fov = GetComponent<FOVController>();
    }
}
