using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Player Move Speed Setting")]
    [SerializeField]private float moveSpeed = 5f;

    [Header("Player Attack Range Object")]
    [SerializeField]private PlayerAttack playerAttack;

    [Header("Player Attack Power")]
    [SerializeField] private float playerAttackPower = 3f;

    private float attackCoolTime = 0.5f;

    private bool isAttack = false;

    private Rigidbody playerRb;
    private Vector3 playerDir;

    public float playerHP = 100;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerAttack.playerTransform = transform;
    }

    void Update()
    {
        PlayerMovement();

        if(Input.GetMouseButtonDown(0) && !isAttack)
        {            
            StartCoroutine(AttackRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (playerDir.sqrMagnitude > 0)
        {
            Vector3 targetPos = playerRb.position + playerDir * moveSpeed * Time.fixedDeltaTime;
            playerRb.MovePosition(targetPos);
        }
    }

    private void PlayerMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        playerDir = new Vector3(horizontalInput, 0f, verticalInput).normalized;
    }

    private IEnumerator AttackRoutine()
    {
        isAttack = true;
        StartCoroutine(playerAttack.AttackRange());
        yield return new WaitForSeconds(attackCoolTime);
        isAttack = false;
    }

    public void TakeDamage(float damage)
    {
        playerHP -= damage;

        UnityEngine.Debug.Log($"[데미지] 받은 데미지: {damage} 현재 HP: {playerHP}");

        if (playerHP <= 0)
        {
            playerHP = 0;
            Die();
        }
    }

    public void Die()
    {
        UnityEngine.Debug.Log($"[사망] 플레이어 HP가 0 이하가 되었습니다! 현재 HP: {playerHP}");
    }
}
