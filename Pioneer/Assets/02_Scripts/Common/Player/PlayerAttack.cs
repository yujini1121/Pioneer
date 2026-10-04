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

        if (playerController == null) playerController = GetComponentInParent<PlayerController>();
        slots = playerController != null ? playerController.animSlots : null;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }

    private void FixedUpdate()
    {
        if (attackCollider != null && attackCollider.enabled) CheckActiveHitbox();
    }

    private void CheckActiveHitbox()
    {
        if (!(attackCollider is BoxCollider box)) return;
        Vector3 scale = box.transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(box.size,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
        foreach (Collider hit in Physics.OverlapBox(box.transform.TransformPoint(box.center),
            halfExtents, box.transform.rotation, enemyLayer, QueryTriggerInteraction.Ignore))
            TryDealDamage(hit);
    }

    private void TryDealDamage(Collider other)
    {
        if (attackCollider == null || !attackCollider.enabled) return;
        if (!IsEnemyTarget(other))
            return;

        CreatureBase target = other.GetComponentInParent<CreatureBase>();
        if (target == null)
        {
            target = other.GetComponent<CreatureBase>();
        }

        if (target == null || target.IsDead || hitTargets.Contains(target))
            return;

        hitTargets.Add(target);
        target.TakeDamage(damage, playerController != null ? playerController.gameObject : gameObject);
        if (damage > 0) AudioManager.instance?.PlaySfx(AudioManager.SFX.Hit);
        UtilityFunctions.Log($"damage : {damage}, this.gameObject : {gameObject}");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ApplyItemDuablilityUsed();
        }

        if (PlayerStatsLevel.Instance != null)
        {
            PlayerStatsLevel.Instance.AddExp(GrowStatType.Combat, damage);
        }

        UtilityFunctions.Log("AddExp() 호출");
    }

    private bool IsEnemyTarget(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Enemy"))
            return true;

        return ((1 << other.gameObject.layer) & enemyLayer.value) != 0;
    }

    public bool HasEnemyInDirection(Vector3 dir, float range)
    {
        if (playerController == null)
            return false;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return false;

        Vector3 origin = playerController.transform.position;
        
        if (!(attackCollider is BoxCollider box)) return false;
        range = Mathf.Max(range, 0.5f);
        Vector3 scale = transform.lossyScale;
        Vector3 halfExtents = new Vector3(Mathf.Abs(box.size.x * scale.x),
            Mathf.Abs(box.size.y * scale.y), range) * 0.5f;
        Vector3 center = origin + dir.normalized * (range * 0.5f);
        center.y = PlayerCore.Instance != null ? PlayerCore.Instance.AttackHeight : origin.y;
        Collider[] hits = Physics.OverlapBox(center, halfExtents,
            Quaternion.LookRotation(dir), enemyLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            CreatureBase target = hit.GetComponentInParent<CreatureBase>();
            if (target == null)
                target = hit.GetComponent<CreatureBase>();

            if (target != null && !target.IsDead)
                return true;
        }

        return false;
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
            UtilityFunctions.Log(">> PlayerAttack.EnableAttackCollider() 호출");
            hitTargets.Clear();
            attackCollider.enabled = true;
            Physics.SyncTransforms();
            CheckActiveHitbox();
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
        v.z = range / Mathf.Max(0.001f, transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.z) : 1f);
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
        if (idx < 0 || slots == null || playerController == null || playerController.animator == null) return;

        AnimationClip baseClip;
        string stateName;
        List<AnimationClip> attackClips = GetAttackClips(out baseClip, out stateName);
        if (attackClips == null || idx >= attackClips.Count || attackClips[idx] == null)
        {
            attackClips = slots.attack;
            baseClip = slots.curAttackClip;
            stateName = "Attack";
        }

        if (attackClips == null || idx >= attackClips.Count || attackClips[idx] == null)
            return;

        var target = attackClips[idx];

        playerController.ChangeAnimationClip(baseClip, target);
        playerController.animator.Play(stateName);
    }

    List<AnimationClip> GetAttackClips(out AnimationClip baseClip, out string stateName)
    {
        baseClip = slots.curAttackClip;
        stateName = "Attack";

        SItemStack selected = InventoryManager.Instance != null ? InventoryManager.Instance.SelectedSlotInventory : null;
        SItemWeaponTypeSO weapon = selected != null ? selected.itemBaseType as SItemWeaponTypeSO : null;
        if (weapon == null || selected.duability <= 0)
            return slots.attack;

        switch (weapon.id)
        {
            case 20001:
                baseClip = slots.curWoodenSwordClip;
                stateName = "WoodenSword";
                return slots.woodenSword;

            case 20002:
                baseClip = slots.curIronSwrordClip;
                stateName = "IronSword";
                return slots.ironSword;

            case 20003:
                baseClip = slots.curConchSwordClip;
                stateName = "ConchSword";
                return slots.conchSword;

            default:
                return slots.attack;
        }
    }
}
