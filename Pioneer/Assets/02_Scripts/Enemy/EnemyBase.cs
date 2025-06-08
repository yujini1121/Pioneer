using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public int hp;
    public int attackPower;
    public float speed;
    public int detectionRange;
    public int attackRange;
    public float attackVisualTime;
    public float restTime;

    public List<Transform> detectList;

    public EnemyState currentState;

    protected virtual void Awake()
    {
        SetAttribute();
    }

    protected virtual void SetAttribute()
    {

    }

    /*public virtual void Detect()
    {

    }

    public virtual void Move()
    {

    }

    public virtual void Attack()
    {

    }*/

    // 单固瘤贸府
    // 荤噶 贸府
}
