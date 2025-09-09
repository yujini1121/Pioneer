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
=============================================================================================
25.09.07 남은 일
    - 음식 섭취 했을 때 어떻게 구현할 것인지
    - 정신력 구현
    - 스테이터스 레벨 구현
25.09.09
    - 플레이어 배 바닥 밖으로 못 나가게 해놔야함 
    - 정신력 구현
    - 스테이터스 레벨 구현
    - 포만감 및 정신력 최소, 최대 제한 걸어두기
 */

public class PlayerCore : CreatureBase, IBegin
{
    // 생체 시스템 변수

        // 포만감 열거형 (fullness 변수 값에 따른 상태)
    public enum FullnessState
    {
        Full,       // 배부름 (80 ~ 100)
        Normal,     // 보통 (30 ~ 79)
        Hungry,     // 배고픔 (1 ~ 29)
        Starving    // 굶주림 (0)
    }
    
        // 포만감 변수  
    int currentfullness;         
    int maxFullness = 100;
    int minFullness = 0;
    FullnessState currentFullnessState;
    int fullnessStarvingMax = 100;
    private Coroutine starvationCoroutine;

    [Header("포만감 설정")]
    [SerializeField] private float fullnessDecreaseTime = 5f;    // 포만감 기본 감소 속도(시간)
    [SerializeField] private float fullnessModifier = 1.3f;      // 포만감 감소 속도 증가값 => 30%

        // 정신력 변수
    int currentmental;         
    int maxMental = 100;
    int minMental = 0;
    bool isDrunk = false;
    private Coroutine enemyExistCoroutine;

    [Header("정신력 설정")]
    [SerializeField] private float existEnemyMenetalCool = 2f;

    [Header("공격 설정")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private float attackHeight = 1.0f;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody playerRb;
    private bool isAttacking = false;

    private float defaultSpeed;

    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        SetSetAttribute();
    }
    
    void Start()
    {
        StartCoroutine(FullnessSystemCoroutine());
    }

    void Update()
    {
        fov.DetectTargets(enemyLayer);
    }

    // =============================================================
    // 스테이터스 기초 값 세팅
    // =============================================================
    void SetSetAttribute()
    {
        maxHp = 100;
        hp = maxHp;                 // 체력
        speed = 4.0f;               // 이동 속도
        defaultSpeed = speed;
        currentfullness = 80;              // 포만감 (시작 값 80)
        currentmental = maxMental;         // 정신력 (시작 값 100)
        attackDamage = 2;           // 공격력
        attackDelayTime = 0.4f;     // 공격 쿨타임
        //attackRange = 0.4f;       // 공격 범위 (이미 attack box 크기를 0.4로 지정해둠)
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

            // TODO: 공격 애니메이션 시작 시간 추가해야 함!!!!!!!!!!! (0.6초)
            playerAttack.gameObject.SetActive(true);
            playerAttack.damage = this.attackDamage;
        }

        // 공격 애니메이션 이후 지연 시간 (0.4초)
        yield return new WaitForSeconds(attackDelayTime);

        playerAttack.gameObject.SetActive(false);

        isAttacking = false;
    }

    /* =============================================================
       { 포만감 }
    - 시작시 80으로 설정, 최대 100 최소 0
    - 현실 시간 5초에 한 번씩 1씩 감소
    - 플레이어 체력이 50% 미만이면 감소 속도 30% 증가 
        - 100 ~ 80 배부름 상태 : 속도 20% 증가
        - 79 ~ 30 배부름 상태 해제
        - 29 ~ 1 배고픔 상태 : 속도 30% 감소
        - 0 굶주림 상태 : 체력이 초 당 1씩 감소 (최대 100초)
    - 음식 종류에 따라 최소 5 ~ 80까지 증가 가능
        - 음식 종류가 무엇인지 알아야 할 듯?
    ====================================
    25.09.07 : 포만감 굶주림 코루틴 수정
    ============================================================= */

    // 초당 포만감 1씩 감소 Start 함수에서 시작 (코루틴)
    private IEnumerator FullnessSystemCoroutine()
    {
        while(true)
        {
            float currentDecreaseTime = fullnessDecreaseTime;
            if (hp < maxHp * 0.5f)
            {
                currentDecreaseTime = fullnessDecreaseTime / fullnessModifier;
            }

            yield return new WaitForSeconds(currentDecreaseTime);

            if(currentfullness > 0)
            {
                currentfullness--;
                UpdateFullnessState();
            }
            Debug.Log($"굶주림 수치 : {currentfullness}");
        }
    }

    /// <summary>
    /// 포만감 수치에 따라 상태 갱신 함수
    /// </summary>
    private void UpdateFullnessState()
    {
        FullnessState fullnessState;

        if (currentfullness >= 80)
            fullnessState = FullnessState.Full;
        else if (currentfullness >= 30)
            fullnessState = FullnessState.Normal;
        else if (currentfullness >= 1)
            fullnessState = FullnessState.Hungry;
        else
            fullnessState = FullnessState.Starving;

        if(fullnessState != currentFullnessState)
        {
            currentFullnessState = fullnessState;

            switch(currentFullnessState)
            {
                case FullnessState.Full:
                    speed = defaultSpeed * 1.2f;
                    break;
                case FullnessState.Hungry:
                    speed = defaultSpeed * 0.7f;
                    break;
                default:
                    speed = defaultSpeed;
                    break;
            }

            if(currentFullnessState == FullnessState.Starving)      // 굶주림 상태일때
            {
                if(starvationCoroutine == null)
                    starvationCoroutine = StartCoroutine(StarvingDamageCorountine());
            }
            else                                                    // 굶주림 상태가 아닐때
            {
                if (starvationCoroutine != null)
                {
                    StopCoroutine(starvationCoroutine);
                    starvationCoroutine = null;
                }
            }
        }
    }

    // 초당 체력 1씩 감소하는 굶주림 함수 (코루틴)
    private IEnumerator StarvingDamageCorountine()
    {        
        Debug.Log("굶주림 상태 : 체력 감소 시작");
        for(int i = 0; i < fullnessStarvingMax; i++)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(1, this.gameObject);
        }        
    }

    // public 형의 음식 섭취시 호출할 수 있는 함수 추가
    public void EatFood(int increase)
    {
        currentfullness += increase;
    }

    /* =============================================================
        { 정신력 }
    - 시작시 100으로 시작, 0 ~ 100 사이의 값을 가짐
    - 정신력 40 ~ 100 : 효과 없음
    - 정신력 0 ~ 39 : 공격력, 설치 작업 대성공 확률, 죄책감 시스템 레벨 감소

    [증가 조건]
    - 아이템 사용에 따라 5 ~ 80까지 증가 가능
    - 음식 섭취 시 10씩 증가 (종류 상관 없음)

    [감소 조건]
    - 플레이어 반경 2M 내 에너미가 존재할 경우 2초당 1씩 감소
    - 에너미에게 공격 받은 경우 공격 1회당 3씩 감소 (반경 내 에너미 존재 조건과 중첩 가능)
    - 승무원 AI 사망시 현재 정신력의 20% 감소

    [동결 조건]
    - 아이템 중 술을 마시면 만취 상태가 됨
    - 만취 상태 : 정신력 증가 및 감소 불가, 동결됨
    ============================================================= */

    void UpdateMental(int increase)
    {
        if(isDrunk)
            return;

        currentmental += increase;
        currentmental = Mathf.Clamp(currentmental, minMental, maxMental);


    }

    // 정신력 올리는 아이템 사용시 호출
    public void UseMentalItem(int increase)
    {

    }

    /// <summary>
    /// 반경 2m 내에 에너미가 존재 여부를 확인하고 정신력 감소 코루틴 실행 및 중단 함수
    /// </summary>
    public void NearEnemy()
    {
        if (fov.visibleTargets.Count > 0 && enemyExistCoroutine == null)
        {
            enemyExistCoroutine = StartCoroutine(EnemyExist());
        }
        else if(fov.visibleTargets.Count == 0 && enemyExistCoroutine != null)
        {
            StopCoroutine(EnemyExist());
            enemyExistCoroutine = null;
        }
    }

    /// <summary>
    /// 에너미 존재시 2초에 한 번 정신력 감소
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnemyExist()
    {
        while(true)
        {
            yield return new WaitForSeconds(existEnemyMenetalCool);
            currentmental--;
        }        
    }
}