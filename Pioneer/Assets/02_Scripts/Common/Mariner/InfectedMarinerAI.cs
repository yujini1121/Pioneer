using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfectedMarinerAI : MarinerBase, IBegin
{
    // 감염된 승무원 고유 설정
    public int marinerId;

    // 밤 혼란 관련
    private float nightConfusionTime; // 랜덤 혼란 시간
    private bool isNight = false;
    private bool isConfused = false;
    private bool isNightBehaviorStarted = false;

    private void Awake()
    {
        maxHp = 100;
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        fov = GetComponent<FOVController>();

        gameObject.layer = LayerMask.NameToLayer("Mariner");
    }

    public override void Start()
    {
        base.Start();  // 먼저 호출

        if (fov != null)
        {
            fov.Start();
        }

        nightConfusionTime = Random.Range(0f, 30f);
        Debug.Log($"감염된 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}, 속도: {speed}");
        Debug.Log($"{marinerId} 밤 혼란 시드값 생성: {nightConfusionTime:F2}초");
    }

    private void Update()
    {
        if (IsDead) return;

        if (GameManager.Instance.IsDaytime && !isNightBehaviorStarted)
        {
            isNight = false;

            if (!isRepairing)
            {
                StartRepair();
            }
        }
        else if (!isNight)
        {
            isNight = true;
            StartCoroutine(NightBehaviorRoutine());
        }
    }

    /// <summary>
    /// 감염된 승무원의 2순위 행동 (가짜 파밍)
    /// </summary>
    public override IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"감염된 승무원 {marinerId}: 개인 경계에서 가짜 파밍");
        yield return StartCoroutine(MoveToMyEdgeAndFarm());

        var needRepairList = MarinerManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)
        {
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    /// <summary>
    /// 감염된 승무원 아이템 처리 (아이템 버림)
    /// </summary>
    private void StoreItemsAndReturnToBase()
    {
        Debug.Log("승무원이 감염됨으로 아이템 없음, 버림?");
        // TODO: 감염된 승무원 아이템 버리는 행동 추가 가능
    }

    /// <summary>
    /// 밤 혼란 행동
    /// </summary>
    private IEnumerator NightBehaviorRoutine()
    {
        isNightBehaviorStarted = true;
        isConfused = true;
        Debug.Log("혼란 상태 시작");

        float escapedTime = 0;

        float angle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        while (escapedTime < nightConfusionTime)
        {
            escapedTime += Time.deltaTime;
            transform.position += direction * speed * Time.deltaTime;

            yield return null;
        }

        isConfused = false;
        Debug.Log("혼란 종료 후 좀비 AI로 변경");

        agent.ResetPath();
        ChangeToZombieAI();
    }

    /// <summary>
    /// 좀비 AI로 전환
    /// </summary>
    private void ChangeToZombieAI()
    {
        Debug.Log("좀비 AI전환");

        if (GetComponent<ZombieMarinerAI>() == null)
        {
            ZombieMarinerAI zombieAI = gameObject.AddComponent<ZombieMarinerAI>();
            zombieAI.marinerId = this.marinerId;
        }

        Destroy(this);
    }

    /// <summary>
    /// 감염된 승무원은 30% 수리 성공률 (70% 실패)
    /// </summary>
    protected override float GetRepairSuccessRate()
    {
        return 0.3f; // 30% 성공률
    }

    /// <summary>
    /// 승무원 ID 반환
    /// </summary>
    protected override int GetMarinerId()
    {
        return marinerId;
    }

    /// <summary>
    /// 승무원 타입 이름 반환
    /// </summary>
    protected override string GetCrewTypeName()
    {
        return "감염승무원";
    }

    /// <summary>
    /// 밤이 다가올 때 처리 (감염된 승무원 전용)
    /// </summary>
    protected override void OnNightApproaching()
    {
        StoreItemsAndReturnToBase();
    }

    protected override void OnPersonalFarmingCompleted()
    {
        // 감염된 승무원은 자원을 수집하지 않음
        Debug.Log($"감염된 승무원 {marinerId}: 개인 경계에서 가짜 파밍 완료");
    }
}