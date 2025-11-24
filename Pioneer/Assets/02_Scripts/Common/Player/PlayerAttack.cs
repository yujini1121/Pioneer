using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerAttack : MonoBehaviour, IBegin
{
    public int damage;
    public Collider attackCollider;
    public LayerMask enemyLayer;

    [Header("애니메이션 설정")]
    [SerializeField] PlayerController playerController;
    AnimationSlot slots;

    private void Awake()
    {
        // 게임 시작 시 확실하게 비활성화
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }

        slots = PlayerCore.Instance.slots;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.gameObject.layer == enemyLayer)
        {
            other.GetComponent<CreatureBase>()?.TakeDamage(damage, this.gameObject);
            Debug.LogError($"damage : {damage}, this.gameObject : {this.gameObject}");

            // 애니메이션 호출
            ChangeAnim(playerController.lastMoveDirection);


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

    void ChangeAnim(Vector3 dir)
    {
        Debug.LogError($"dir : {dir}");
        int idx = PlayerCore.Get4DirIndex(dir);
        
        if (idx < 0) return;
        ChangeAttackByIndex(idx);
    }

    void ChangeAttackByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.attack[idx];
        Debug.LogError("왜안돼용");

        playerController.ChangeAnimationClip(slots.curAttackClip, target);
        playerController.animator.Play("Attack");
        playerController.nextAnimTrigger = "SetAttack";
    }
}
