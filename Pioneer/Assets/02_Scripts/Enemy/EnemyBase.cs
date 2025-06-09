using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("기본 속성")]
    public int hp;
    public int maxHp; // 최대 체력 추가
    public int attackPower;
    public float speed;
    public int detectionRange;
    public int attackRange;
    public float attackVisualTime;
    public float restTime;

    [Header("상태")]
    public GameObject targetObject;
    public EnemyState currentState;

    [Header("사망 설정")]
    public bool destroyOnDeath = true;
    public float deathDelay = 1.0f;
    public GameObject deathEffect; // 사망 시 이펙트

    // 이벤트 시스템
    public System.Action<EnemyBase> OnDeath;
    public System.Action<EnemyBase, int> OnDamageTaken;
    public System.Action<EnemyBase> OnHealthChanged;

    // 상태 플래그
    protected bool isDead = false;
    protected bool isInvulnerable = false;

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
        // 이미 죽었거나 무적 상태면 데미지 무시
        if (isDead || isInvulnerable)
            return;

        // 데미지 적용
        hp -= damage;
        hp = Mathf.Max(0, hp); // 체력이 음수가 되지 않도록

        UnityEngine.Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받았습니다. 남은 체력: {hp}/{maxHp}");

        // 이벤트 호출
        OnDamageTaken?.Invoke(this, damage);
        OnHealthChanged?.Invoke(this);

        // 사망 체크
        if (hp <= 0)
        {
            Die();
        }
        else
        {
            // 데미지 반응 (자식 클래스에서 오버라이드 가능)
            OnDamageReaction(damage, source);
        }
    }

    /// <summary>
    /// 체력을 회복하는 메서드
    /// </summary>
    /// <param name="healAmount">회복할 체력</param>
    public virtual void Heal(int healAmount)
    {
        if (isDead)
            return;

        hp += healAmount;
        hp = Mathf.Min(hp, maxHp); // 최대 체력을 초과하지 않도록

        UnityEngine.Debug.Log($"{gameObject.name}이(가) {healAmount} 체력을 회복했습니다. 현재 체력: {hp}/{maxHp}");

        OnHealthChanged?.Invoke(this);
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
    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentState = EnemyState.Dead;

        UnityEngine.Debug.Log($"{gameObject.name}이(가) 사망했습니다.");

        // 사망 이벤트 호출
        OnDeath?.Invoke(this);

        // 사망 시 특별한 처리 (자식 클래스에서 오버라이드 가능)
        OnDeathBehavior();

        // 사망 이펙트 재생
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // 오브젝트 파괴 또는 비활성화
        if (destroyOnDeath)
        {
            StartCoroutine(DestroyAfterDelay());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 사망 시 특별한 행동 (자식 클래스에서 오버라이드)
    /// </summary>
    protected virtual void OnDeathBehavior()
    {
        // 기본적으로는 아무것도 하지 않음
        // 자식 클래스에서 특별한 사망 효과 구현 가능
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    #endregion

    #region 상태 및 유틸리티 메서드

    /// <summary>
    /// 현재 체력 비율 반환 (0.0 ~ 1.0)
    /// </summary>
    public float GetHealthRatio()
    {
        return maxHp > 0 ? (float)hp / maxHp : 0f;
    }

    /// <summary>
    /// 적이 살아있는지 확인
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && hp > 0;
    }

    /// <summary>
    /// 무적 상태 설정
    /// </summary>
    /// <param name="invulnerable">무적 여부</param>
    /// <param name="duration">무적 지속 시간 (0이면 수동으로 해제할 때까지)</param>
    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;

        if (invulnerable && duration > 0f)
        {
            StartCoroutine(RemoveInvulnerabilityAfterDelay(duration));
        }
    }

    private IEnumerator RemoveInvulnerabilityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isInvulnerable = false;
    }

    /// <summary>
    /// 체력을 최대로 회복
    /// </summary>
    public void FullHeal()
    {
        Heal(maxHp - hp);
    }

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