using System.Collections;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

/* PlayerStats (CreatureBase 상속) : 체력, 공격력 같은 핵심 스탯 및 TakeDamage 같은 기능 관리
 * [있어야 할 변수]
int hp = 100;					// 체력
int fullness = 100;				// 포만감
int mental = 100;					// 정신력
int attackDamage = 2; 				// 공격력
float beforeAttackDelay = 0.6f;		// 공격 전 지연 시간 
float AttackCooldown = 0.4f;			// 공격 후 지연 시간
float totalAttackTime = 1.0f;			// 총 공격 시간
int attackPerSecond = 1;			// 초당 공격 가능 횟수
float attackRange = 0.4f;			// 공격 거리
============================================================================================
- 이동
- 공격
- 포만감
- 정신력
- 체력 깎이는 함수 + 체력 올라가는 함수
 */

public class PlayerCore : CreatureBase, IBegin
{
    // 생체 시스템 변수
    int fullness;       // 포만감 변수    
    int maxFullness = 100;
    float fullnessTimer = 0f;
    float fullnessDecrease = 5f;

    int mental;         // 정신력 변수
    int maxMental = 100;

    [Header("공격 설정")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private float attackDuration = 0.2f; // 공격 판정 유지 시간
    [SerializeField] private float attackHeight = 1.0f;

    private Rigidbody playerRb;
    private bool isAttacking = false;

    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        SetSetAttribute();
    }

    void Update()
    {
        
    }

    // =============================================================
    // 스테이터스 기초 값 세팅
    // =============================================================
    void SetSetAttribute()
    {
        maxHp = 100;
        hp = maxHp;                   // 체력
        speed = 4.0f;               // 이동 속도
        fullness = maxFullness;     // 포만감
        mental = maxMental;         // 정신력
        attackDamage = 2;           // 공격력
        attackDelayTime = 0.4f;     // 공격 쿨타임
        attackRange = 0.4f;         // 공격 범위
    }

    // =============================================================
    // 이동
    // =============================================================
    public void Move(Vector3 moveInput)
    {
        Vector3 moveVelocity = moveInput.normalized * speed;

        playerRb.velocity = new Vector3(moveVelocity.x, playerRb.velocity.y, moveVelocity.z);
    }

    // =============================================================
    // 공격
    // =============================================================
    public void Attack()
    {
        if (isAttacking) return;
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 dir = (hit.point - transform.position).normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);

            Vector3 position = transform.position + dir * 0.5f;
            position.y = attackHeight;
            playerAttack.transform.position = position;
            playerAttack.transform.rotation = Quaternion.LookRotation(dir);

            playerAttack.gameObject.SetActive(true);
            playerAttack.damage = this.attackDamage;
        }

        yield return new WaitForSeconds(attackDuration);

        playerAttack.gameObject.SetActive(false);

        isAttacking = false;
    }

    // =============================================================
    // 포만감
    // =============================================================


    // =============================================================
    // 정신력
    // =============================================================
}