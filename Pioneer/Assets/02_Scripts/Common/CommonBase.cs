using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 최상위 부모 스크립트
public class CommonBase : MonoBehaviour, IBegin
{
    int hp;
    int maxHp;

    public virtual void Init()
    {
        
    }

    void Update()
    {
        
    }

    // 데미지 받는 함수
    public virtual void TakeDamage(int damage)
    {

    }

    // 사라졌을때 호출하는 변수 (생명체인 경우 사망했을 때)
    public virtual void WhenDestroy()
    {

    }
}
