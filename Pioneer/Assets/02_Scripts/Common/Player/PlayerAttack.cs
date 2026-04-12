using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour, IBegin
{
    public int damage;
    public Collider attackCollider;
    public LayerMask enemyLayer;

    [Header("애니메이션 설정")]
    [SerializeField] PlayerController playerController;
    AnimationSlot slots;
    readonly HashSet<CreatureBase> hitTargets = new HashSet<CreatureBase>();

    private void Awake()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }

        slots = PlayerCore.Instance.slots;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider other)
    {
        if (!IsEnemyTarget(other))
            return;

        CreatureBase target = other.GetComponentInParent<CreatureBase>();
        if (target == null)
        {
            target = other.GetComponent<CreatureBase>();
        }

        if (target == null || hitTargets.Contains(target))
            return;

        hitTargets.Add(target);
        target.TakeDamage(damage, gameObject);
        Debug.LogError($"damage : {damage}, this.gameObject : {gameObject}");

        ChangeAnim(playerController.lastMoveDirection);
        InventoryManager.Instance.ApplyItemDuablilityUsed();
        PlayerStatsLevel.Instance.AddExp(GrowStatType.Combat, damage);
        Debug.Log("AddExp() 호출");
    }

    private bool IsEnemyTarget(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Enemy"))
            return true;

        return ((1 << other.gameObject.layer) & enemyLayer.value) != 0;
    }

    public void PlayAttack(Vector3 dir)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        ChangeAnim(dir.normalized);
    }

    public void EnableAttackCollider()
    {
        if (attackCollider != null)
        {
            Debug.Log(">> PlayerAttack.EnableAttackCollider() 호출");
            hitTargets.Clear();
            attackCollider.enabled = true;
        }
    }

    public void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            hitTargets.Clear();
        }
    }

    public void SetAttackRange(float range)
    {
        Vector3 v = transform.localScale;
        v.z = range;
        transform.localScale = v;
    }

    void ChangeAnim(Vector3 dir)
    {
        int idx = PlayerCore.Get4DirIndex(dir);

        if (idx < 0) return;
        ChangeAttackByIndex(idx);
    }

    void ChangeAttackByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.attack[idx];

        playerController.ChangeAnimationClip(slots.curAttackClip, target);
        playerController.animator.Play("Attack");
    }
}