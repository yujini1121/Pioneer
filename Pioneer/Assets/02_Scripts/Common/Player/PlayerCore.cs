using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.VirtualTexturing;
using static MarinerBase;

#region 그냥 메모
#endregion

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
    public bool IsAttackDamageDebuff
    {
        get => IsMentalDebuff();
    }

    private float lastEffectTime = -999f;

    [Header("포만감 변수")]
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

    [Header("정신력 변수")]
    //[ 정신력 변수 ]
    public int currentMental;                                              // 현재 정신력 값
    public int CurrentMental => currentMental;
    public int maxMental = 100;                                            // 최대 정신력 값
    int minMental = 0;                                              // 최소 정신력 값
    bool isDrunk = false;                                           // 만취 상태 여부
    [SerializeField, Min(0.01f)] private float drunkDuration = 60f;
    private float drunkUntil;
    private Coroutine drunkCoroutine;
    public float DrunkRemaining => isDrunk ? Mathf.Max(0f, drunkUntil - Time.time) : 0f;
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
    public float duabilityReducePrevent = 0f;
    public int DuabilityReducePrevent => Mathf.RoundToInt(duabilityReducePrevent);
    int currentAttackDamage = 0;

    public CreatureEffect creatureEffect;

    public PlayerAttack PlayerAttack => playerAttack;
    public float AttackHeight => attackHeight;
    public LayerMask EnemyLayer => enemyLayer;

    [Header("애니메이션 설정")]
    public AnimationSlot slots;
    private Animator animator;

    private Vector3 currentDirection;
    private int _curIdleIdx = -1;
    private int _curRunIdx = -1;
    private int _curFishingReadyIdx = -1;
    public int _curFishingHoldIdx = -1;

    [SerializeField] private SItemWeaponTypeSO handAttackStartDefault;
	public SItemWeaponTypeSO handAttackCurrentValueRaw;

    public Transform mast;

    private bool isPlaySFXHunger = false;

    private bool isPlaySFXMental = false;

    private bool isPlaySFXLowHp = false;

    private StunHandler stunHandler;

    public SItemWeaponTypeSO CalculatedHandAttack
    {
        get
        {
            SItemWeaponTypeSO returnValue = ScriptableObject.CreateInstance<SItemWeaponTypeSO>();
            returnValue.DeepCopyFrom(handAttackCurrentValueRaw);

            if (IsMentalDebuff())
            {
#warning [생체 시스템 : 정신력 시스템] 정신력 40미만 공격력 감소량 구체적으로 작성
				returnValue.weaponDamage /= 2; // 정신적으로 미쳐있을때만 영향 줌. 원래대로 복구함. 감소값 수정
			}

            return returnValue;
		}
    }


    public SItemStack dummyHandAttackItem;


    // 기본 시스템 관련 번수
    private Rigidbody playerRb;
    private bool isAttacking = false;
    private float defaultSpeed;
    private float thunderSpeedMultiplier = 1f;

    public static event Action<int> PlayerHpChanged;
    public static event Action<int> PlayerFullnessChanged;
    public static event Action<int> PlayerMentalChanged;

    public PlayerState currentState { get; private set; }

    // 코루틴 변수
    private bool isRunningCoroutineItem = false;
    public bool IsRunningCoroutineItem => isRunningCoroutineItem;

    [Header("디버깅")]
    public bool isDebugging;

    void Awake()
    {
        Instance = this;
        playerController = GetComponent<PlayerController>();
        playerRb = GetComponent<Rigidbody>();
        IgnorePlatformCollisionSeams();
        creatureEffect = GetComponent<CreatureEffect>();
        SetSetAttribute();

        handAttackCurrentValueRaw.DeepCopyFrom(handAttackStartDefault);
        dummyHandAttackItem = new SItemStack(-1, -1);

        // 애니메이션
        slots = playerController.animSlots;
        animator = playerController.animator;
        playerRb = GetComponent<Rigidbody>();

    }

    private static void IgnorePlatformCollisionSeams()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int platformLayer = LayerMask.NameToLayer("Platform");
        if (playerLayer < 0 || platformLayer < 0) return;

        Physics.IgnoreLayerCollision(playerLayer, platformLayer, true);
    }

    new void Start()
    {
        base.Start();
        stunHandler = GetComponent<StunHandler>();

        UpdateFullnessState();
        StartCoroutine(FullnessSystemCoroutine());                   // 게임 시작시 포만감 계속 1씩 감소 시작
    }

    void Update()
    {
        if (IsDead)
            return;

        if (hp <= 0)
        {
            IsDead = true;
            WhenDestroy();
            return;
        }

        if (stunHandler != null && stunHandler.IsStunned)
            return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F12))
        {
            transform.position = mast.position;
        }
#endif

        UtilityFunctions.Assert(fov != null);
        UtilityFunctions.Assert(enemyLayer != null);


        fov.DetectTargets(enemyLayer);
        if(!isDrunk)
        {
        }
        NearEnemy();
    }
    public override void WhenDestroy()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.GameOver);

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
        speed = 1.8f;               // 이동 속도
        defaultSpeed = speed;
        currentFullness = 80;              // 포만감 (시작 값 80)
        currentMental = maxMental;         // 정신력 (시작 값 100)
        attackDamage = 2;           // 공격력
        attackDelayTime = 0.4f;     // 공격 쿨타임
        attackRange = 0.4f;
    }

    public void SetState(PlayerState state)
    {
        currentState = state;
        if (state == PlayerState.ChargingFishing || state == PlayerState.ActionFishing)
            StopHorizontalMovement();
    }

    public static int Get4DirIndex(in Vector3 v)
    {
        if (v.sqrMagnitude < 1e-6f) return -1;
        float ax = Mathf.Abs(v.x);
        float az = Mathf.Abs(v.z);
        if (ax >= az) return (v.x >= 0f) ? 3 : 2;
        else return (v.z <= 0f) ? 0 : 1;
    }

    public static int Get2DirIndex(in Vector3 v)
    {
        if (v.sqrMagnitude < 1e-6f) return -1;   // 정지면 -1
        return (v.x >= 0f) ? 1 : 0;
    }

    void ChangeIdleByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.idle[idx];

        playerController.ChangeAnimationClip(slots.curIdleClip, target);
        playerController.nextAnimTrigger = "SetIdle";
    }

    void ChangeRunByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.run[idx];

        playerController.ChangeAnimationClip(slots.curRunClip, target);
        playerController.nextAnimTrigger = "SetRun";
    }

    void ChangeFishingReadyByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.fising[idx];

        playerController.ChangeAnimationClip(slots.curFishingClip, target);
        playerController.nextAnimTrigger = "SetFishing";
    }

    public void ChangeFishingHoldByIndex(int idx)
    {
        if (idx < 0) return;
        var target = slots.fisingHold[idx];

        playerController.ChangeAnimationClip(slots.curFishingHoldClip, target);
        playerController.nextAnimTrigger = "SetFishingHold";
    }

    // =============================================================
    // 가만히있엇
    // =============================================================
    public void Idle(Vector3 moveInput)
    {
        int idx = Get4DirIndex(moveInput);
        if (isDebugging)
        {
        }

        if (idx != _curRunIdx)
        {
            ChangeIdleByIndex(idx);
            _curIdleIdx = idx;
        }
    }

    // =============================================================
    // 이동
    // =============================================================
    public void Move(Vector3 moveInput)
    {
        if (currentState != PlayerState.Default || IsKnockbackActive) return;

        int idx = Get4DirIndex(moveInput);
        if (idx != _curRunIdx)
        {
            ChangeRunByIndex(idx);
            _curRunIdx = idx;
        }

        var v = moveInput.normalized * speed;
        playerRb.velocity = new Vector3(v.x, playerRb.velocity.y, v.z);
    }

    public void StopHorizontalMovement()
    {
        if (playerRb == null) return;

        playerRb.velocity = new Vector3(0f, playerRb.velocity.y, 0f);
    }

    // =============================================================
    // 뇌우 적용 : 이동속도 감소
    // =============================================================
    public void ApplyThunderSpeedModifier(float multiplier)
    {
        thunderSpeedMultiplier = multiplier;
        UpdateFullnessState();
    }

    public void ResetThunderSpeedModifier()
    {
        thunderSpeedMultiplier = 1f;
        UpdateFullnessState();
    }

    // =============================================================
    // 낚시 준비
    // =============================================================
    public void FishingReady(Vector3 dir)
    {
        int idx = Get2DirIndex(dir);
        if (idx < 0) return;

        ChangeFishingReadyByIndex(idx);

    }

    // =============================================================
    // 낚시 중
    // =============================================================
    public void FishingHold(Vector3 dir)
    {
        int idx = Get2DirIndex(dir);
        if (idx < 0) return;

        if (idx != _curFishingHoldIdx)
        {
            ChangeFishingHoldByIndex(idx);
            _curFishingHoldIdx = idx;
        }
    }

    // =============================================================
    // 공격
    // =============================================================

    public bool IsMentalDebuff()
    {
        return currentMental < 40.0f;
    }

    public bool BeginCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null || isRunningCoroutineItem || !isActiveAndEnabled || IsDead
            || Time.timeScale <= 0f || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return false;
        StartCoroutine(CoroutineWraper(coroutine));
        return true;
    }

    private IEnumerator CoroutineWraper(IEnumerator coroutine)
    {
        isRunningCoroutineItem = true;
        try { yield return coroutine; }
        finally { isRunningCoroutineItem = false; }
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;
        base.TakeDamage(damage, attacker);
        if (damage > 0) InGameUI.instance?.ShowPlayerDamage(damage, maxHp);
        if (damage > 0 && attacker != null
            && (attacker.GetComponent<EnemyBase>() != null || attacker.GetComponent<ZombieMarinerAI>() != null))
            AudioManager.instance?.PlaySfx(AudioManager.SFX.Hit2);
        PlayerHpChanged?.Invoke(hp);

        if (hp <= 29 && !isPlaySFXLowHp)
        {
            isPlaySFXLowHp = true;
            AudioManager.instance?.PlaySfx(AudioManager.SFX.Sanity29Down);
        }

        if (hp >= 30 && isPlaySFXLowHp)
        {
            isPlaySFXLowHp = false;
        }


        if (attacker != null && attacker.CompareTag("Enemy"))
            AttackedFromEnemy();

        if (currentState == PlayerState.ChargingFishing || currentState == PlayerState.ActionFishing)
        {
            SetState(PlayerState.Default);

            if (playerController != null)
            {
                playerController.CancelFishing();
            }
            UtilityFunctions.Log("피격으로 인해 낚시가 취소되었습니다!");
        }

        if(hp <= 0)
        {
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

            if(currentFullness >= 0)
            {
                currentFullness--;
                currentFullness = Mathf.Clamp(currentFullness, minFullness, maxFullness);
                UpdateFullnessState();

                PlayerFullnessChanged?.Invoke(currentFullness);
            }
            if (isDebugging)
            {
                UtilityFunctions.Log($"굶주림 수치 : {currentFullness}");
            }
        }
    }

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

        float baseMoveSpeed;
        switch (fullnessState)
        {
            case FullnessState.Full:
                baseMoveSpeed = defaultSpeed * 1.2f;
                break;
            case FullnessState.Starving:
                baseMoveSpeed = defaultSpeed * 0.7f;
                break;
            default:
                baseMoveSpeed = defaultSpeed;
                break;
        }
        speed = baseMoveSpeed * thunderSpeedMultiplier;

        if (fullnessState != currentFullnessState)
        {
            RemoveFullnessUI(currentFullnessState);

            currentFullnessState = fullnessState;

            AddFullnessUI(currentFullnessState);

            if (currentFullnessState == FullnessState.Hungry)
            {
                AudioManager.instance?.PlaySfx(AudioManager.SFX.Hunger);
            }

            if (currentFullnessState == FullnessState.Starving)      // 굶주림 상태일때
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

         private IEnumerator StarvingDamageCorountine()
    {
        UtilityFunctions.Log("굶주림 상태 : 체력 감소 시작");
        for(int i = 0; i < fullnessStarvingMax; i++)
        {
            yield return new WaitForSeconds(1f);
            hp -= 1;
            hp = Mathf.Clamp(hp, 0, maxHp);
            PlayerHpChanged?.Invoke(hp);
        }
    }

         public void EatFoodFullness(int increase)
    {
        currentFullness += increase;
        currentFullness = Mathf.Clamp(currentFullness, minFullness, maxFullness);
        UpdateFullnessState();

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



         public void UpdateMental(int increase)
    {
        if(isDrunk)
            return;

        if(currentMental <= 29 && !isPlaySFXMental)
        {
            isPlaySFXMental = true;
            if (AudioManager.instance != null) AudioManager.instance.PlaySfx(AudioManager.SFX.Sanity29Down);
        }

        if (currentMental >= 30 && isPlaySFXMental)
        {
            isPlaySFXMental = false;
        }

        if (CreatureEffect.Instance != null && Time.time - lastEffectTime >= 10f)
        {
            if (increase <= 0)
            {
                var ps = CreatureEffect.Instance.GetEffect(5);
                CreatureEffect.Instance.PlayEffectFollow(ps, PlayerCore.Instance.transform, new Vector3(0f, 0f, 0f));
            }
            else if (increase > 0)
            {
                var ps = CreatureEffect.Instance.GetEffect(4);
                CreatureEffect.Instance.PlayEffectFollow(ps, PlayerCore.Instance.transform, new Vector3(0f, 0f, 0f));
            }

            lastEffectTime = Time.time;
        }

        currentMental += increase;
        currentMental = Mathf.Clamp(currentMental, minMental, maxMental);

        PlayerMentalChanged?.Invoke(currentMental);

        // 수치에 따라 디버프 부여,,
    }

    // 바다이벤트 : 안개 -> 정신력 감소
    public void ReduceMentalByFog()
    {
        int reduceValue = Mathf.RoundToInt(maxMental * 0.1f);
        UpdateMental(-reduceValue);
    }

         public void AttackedFromEnemy()
    {
        UpdateMental(attackedFromEnemy);
    }

         public void ReduceMentalOnMarinerDie()
    {
        float reduce = currentMental * reduceMentalOnMarinerDie;
        UpdateMental(Mathf.RoundToInt(-reduce)); // 반올림하고 았는데 그냥 . 아래 수 버릴거면 수정 가능
    }

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

    public void StartDrunk()
    {
        if (drunkCoroutine != null) StopCoroutine(drunkCoroutine);
        drunkCoroutine = StartCoroutine(Drunk());
    }

    // 술 아이템 사용시 호출
    public IEnumerator Drunk()
    {
        isDrunk = true;
        drunkUntil = Time.time + Mathf.Max(0.01f, drunkDuration);
        RefreshStatusEffectUI();
        while (Time.time < drunkUntil) yield return null;
        isDrunk = false;
        drunkCoroutine = null;
        RefreshStatusEffectUI();
    }

    public void RefreshStatusEffectUI()
    {
        var hud = BuffUIManager.Instance;
        if (hud == null || !isActiveAndEnabled || IsDead) return;
        var fullness = currentFullnessState == FullnessState.Full ? EffectType.Fullness_Full
            : currentFullnessState == FullnessState.Hungry ? EffectType.Fullness_Hungry
            : currentFullnessState == FullnessState.Starving ? EffectType.Fullness_Starving : EffectType.None;
        hud.SetFullnessUI(fullness);
        if (IsMentalDebuff()) hud.BeginUI(EffectType.Mental_Unstable, false);
        else hud.EndUI(EffectType.Mental_Unstable);
        if (isDrunk)
        {
            hud.BeginUI(EffectType.Drunk, true);
            hud.SetRemainingTime(EffectType.Drunk, DrunkRemaining);
        }
        else hud.EndUI(EffectType.Drunk);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (drunkCoroutine != null) StopCoroutine(drunkCoroutine);
        drunkCoroutine = null;
        isDrunk = false;
        if (Instance == this) BuffUIManager.Instance?.ClearAll();
    }

    protected override void OnDestroy()
    {
        if (Instance == this) { BuffUIManager.Instance?.ClearAll(); Instance = null; }
        base.OnDestroy();
    }
    #endregion

    // ==============================================================
    // 헬퍼함수

    private void AddFullnessUI(FullnessState state)
    {
        if (BuffUIManager.Instance == null) return;

        switch (state)
        {
            case FullnessState.Full:
                BuffUIManager.Instance.BeginUI(EffectType.Fullness_Full, true);
                break;
            case FullnessState.Hungry:
                BuffUIManager.Instance.BeginUI(EffectType.Fullness_Hungry, false);
                break;
            case FullnessState.Starving:
                BuffUIManager.Instance.BeginUI(EffectType.Fullness_Starving, false);
                break;
        }
    }

    private void RemoveFullnessUI(FullnessState state)
    {
        if (BuffUIManager.Instance == null) return;

        switch (state)
        {
            case FullnessState.Full:
                BuffUIManager.Instance.EndUI(EffectType.Fullness_Full);
                break;
            case FullnessState.Hungry:
                BuffUIManager.Instance.EndUI(EffectType.Fullness_Hungry);
                break;
            case FullnessState.Starving:
                BuffUIManager.Instance.EndUI(EffectType.Fullness_Starving);
                break;
        }
    }
}
