using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

#region �׳� �޸�
/* =============================================================
 * PlayerStats (CreatureBase ���) : ü��, ���ݷ� ���� �ٽ� ���� �� TakeDamage ���� ��� ����
 [�־�� �� ����]
int hp = 100;					// ü��
int fullness = 100;				// ������
int mental = 100;					// ���ŷ�
int attackDamage = 2; 				// ���ݷ�
float beforeAttackDelay = 0.6f;		// ���� �� ���� �ð� 
float AttackCooldown = 0.4f;			// ���� �� ���� �ð�
float totalAttackTime = 1.0f;			// �� ���� �ð�
int attackPerSecond = 1;			// �ʴ� ���� ���� Ƚ��
float attackRange = 0.4f;			// ���� �Ÿ�
===============================================================
25.09.07 ���� ��
    - ���� ���� ���� �� ��� ������ ������
    - ���ŷ� ����
    - �������ͽ� ���� ����
25.09.09
    - �÷��̾� �� �ٴ� ������ �� ������ �س����� 
    - ���ŷ� ����
    - �������ͽ� ���� ����
    - ������ �� ���ŷ� �ּ�, �ִ� ���� �ɾ�α�
 ============================================================= */
#endregion

// TODO : ��å�� �ý���.cs : ��Ż ����� ������ ��å�� ���� + 1 / CommonUi.cs : �뼺�� Ȯ�� -40%;
public class PlayerCore : CreatureBase, IBegin
{
    public static PlayerCore Instance;

    // ��ü �ý��� ����

        // ������ ������ (fullness ���� ���� ���� ����)
    public enum FullnessState
    {
        Full,       // ��θ� (80 ~ 100)
        Normal,     // ���� (30 ~ 79)
        Hungry,     // ����� (1 ~ 29)
        Starving    // ���ָ� (0)
    }

        // [ ���ݷ� ���� ]
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
        // [ ������ ���� ]  
    public int currentFullness;                                            // ���� ������ ��
    public int maxFullness = 100;                                          // �ִ� ������ ��
    int minFullness = 0;                                            // �ּ� ������ ��
    FullnessState currentFullnessState;                             // ���� ������ ����
    int fullnessStarvingMax = 100;                                  // ���� ���½� ü�� ���̴� �ִ� Ƚ�� (100ȸ)
    private Coroutine starvationCoroutine;                          // ���� ���½� ����Ǵ� �ڷ�ƾ

    [Header("������ ����")]
    [SerializeField] private float fullnessDecreaseTime = 5f;       // ������ �⺻ ���� �ӵ�(�ð�)
    [SerializeField] private float fullnessModifier = 1.3f;         // ������ ���� �ӵ� ������ => 30%

        // [ ���ŷ� ���� ]
    public int currentMental;                                              // ���� ���ŷ� ��
    public int maxMental = 100;                                            // �ִ� ���ŷ� ��
    int minMental = 0;                                              // �ּ� ���ŷ� ��
    bool isDrunk = false;                                           // ���� ���� ����
    private Coroutine enemyExistCoroutine;                          // ���� ���� �� ���ʹ� ����� ����Ǵ� �ڷ�ƾ 
    bool isApplyDebuff = false;

    [Header("���ŷ� ����")]
    [SerializeField] private float existEnemyMentalCool = 2f;        // ���� ���� �� ���ʹ� ����� ���ŷ��� ���̴� �ð� ��
    [SerializeField] private int existEnemyMentalDecrease = -1;      // ���� ���� �� ���ʹ� ����� ���̴� ���ŷ� �� 
    [SerializeField] private int attackedFromEnemy = -3;             // ���ʹ����� ���� ������ ��� ���̴� ���ŷ� ��
    [SerializeField] private float reduceMentalOnMarinerDie = 0.2f; // �¹��� ����� ���̴� ���ŷ� ��
    [SerializeField] private int eatFoodincreaseMental = 10;

    // ���� ���� ���� ����
    [Header("���� ����")]
    [SerializeField] private PlayerAttack playerAttack;
    public PlayerAttack PlayerAttack => playerAttack;
    [SerializeField] private float attackHeight = 1.0f;
    public float AttackHeight => attackHeight;
    [SerializeField] private LayerMask enemyLayer;
    public LayerMask EnemyLayer => enemyLayer;
    public SItemWeaponTypeSO handAttack;

    // �⺻ �ý��� ���� ����
    private Rigidbody playerRb;
    private bool isAttacking = false;
    private float defaultSpeed;

    public static event Action<int> PlayerHpChanged;
    public static event Action<int> PlayerFullnessChanged;
    public static event Action<int> PlayerMentalChanged;

    // �ڷ�ƾ ����
    private bool isRunningCoroutineItem = false;
    public bool IsRunningCoroutineItem => isRunningCoroutineItem;


    void Awake()
    {
        Instance = this;

        playerRb = GetComponent<Rigidbody>();
        SetSetAttribute();
    }
    
    new void Start()
    {
        base.Start();

        UpdateFullnessState();
        StartCoroutine(FullnessSystemCoroutine());                   // ���� ���۽� ������ ��� 1�� ���� ����
    }

    void Update()
    {
        UnityEngine.Debug.Assert(fov != null);
        UnityEngine.Debug.Assert(enemyLayer != null);


        fov.DetectTargets(enemyLayer);
        NearEnemy();
        // UnityEngine.Debug.Log($"���ŷ� ��ġ : {currentMental}");
    }

    #region �⺻ �ý���
    // =============================================================
    // �������ͽ� ���� �� ����
    // =============================================================
    void SetSetAttribute()
    {
        maxHp = 100;
        hp = maxHp;                 // ü��
        speed = 4.0f;               // �̵� �ӵ�
        defaultSpeed = speed;
        currentFullness = 80;              // ������ (���� �� 80)
        currentMental = maxMental;         // ���ŷ� (���� �� 100)
        attackDamage = 2;           // ���ݷ�
        attackDelayTime = 0.4f;     // ���� ��Ÿ��
        attackRange = 0.4f;       // ���� ���� (�̹� attack box ũ�⸦ 0.4�� �����ص�)
    }

    // =============================================================
    // �̵�
    // =============================================================
    public void Move(Vector3 moveInput)
    {
        Vector3 moveVelocity = moveInput.normalized * speed;

        playerRb.velocity = new Vector3(moveVelocity.x, playerRb.velocity.y, moveVelocity.z);
    }

    // =============================================================
    // ����
    // =============================================================
    public void Attack(SItemWeaponTypeSO weapon)
    {
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

        // ���� �� �ִϸ��̼�
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

            // TODO: ���� �ִϸ��̼� ���� �ð� �߰��ؾ� ��!!!!!!!!!!! (0.6��)
            // playerAttack.gameObject.SetActive(true);
            playerAttack.EnableAttackCollider();
            playerAttack.damage = (int)(weapon.weaponDamage);

            if (itemWithState != null)
            {
                itemWithState.duability -= weapon.duabilityRedutionPerHit;
            }

            // �κ��丮 ������Ʈ 
            InventoryUiMain.instance.IconRefresh();
        }

        // ���� �ִϸ��̼� ���� ���� �ð� (0.4��)
        //yield return new WaitForSeconds(attackDelayTime);

        // ���� �ִϸ��̼� ���� ������� ���� ���� �ð�
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
    }
    #endregion

    #region ������
    /* =============================================================
       { ������ }
    - ���۽� 80���� ����, �ִ� 100 �ּ� 0
    - ���� �ð� 5�ʿ� �� ���� 1�� ����
    - �÷��̾� ü���� 50% �̸��̸� ���� �ӵ� 30% ���� 
        - 100 ~ 80 ��θ� ���� : �ӵ� 20% ����
        - 79 ~ 30 ��θ� ���� ����
        - 29 ~ 1 ����� ���� : �ӵ� 30% ����
        - 0 ���ָ� ���� : ü���� �� �� 1�� ���� (�ִ� 100��)
    - ���� ������ ���� �ּ� 5 ~ 80���� ���� ����
        - ���� ������ �������� �˾ƾ� �� ��?
    ====================================
    25.09.07 : ������ ���ָ� �ڷ�ƾ ����
    ============================================================= */


    /// <summary>
    /// �ʴ� ������ 1�� ���� Start �Լ����� ���� (�ڷ�ƾ)
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
            UnityEngine.Debug.Log($"���ָ� ��ġ : {currentFullness}");
        }
    }

    /// <summary>
    /// ������ ��ġ�� ���� ���� ���� �Լ�
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

            if(currentFullnessState == FullnessState.Starving)      // ���ָ� �����϶�
            {
                if(starvationCoroutine == null)
                    starvationCoroutine = StartCoroutine(StarvingDamageCorountine());
            }
            else                                                    // ���ָ� ���°� �ƴҶ�
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
    /// �ʴ� ü�� 1�� �����ϴ� ���ָ� �Լ� (�ڷ�ƾ)
    /// </summary>
    /// <returns></returns>
    private IEnumerator StarvingDamageCorountine()
    {
        UnityEngine.Debug.Log("���ָ� ���� : ü�� ���� ����");
        for(int i = 0; i < fullnessStarvingMax; i++)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(1, this.gameObject);
        }        
    }

    /// <summary>
    /// ���� ����� ������ ����, ������ �Ű������� ����
    /// </summary>
    /// <param name="increase"></param>
    public void EatFoodFullness(int increase)
    {
        currentFullness += increase;
        currentFullness = Mathf.Clamp(currentFullness, minFullness, maxFullness);

        PlayerFullnessChanged?.Invoke(currentFullness);
    }

    // ���ָ� ���� 
    public void RemoveStarvingIEnumerator()
    {
        if(starvationCoroutine != null)
        {
            StopCoroutine(starvationCoroutine);
            starvationCoroutine = null;
        }
    }
    #endregion

    #region ���ŷ�


    /* =============================================================
        { ���ŷ� }
    - ���۽� 100���� ����, 0 ~ 100 ������ ���� ����
    - ���ŷ� 40 ~ 100 : ȿ�� ����
    - ���ŷ� 0 ~ 39 : ���ݷ�, ��ġ �۾� �뼺�� Ȯ��, ��å�� �ý��� ���� ����

    [���� ����]
    - �� �� ������ ���� �������� �����ϸ� ���ŷ� �߰��ϴ� �Լ��� �߰�
        - ������ ��뿡 ���� 5 ~ 80���� ���� ����
        - ���� ���� �� 10�� ���� (���� ��� ����)

    [���� ����]    
        - �÷��̾� �ݰ� 2M �� ���ʹ̰� ������ ��� 2�ʴ� 1�� ����
        - ���ʹ̿��� ���� ���� ��� ���� 1ȸ�� 3�� ���� (�ݰ� �� ���ʹ� ���� ���ǰ� ��ø ����)
        - �¹��� AI ����� ���� ���ŷ��� 20% ����

    [���� ����]
    - ������ �� ���� ���ø� ���� ���°� ��
    - ���� ���� : ���ŷ� ���� �� ���� �Ұ�, �����

    TODO : 
    ============================================================= */



    //// ���� ������!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //void MentalState() // ȣ�� ���� -> update()
    //{
    //    currentAttackDamage = attackDamage;
    //    if (currentMental < 40 && isApplyDebuff == false)
    //    {
    //        // ���ݷ�(5%), ��ġ �۾� �뼺�� Ȯ��(40%)�� ����, ��å�� �ý��� ���� ����(1Lv) => ���ŷ��� 40�̻��� �Ǹ� ���� ������ ���ư�    
    //    }
    //    else
    //    {
    //        RemoveMentalDebuff();
    //    }
    //}

    //// TODO : ���ŷ� ����� (0 ~ 39 ���� ���� ��)  �����ؾ���..
    //void ApplyMentalDebuff()
    //{
    //    isApplyDebuff = true;
    //    // [[ ���ݷ� ���� ]]

    //    float debuffAttackDamage = attackDamage * 0.5f; // ���� ���ݷ��� 5% ����ϱ� ���� ���� ���ݷ� ����
    //    attackDamage -= Mathf.RoundToInt(debuffAttackDamage); // ���ݷ� float������ �ٲ㵵 �Ǵ��� ������ߵ�.. ������ �ݿø� �ؼ� ����

    //    // [[ ��ġ �۾� �뼺�� Ȯ�� ���� ]] => ��� ���� ���� �� �غ���.. ���� Ȯ�� �����ϴ� ��ũ��Ʈ�� PlayerCore ��ũ��Ʈ �����ϰ� ����

    //    // ��å�� �ý��� ���� 1����, 
    //}

    //// TODO : ���ŷ� ����� ����
    //void RemoveMentalDebuff()
    //{
    //    attackDamage = currentAttackDamage;

    //}
    // ���� ����!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    /// <summary>
    /// ���ŷ� ��� ? �޼��� 
    /// </summary>
    /// <param name="increase"></param>
    void UpdateMental(int increase)
    {
        if(isDrunk)
            return;

        currentMental += increase;
        currentMental = Mathf.Clamp(currentMental, minMental, maxMental);

        PlayerMentalChanged?.Invoke(currentMental);

        // ��ġ�� ���� ����� �ο�,,
    }

    /// <summary>
    /// ���ŷ� �ø��� ������ ���� ȣ��, 
    /// </summary>
    /// <param name="increase"></param>
    public void UseMentalItem(int increase)
    {
        UpdateMental(increase);
    }

    /// <summary>
    /// ���� ����� ȣ��, ���� ���� ������� 10�� ����..
    /// </summary>
    public void EatFoodMental()
    {
        UpdateMental(eatFoodincreaseMental);
    }

    /// <summary>
    /// ���ʹ̿��� ���� ���� ��� ���ŷ� ���� ��Ű�� �Լ� -3
    /// </summary>
    public void AttackedFromEnemy()
    {
        UpdateMental(attackedFromEnemy);
    }

    /// <summary>
    /// �¹��� �׾����� ȣ��, ���ŷ� ����, ���� ���ŷ��� 20%
    /// </summary>
    public void ReduceMentalOnMarinerDie()
    {
        float reduce = currentMental * reduceMentalOnMarinerDie;
        UpdateMental(Mathf.RoundToInt(-reduce)); // �ݿø��ϰ� �Ҵµ� �׳� . �Ʒ� �� �����Ÿ� ���� ����
    }

    /// <summary>
    /// �ݰ� 2m ���� ���ʹ̰� ���� ���θ� Ȯ���ϰ� ���ŷ� ���� �ڷ�ƾ ���� �� �ߴ��Ҷ� ȣ�� 
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
    /// ���ʹ� ����� 2�ʿ� �� �� ���ŷ� ���� -1
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

    public bool IsDrunk() // ������������� �����ϴ� �޼��� 
    {
        return isDrunk;
    }

    // �� ������ ���� ȣ��
    public IEnumerator Drunk()
    {
        isDrunk = true;

        yield return new WaitForSeconds(60f);

        isDrunk = false;
    }
    #endregion

}