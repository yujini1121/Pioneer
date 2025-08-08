using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfectedMarinerAI : CreatureBase, IBegin
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;
    private int repairAmount = 30;

    private bool isSecondPriorityStarted = false;

    private float nightConfusionTime; // 랜덤 혼란 시간

    private bool isNight = false;
    private bool isConfused = false;
    private bool isNightBehaviorStarted = false;

    private NavMeshAgent agent;

    private void Awake()
    {
        maxHp = 100;
        speed = 1f;
        attackDamage = 6;
        attackRange = 3f;
        attackDelayTime = 1f;

        // CreatureBase의 fov 변수 사용
        fov = GetComponent<FOVController>();

        gameObject.layer = LayerMask.NameToLayer("Mariner");
    }

    public override void Init()
    {
        agent = GetComponent<NavMeshAgent>();

        // FOVController 초기화
        if (fov != null)
        {
            fov.Init();
        }

        nightConfusionTime = Random.Range(0f, 30f);
        Debug.Log($"감염된 승무원 {marinerId} 초기화 - HP: {maxHp}, 공격력: {attackDamage}, 속도: {speed}");
        Debug.Log($"{marinerId} 밤 혼란 시드값 생성: {nightConfusionTime:F2}초");

        base.Init();
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
    /// 낮 로직
    /// </summary>
    private void StartRepair()
    {
        List<DefenseObject> needRepairList = GameManager.Instance.GetNeedsRepair();

        for (int i = 0; i < needRepairList.Count; i++)
        {
            DefenseObject obj = needRepairList[i];

            if (GameManager.Instance.TryOccupyRepairObject(obj, marinerId))
            {
                targetRepairObject = obj;

                if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
                {
                    Debug.Log($"감염된 승무원 {marinerId} 수리 시작: {targetRepairObject.name}");
                    isRepairing = true;
                    StartCoroutine(MoveToRepairObject(targetRepairObject.transform.position));
                    return;
                }
                else
                {
                    GameManager.Instance.ReleaseRepairObject(obj); // 점유 해제
                }
            }
        }

        if (!isSecondPriorityStarted)
        {
            Debug.Log("감염된 승무원 수리 대상 없음 -> 2순위 행동 시작");
            isSecondPriorityStarted = true;
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    private IEnumerator MoveToRepairObject(Vector3 targetPosition)
    {
        // NavMeshAgent로 수리 대상 위치로 이동
        agent.SetDestination(targetPosition);

        // 이동이 완료될 때까지 기다립니다.
        while (!IsArrived())
        {
            yield return null;
        }

        // 이동 완료 후 수리 작업을 시작합니다.
        StartCoroutine(RepairProcess());
    }

    private IEnumerator RepairProcess()
    {
        float repairDuration = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bool repairSuccess = Random.value > 0.7f;

        if (repairSuccess)
        {
            Debug.Log($"Infected Mariner {marinerId} 수리 성공: {targetRepairObject.name}/ 수리량: {repairAmount}");
            targetRepairObject.Repair(repairAmount);
        }
        else
        {
            Debug.Log($"Infected Mariner {marinerId} 수리 성공: {targetRepairObject.name}/ 수리량: {repairAmount}");
            targetRepairObject.Repair(0);
        }

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("감염된 승무원 밤 도달 예외행동 시작");
            StoreItemsAndReturnToBase();
            yield break;
        }

        StartRepair();
        GameManager.Instance.ReleaseRepairObject(targetRepairObject);
    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("감염된 AI 스파이 활동 진행 중, 쓰레기 파밍 안함.");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : 1; // 임시 홀짝 fallback
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? fallbackIndex : Random.Range(0, spawnPoints.Length);

            if (triedIndexes.Contains(index)) continue;

            if (!GameManager.Instance.IsSpawnerOccupied(index))
            {
                GameManager.Instance.OccupySpawner(index);
                chosenIndex = index;
                Debug.Log("현재 다른 승무원이 사용중 인 스포너");
                break;
            }
            else
            {
                triedIndexes.Add(index);
                float waitTime = Random.Range(0f, 1f);
                Debug.Log("다른 승무원이 점유 중이라 랜덤 시간 후 다시 탐색");
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (chosenIndex == -1)
        {
            Debug.LogWarning("모든 승무원이 사용중 임으로 처음 위치로 이동함.");
            chosenIndex = fallbackIndex;
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        MoveTo(targetSpawn.position);

        while (!IsArrived())
        {
            yield return null;
        }

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("감염된 승무원 밤 행동 시작");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            StoreItemsAndReturnToBase();
            yield break;
        }

        Debug.Log("감염된 승무원 가짜 파밍 10초");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetNeedsRepair();
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

    private void StoreItemsAndReturnToBase()
    {
        Debug.Log("승문원이 감염됨으로 아이템 없음, 버림?");
        // TODO: 감염된 승무원 아이템 버리는 행동 추가 가능
    }

    // ↓ 기존 MoveToTarget 제거 후 NavMeshAgent 버전 추가

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    public bool IsArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    /// <summary>
    /// 밤 로직
    /// </summary>
    private IEnumerator NightBehaviorRoutine() // 혼란 상태
    {
        isNightBehaviorStarted = true;
        isConfused = true;
        Debug.Log("혼란 상태 시작");

        // speed 변수 사용 (혼란 상태에서는 더 빠르게)
        float confusedSpeed = speed * 3f; // CreatureBase의 speed * 3
        float escapedTime = 0;

        float angle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        while (escapedTime < nightConfusionTime)
        {
            escapedTime += Time.deltaTime;
            transform.position += direction * confusedSpeed * Time.deltaTime;

            yield return null;
        }

        isConfused = false;
        Debug.Log("혼란 종료 후 좀비 AI로 변경");

        agent.ResetPath(); // 밤 시작임으로 경로 초기화
        ChangeToZombieAI();
    }

    private void ChangeToZombieAI()
    {
        Debug.Log("좀비 AI전환");

        // ZombieMarinerAI 컴포넌트 추가
        if (GetComponent<ZombieMarinerAI>() == null)
        {
            ZombieMarinerAI zombieAI = gameObject.AddComponent<ZombieMarinerAI>();
            zombieAI.marinerId = this.marinerId;
        }

        // 현재 InfectedMarinerAI 컴포넌트 제거
        Destroy(this);
    }

    // 사망 시 특별한 처리가 필요한 경우 오버라이드
    public override void WhenDestroy()
    {
        Debug.Log($"감염된 승무원 {marinerId} 사망!");
        // 감염된 승무원 사망 시 특별한 로직 추가 가능
        base.WhenDestroy();
    }
}