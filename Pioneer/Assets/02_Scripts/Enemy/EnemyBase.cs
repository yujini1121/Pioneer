using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("기본 속성")]
    public int hp;
    public int maxHp;
    public int attackPower;
    public float speed;
    public int detectionRange;
    public int attackRange;
    public float attackVisualTime;
    public float restTime;
    protected GameObject targetObject;

    private GameObject attacker;

    private bool isDead = false;

    protected virtual void Awake()
    {
        SetAttribute();
        InitializeHealth();
    }

    protected virtual void SetAttribute()
    {
        // 기본값 설정 (자식 클래스에서 오버라이드)
    }

    private void InitializeHealth()
    {
        if (maxHp <= 0)
        {
            maxHp = hp; // maxHp가 설정되지 않았다면 현재 hp로 설정
        }
        hp = maxHp; // 시작할 때 최대 체력으로 설정
    }

    #region 데미지 및 체력 시스템

    /// <summary>
    /// 데미지를 받는 메서드
    /// </summary>
    /// <param name="damage">받을 데미지</param>
    /// <param name="source">데미지를 준 오브젝트 (반격 등에 사용)</param>
    public virtual void TakeDamage(int damage, GameObject source = null)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
        }

        attacker = source;
    }

    /// <summary>
    /// 체력을 회복하는 메서드
    /// </summary>
    /// <param name="healAmount">회복할 체력</param>
    public virtual void Heal(int healAmount)
    {
        hp += healAmount;

        if (hp > maxHp)
        {
            hp = maxHp;
        }
    }

    /// <summary>
    /// 데미지를 받았을 때의 반응 (자식 클래스에서 오버라이드)
    /// </summary>
    /// <param name="damage">받은 데미지</param>
    /// <param name="source">데미지 소스</param>
    protected virtual void OnDamageReaction(int damage, GameObject source)
    {
        // 기본적으로는 아무것도 하지 않음
        // 자식 클래스에서 특별한 반응 구현 가능 (예: 반격, 도망 등)
    }

    #endregion

    #region 사망 처리

    /// <summary>
    /// 사망 처리 메서드
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Destroy(gameObject, 1f);
    }

    #endregion

    #region 상태 및 유틸리티 메서드

    /// <summary>
    /// 강제로 즉사시키는 메서드
    /// </summary>
    public void Kill()
    {
        hp = 0;
        Die();
    }

    #endregion
}