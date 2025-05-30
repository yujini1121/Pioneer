using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Move Speed Setting")]
    [SerializeField]private float moveSpeed = 5f;

    [Header("Player Attack Range Object")]
    [SerializeField]private PlayerAttack playerAttack;

    private float playerHP = 100;
    private float attackCoolTime = 0.5f;

    private bool isAttack = false;

    private Rigidbody playerRb;
    private Vector3 playerDir;

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

        if (playerDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(playerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttack = true;
        StartCoroutine(playerAttack.AttackRange());
        yield return new WaitForSeconds(attackCoolTime);
        isAttack = false;
    }
}
