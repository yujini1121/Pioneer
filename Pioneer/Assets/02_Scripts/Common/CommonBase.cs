using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 최상위 부모 스크립트
public class CommonBase : MonoBehaviour, IBegin
{
    protected int hp;
    protected int maxHp;
    public bool IsDead = false;

    public int CurrentHp => hp;

    /*public virtual void Start()
    {
        hp = maxHp;
    }*/

    // ===============
    void Start()
    {
        hp = maxHp;
    }
    // ===============

    void Update()
    {
        
    }

    // 데미지 받는 함수
    public virtual void TakeDamage(int damage)
    {
        if (IsDead) return;

        hp -= damage;
        Debug.Log($"데미지 {damage} 받음. 현재 HP: {hp}");

        if (hp <= 0)
        {
            IsDead = true;
            WhenDestroy();
        }
    }

    // 사라졌을때 호출하는 변수 (생명체인 경우 사망했을 때)
    public virtual void WhenDestroy()
    {
        Debug.Log($"{gameObject.name} 오브젝트 파괴");
        Destroy(gameObject);
    }
}
