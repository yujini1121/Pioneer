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
        if (fov != null)
        {
            fov.Start();
        }

        nightConfusionTime = Random.Range(0f, 30f);
        Debug.Log($"감염된 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}, 속도: {speed}");
        Debug.Log($"{marinerId} 밤 혼란 시드값 생성: {nightConfusionTime:F2}초");

        base.Start();
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
        Debug.Log("감염된 AI 스파이 활동 진행 중, 쓰레기 파밍 안함.");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;

        int chosenIndex = marinerId % spawnPoints.Length;

        Debug.Log($"감염된 승무원 {marinerId}: 할당된 스포너 {chosenIndex}로 이동");

        UnityEngine.Transform targetSpawn = spawnPoints[chosenIndex].transform;
        MoveTo(targetSpawn.position);

        while (!IsArrived())
        {
            yield return null;
        }

        // NavMesh 경로 초기화
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("감염된 승무원 밤 행동 시작");
            StoreItemsAndReturnToBase(); // 감염된 승무원 전용 처리
            yield break;
        }

        Debug.Log("감염된 승무원 가짜 파밍 10초");
        yield return new WaitForSeconds(10f);

        var needRepairList = MarinerManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log("감염된 승무원 수리 대상 발견으로 1순위 행동 실행");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log("감염된 승무원 수리 대상 미발견으로 2순위 행동 실행");
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
}