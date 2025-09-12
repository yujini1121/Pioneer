using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 최상위 부모 스크립트
public class CommonBase : MonoBehaviour, IBegin
{
    public int hp;
    public int maxHp;
    public bool IsDead = false;
    public GameObject attacker = null;

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
    public virtual void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;

        hp -= damage;
        Debug.Log(gameObject.name + "가 " + damage + "의 데미지를 입었습니다! 현재 체력: " + hp);

        this.attacker = attacker;

        if (hp <= 0)
        {
            IsDead = true;
            WhenDestroy();
        }
    }

    // 사라졌을때 호출하는 변수 (생명체인 경우 사망했을 때)
    public virtual void WhenDestroy()
    {
        if (GameManager.Instance == null) return; // 얼리 리턴

        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GameManager.Instance.TriggerGameOver();
            return;
        }
        else if (gameObject.layer == LayerMask.NameToLayer("Mariner"))
        {
            GameManager.Instance.MarinerDiedCount();
        }
        Destroy(gameObject);
    }
}
