using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Playables;
using static MarinerBase;

#region 그냥 메모
/* =============================================================
 * PlayerStats (CreatureBase 상속) : 체력, 공격력 같은 핵심 스탯 및 TakeDamage 같은 기능 관리
 [있어야 할 변수]
int hp = 100;					// 체력
int fullness = 100;				// 포만감
int mental = 100;					// 정신력
int attackDamage = 2; 				// 공격력
float beforeAttackDelay = 0.6f;		// 공격 전 지연 시간 
float AttackCooldown = 0.4f;			// 공격 후 지연 시간
float totalAttackTime = 1.0f;			// 총 공격 시간
int attackPerSecond = 1;			// 초당 공격 가능 횟수
float attackRange = 0.4f;			// 공격 거리
===============================================================
25.09.07 남은 일
    - 음식 섭취 했을 때 어떻게 구현할 것인지
    - 정신력 구현
    - 스테이터스 레벨 구현
25.09.09
    - 플레이어 배 바닥 밖으로 못 나가게 해놔야함 
    - 정신력 구현
    - 스테이터스 레벨 구현
    - 포만감 및 정신력 최소, 최대 제한 걸어두기
 ============================================================= */
#endregion

// TODO : 죄책감 시스템.cs : 멘탈 디버프 있을때 죄책감 레벨 + 1 / CommonUi.cs : 대성공 확률 -40%;
public class PlayerCore : CreatureBase, IBegin
{
    public static PlayerCore Instance;

        // 플레이어 행동 상태 열거형
    public enum PlayerState
    {
        Default,            // 기본
        ChargingFishing,    // 낚시 키 누르는 중
        ActionFishing,      // 낚시 중
        Dead                // 사망
    }    

    // { 생체 시스템 변수 } //
    // 포만감 열거형 (fullness 변수 값에 따른 상태)
    public enum FullnessState
    {
        Full,       // 배부름 (80 ~ 100)
        Normal,     // 보통 (30 ~ 79)
        Hungry,     // 배고픔 (1 ~ 29)
        Starving    // 굶주림 (0)
    }

        // [ 공격력 변수 ]
    public float AttackDamageCalculated
    {
        get
        {
            if (IsMentalDebuff())
            {
                return (attackDamage * 5) / 10;
            }
            else
            {
                return attackDamage;
            }
        }
    }
        // [ 포만감 변수 ]  
    public int currentFullness;                                            // 현재 포만감 값
    public int maxFullness = 100;                                          // 최대 포만감 값
    int minFullness = 0;                                            // 최소 포만감 값
    FullnessState currentFullnessState;                             // 현재 포만감 상태
    int fullnessStarvingMax = 100;                                  // 굶기 상태시 체력 깎이는 최대 횟수 (100회)
    private Coroutine starvationCoroutine;                          // 굶기 상태시 실행되는 코루틴

    [Header("포만감 설정")]
    [SerializeField] private float fullnessDecreaseTime = 5f;       // 포만감 기본 감소 속도(시간)
    [SerializeField] private float fullnessModifier = 1.3f;         // 포만감 감소 속도 증가값 => 30%

        // [ 정신력 변수 ]
    public int currentMental;                                              // 현재 정신력 값
    public int maxMental = 100;                                            // 최대 정신력 값
    int minMental = 0;                                              // 최소 정신력 값
    bool isDrunk = false;                                           // 만취 상태 여부
    private Coroutine enemyExistCoroutine;                          // 일정 범위 안 에너미 존재시 실행되는 코루틴 
    bool isApplyDebuff = false;

    [Header("정신력 설정")]
    [SerializeField] private float existEnemyMentalCool = 2f;        // 일정 범위 안 에너미 존재시 정신력이 깎이는 시간 텀
    [SerializeField] private int existEnemyMentalDecrease = -1;      // 일정 범위 안 에너미 존재시 깎이는 정신력 값 
    [SerializeField] private int attackedFromEnemy = -3;             // 에너미한테 공격 당했을 경우 깎이는 정신력 값
    [SerializeField] private float reduceMentalOnMarinerDie = 0.2f; // 승무원 사망시 깎이는 정신력 값
    [SerializeField] private int eatFoodincreaseMental = 10;

    // 공격 관련 설정 변수
    [Header("공격 설정")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private float attackHeight = 1.0f;
    [SerializeField] private LayerMask enemyLayer;
    private PlayerController playerController;
    private bool isattacked = false;

    public PlayerAttack PlayerAttack => playerAttack;
    public float AttackHeight => attackHeight;
    public LayerMask EnemyLayer => enemyLayer;

    public SItemWeaponTypeSO handAttackStartDefault;
    public SItemStack dummyHandAttackItem;

    [SerializeField] private SItemWeaponTypeSO handAttackCurrentValue;

    // 기본 시스템 관련 번수
    private Rigidbody playerRb;
    private bool isAttacking = false;
    private float defaultSpeed;

    public static event Action<int> PlayerHpChanged;
    public static event Action<int> PlayerFullnessChanged;
    public static event Action<int> PlayerMentalChanged;

    public PlayerState currentState { get; private set; }

    // 코루틴 변수
    private bool isRunningCoroutineItem = false;
    public bool IsRunningCoroutineItem => isRunningCoroutineItem;

    void Awake()
    {
        Instance = this;
        playerController = GetComponent<PlayerController>();
        playerRb = GetComponent<Rigidbody>();
        SetSetAttribute();

        handAttackCurrentValue.DeepCopyFrom(handAttackStartDefault);
        dummyHandAttackItem = new SItemStack(-1, -1);
    }
    
    new void Start()
    {
        base.Start();

        UpdateFullnessState();
        StartCoroutine(FullnessSystemCoroutine());                   // 게임 시작시 포만감 계속 1씩 감소 시작
    }

    void Update()
    {
        UnityEngine.Debug.Assert(fov != null);
        UnityEngine.Debug.Assert(enemyLayer != null);


        fov.DetectTargets(enemyLayer);
        NearEnemy();
        // UnityEngine.Debug.Log($"정신력 수치 : {currentMental}");
    }
    public override void WhenDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    #region 기본 시스템
    // =============================================================
    // 스테이터스 기초 값 세팅
    // =============================================================
    void SetSetAttribute()
    {
        maxHp = 100;
        hp = maxHp;                 // 체력
        speed = 4.0f;               // 이동 속도
        defaultSpeed = speed;
        currentFullness = 80;              // 포만감 (시작 값 80)
        currentMental = maxMental;         // 정신력 (시작 값 100)
        attackDamage = 2;           // 공격력
        attackDelayTime = 0.4f;     // 공격 쿨타임
        attackRange = 0.4f;       // 공격 범위 (이미 attack box 크기를 0.4로 지정해둠)
    }

    public void SetState(PlayerState state)
    {
        currentState = state;
        UnityEngine.Debug.Log("Player State Changed to: " + state);
    }

    // =============================================================
    // 이동
    // =============================================================
    public void Move(Vector3 moveInput)
    {
        if (currentState != PlayerState.Default) return;

        Vector3 moveVelocity = moveInput.normalized * speed;

        playerRb.velocity = new Vector3(moveVelocity.x, playerRb.velocity.y, moveVelocity.z);
    }

    // =============================================================
    // 공격
    // =============================================================
    public void Attack(SItemWeaponTypeSO weapon)
    {
        if (currentState != PlayerState.Default) return;
        if (isAttacking) return;
        StartCoroutine(AttackCoroutine(weapon, InventoryManager.Instance.SelectedSlotInventory));
    }

    public bool IsMentalDebuff()
    {
        return currentMental < 40.0f; 
    }

    public bool BeginCoroutine(IEnumerator coroutine)
    {
        if (isRunningCoroutineItem) return false;
        StartCoroutine(CoroutineWraper(coroutine));
        return true;
    }

    private IEnumerator CoroutineWraper(IEnumerator coroutine)
    {
        isRunningCoroutineItem = true;
        yield return coroutine;
        isRunningCoroutineItem = false;
    }

    private IEnumerator AttackCoroutine(SItemWeaponTypeSO weapon, SItemStack itemWithState)
    {
        isAttacking = true;

        yield return new WaitForSeconds(weapon.weaponAnimation);

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
            // playerAttack.gameObject.SetActive(true);
            playerAttack.EnableAttackCollider();
            playerAttack.damage = (int)(weapon.weaponDamage) + this.attackDamage;

            if (itemWithState != null)
            {
                itemWithState.duability -= weapon.duabilityRedutionPerHit;
            }

            InventoryUiMain.instance.IconRefresh();
        }

        // 공격 애니메이션 이후 지연 시간 (0.4초)
        //yield return new WaitForSeconds(attackDelayTime);
        yield return new WaitForSeconds(weapon.weaponDelay);

        // playerAttack.gameObject.SetActive(false);
        playerAttack.DisableAttackCollider();
        isAttacking = false;
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        base.TakeDamage(damage, attacker);
        PlayerHpChanged?.Invoke(hp);
        if(attacker.CompareTag("Enemy"))
            AttackedFromEnemy();

        if (currentState == PlayerState.ChargingFishing || currentState == PlayerState.ActionFishing)
        {
            SetState(PlayerState.Default);

            // ++++ 낚시 ui 바꿔야하는데 음 
            if (playerController != null)
            {
                playerController.CancelFishing();
            }
            UnityEngine.Debug.Log("피격으로 인해 낚시가 취소되었습니다!");
        }
    }
    #endregion

    #region 포만감
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


    /// <summary>
    /// 초당 포만감 1씩 감소 Start 함수에서 시작 (코루틴)
    /// </summary>
    /// <returns></returns>
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

            if(currentFullness > 0)
            {
                currentFullness--;
                currentFullness = Mathf.Clamp(currentFullness, minFullness, maxFullness);
                UpdateFullnessState();

                PlayerFullnessChanged?.Invoke(currentFullness);
            }
            UnityEngine.Debug.Log($"굶주림 수치 : {currentFullness}");
        }
    }

    /// <summary>
    /// 포만감 수치에 따라 상태 갱신 함수
    /// </summary>
    private void UpdateFullnessState()
    {
        FullnessState fullnessState;

        if (currentFullness >= 80)
            fullnessState = FullnessState.Full;
        else if (currentFullness >= 30)
            fullnessState = FullnessState.Normal;
        else if (currentFullness >= 1)
            fullnessState = FullnessState.Hungry;
        else
            fullnessState = FullnessState.Starving;

        switch (fullnessState)
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

        if (fullnessState != currentFullnessState)
        {
            currentFullnessState = fullnessState;            

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

    /// <summary>
    /// 초당 체력 1씩 감소하는 굶주림 함수 (코루틴)
    /// </summary>
    /// <returns></returns>
    private IEnumerator StarvingDamageCorountine()
    {
        UnityEngine.Debug.Log("굶주림 상태 : 체력 감소 시작");
        for(int i = 0; i < fullnessStarvingMax; i++)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(1, this.gameObject);
        }        
    }

    /// <summary>
    /// 음식 섭취시 포만감 증가, 증가값 매개변수로 전달
    /// </summary>
    /// <param name="increase"></param>
    public void EatFoodFullness(int increase)
    {
        currentFullness += increase;
        currentFullness = Mathf.Clamp(currentFullness, minFullness, maxFullness);

        PlayerFullnessChanged?.Invoke(currentFullness);
    }

    // 굶주림 제거 
    public void RemoveStarvingIEnumerator()
    {
        if(starvationCoroutine != null)
        {
            StopCoroutine(starvationCoroutine);
            starvationCoroutine = null;
        }
    }
    #endregion

    #region 정신력


    /* =============================================================
        { 정신력 }
    - 시작시 100으로 시작, 0 ~ 100 사이의 값을 가짐
    - 정신력 40 ~ 100 : 효과 없음
    - 정신력 0 ~ 39 : 공격력, 설치 작업 대성공 확률, 죄책감 시스템 레벨 감소

    [증가 조건]
    - 둘 다 아이템 사용시 증가값만 전달하면 정신력 추가하는 함수를 추가
        - 아이템 사용에 따라 5 ~ 80까지 증가 가능
        - 음식 섭취 시 10씩 증가 (종류 상관 없음)

    [감소 조건]    
        - 플레이어 반경 2M 내 에너미가 존재할 경우 2초당 1씩 감소
        - 에너미에게 공격 받은 경우 공격 1회당 3씩 감소 (반경 내 에너미 존재 조건과 중첩 가능)
        - 승무원 AI 사망시 현재 정신력의 20% 감소

    [동결 조건]
    - 아이템 중 술을 마시면 만취 상태가 됨
    - 만취 상태 : 정신력 증가 및 감소 불가, 동결됨

    TODO : 
    ============================================================= */



    //// 여기 보세요!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //void MentalState() // 호출 시점 -> update()
    //{
    //    currentAttackDamage = attackDamage;
    //    if (currentMental < 40 && isApplyDebuff == false)
    //    {
    //        // 공격력(5%), 설치 작업 대성공 확률(40%)은 감소, 죄책감 시스템 레벨 증가(1Lv) => 정신력이 40이상이 되면 원래 레벨로 돌아감    
    //    }
    //    else
    //    {
    //        RemoveMentalDebuff();
    //    }
    //}

    //// TODO : 정신력 디버프 (0 ~ 39 사이 값일 때)  구현해야함..
    //void ApplyMentalDebuff()
    //{
    //    isApplyDebuff = true;
    //    // [[ 공격력 감소 ]]

    //    float debuffAttackDamage = attackDamage * 0.5f; // 현재 공격력의 5% 계산하기 전에 원래 공격력 저장
    //    attackDamage -= Mathf.RoundToInt(debuffAttackDamage); // 공격력 float형으로 바꿔도 되는지 물어봐야됨.. 지금은 반올림 해서 감소

    //    // [[ 설치 작업 대성공 확률 감소 ]] => 어떻게 할지 생각 좀 해보고.. 지금 확률 관리하는 스크립트가 PlayerCore 스크립트 참조하고 있음

    //    // 죄책감 시스템 레벨 1증가, 
    //}

    //// TODO : 정신력 디버프 해제
    //void RemoveMentalDebuff()
    //{
    //    attackDamage = currentAttackDamage;

    //}
    // 여기 까지!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    /// <summary>
    /// 정신력 계산 ? 메서드 
    /// </summary>
    /// <param name="increase"></param>
    void UpdateMental(int increase)
    {
        if(isDrunk)
            return;

        currentMental += increase;
        currentMental = Mathf.Clamp(currentMental, minMental, maxMental);

        PlayerMentalChanged?.Invoke(currentMental);

        // 수치에 따라 디버프 부여,,
    }

    /// <summary>
    /// 정신력 올리는 아이템 사용시 호출, 
    /// </summary>
    /// <param name="increase"></param>
    public void UseMentalItem(int increase)
    {
        UpdateMental(increase);
    }

    /// <summary>
    /// 음식 섭취시 호출, 음식 종류 상관없이 10씩 증가..
    /// </summary>
    public void EatFoodMental()
    {
        UpdateMental(eatFoodincreaseMental);
    }

    /// <summary>
    /// 에너미에게 공격 받은 경우 정신력 감소 시키는 함수 -3
    /// </summary>
    public void AttackedFromEnemy()
    {
        UpdateMental(attackedFromEnemy);
    }

    /// <summary>
    /// 승무원 죽었을때 호출, 정신력 감소, 현재 정신력의 20%
    /// </summary>
    public void ReduceMentalOnMarinerDie()
    {
        float reduce = currentMental * reduceMentalOnMarinerDie;
        UpdateMental(Mathf.RoundToInt(-reduce)); // 반올림하고 았는데 그냥 . 아래 수 버릴거면 수정 가능
    }

    /// <summary>
    /// 반경 2m 내에 에너미가 존재 여부를 확인하고 정신력 감소 코루틴 실행 및 중단할때 호출 
    /// </summary>
    public void NearEnemy()
    {
        if (fov.visibleTargets.Count > 0 && enemyExistCoroutine == null)
        {
            enemyExistCoroutine = StartCoroutine(EnemyExist());
        }
        else if(fov.visibleTargets.Count == 0 && enemyExistCoroutine != null)
        {
            StopCoroutine(enemyExistCoroutine);
            enemyExistCoroutine = null;
        }
    }

    /// <summary>
    /// 에너미 존재시 2초에 한 번 정신력 감소 -1
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnemyExist()
    {
        while(true)
        {
            yield return new WaitForSeconds(existEnemyMentalCool);
            UpdateMental(existEnemyMentalDecrease);
        }        
    }

    public bool IsDrunk() // 만취상태인지만 리턴하는 메서드 
    {
        return isDrunk;
    }

    // 술 아이템 사용시 호출
    public IEnumerator Drunk()
    {
        isDrunk = true;

        yield return new WaitForSeconds(60f);

        isDrunk = false;
    }
    #endregion

}