using UnityEngine;

public class PlayerAttack : MonoBehaviour, IBegin
{
    public int damage;
    public Collider attackCollider;

    private void Awake()
    {
        // 게임 시작 시 확실하게 비활성화
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<CreatureBase>()?.TakeDamage(damage, this.gameObject);
            // 경험치 제공
            PlayerStatsLevel.Instance.AddExp(GrowStatType.Combat, damage);
            UnityEngine.Debug.Log($"AddExp() 호출");
        }
    }

    public void EnableAttackCollider()
    {
        if (attackCollider != null)
        {
            UnityEngine.Debug.Log($">> PlayerAttack.EnableAttackCollider() 호출");

            attackCollider.enabled = true;
        }
    }

    public void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    public void SetAttackRange(float range)
    {
        Vector3 v = transform.localScale;
        v.z = range;
        transform.localScale = v;
    }
}
