using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 현재 공격 대상이 시야안에 들어왔을 때 공격 로직 미구현
/// </summary>
public class ZombieMarinerAI : MonoBehaviour
{
    public enum ZombieState { Wandering, Idle } // 떠돌다 , 대기
    private ZombieState currentState = ZombieState.Wandering;

    private float speed = 1f;
    private float hp = 40f;
    private float moveDuration = 2f;
    private float idleDuration = 4f;

    private float stateTimer = 0f;
    private Vector3 moveDirection;

    private void Start()
    {
        InitZombieStats();
        SetRandomDirection();
        stateTimer = moveDuration;
        Debug.Log("좀비 승무원 작동 중");
    }

    private void InitZombieStats()
    {
        if (hp > 40f)
        {
            Debug.Log("좀비 AI HP 자동 조정");
            hp = 40f;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case ZombieState.Wandering:
                Wander();
                break;

            case ZombieState.Idle:
                Idle();
                break;
        }
    }

    /// <summary>
    /// 이동 -> 대기 -> 이동 , 플레이어 발견시 공격
    /// </summary>

    private void Wander()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
        stateTimer -= Time.deltaTime;

        Debug.DrawRay(transform.position, moveDirection * 2f, Color.green); // 이동 방향 시각화

        if (stateTimer <= 0f)
        {
            Debug.Log("좀비 AI 이동 후 대기 상태");
            EnterIdleState();
        }
    }

    private void Idle()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Debug.Log("종비 AI 대기에서 다시 이동 상태");
            EnterWanderingState();
        }
    }

    private void EnterWanderingState()
    {
        SetRandomDirection();
        currentState = ZombieState.Wandering;
        stateTimer = moveDuration;
        Debug.Log("랜덤 방향으로 이동 시작");
    }

    private void EnterIdleState()
    {
        currentState = ZombieState.Idle;
        stateTimer = idleDuration;
        Debug.Log("좀비 AI 대기 상태로 전환");
    }


    /// <summary>
    /// 랜덤 방향 생성
    /// </summary>

    private void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f); // 랜덤 방향 후 MOVE
        moveDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }




}
