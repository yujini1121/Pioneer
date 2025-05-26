using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfectedMarinerAI : MonoBehaviour
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;

    [Header("오브젝트 수리 회복량 임시")]
    private int repairAmount = 30;

    private bool isSecondPriorityStarted = false;
    private float nightConfusionDuration; // 랜덤 혼란 확률

    private bool isNight = false;
    private bool isConfused = false;
    private bool isNightBehaviorStarted = false;

    private void Start()
    {
        nightConfusionDuration = Random.Range(0f, 30f);
        Debug.Log($"{marinerId} 밤 혼란 시드값 생성: {nightConfusionDuration:F2}초");
    }

    private void Update()
    {
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
        List<DefenseObject> needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0]; // 임시로 index 0번 테스트 수리
            Debug.Log($"Infected Mariner {marinerId} 수리할 오브젝트 이름 : {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log($"Infected Mariner {marinerId} 수리를 시작합니다 : {targetRepairObject.name}");
                isRepairing = true;
                StartCoroutine(RepairProcess());
            }
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log($"Infected Mariner {marinerId} 수리할 오브젝트가 없음으로 2순위 행동 시작");
                isSecondPriorityStarted = true;
                StartCoroutine(StartSecondPriorityAction());
            }
        }
    }

    private IEnumerator RepairProcess()
    {
        float repairDuration = 3f;
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
            Debug.Log("수리실패 , 로직 추가 필요");
        }

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"Infected Mariner {marinerId} 수리 완료 직후 밤 도달 예외 행동 실행");
            StoreItemsAndReturnToBase();
            yield break;
        }

        StartRepair();
    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"감염된 AI 스파이 활동 진행 중, 쓰레기 파밍 안함.");

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
                Debug.Log($"Infected Mariner {marinerId} 현재 선택된 스포너: {index}");
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
            Debug.LogWarning($"Infected Mariner {marinerId} 모든 스포너가 점유 중, fallback 위치로 이동");
            chosenIndex = fallbackIndex;
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        yield return StartCoroutine(MoveToTarget(targetSpawn.position));

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"Infected Mariner {marinerId} 밤 예외 행동 진입");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            StoreItemsAndReturnToBase();
            yield break;
        }

        Debug.Log($"Infected Mariner {marinerId} 파밍 시작 (10초)");

        yield return new WaitForSeconds(10f);
        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log($"Infected Mariner {marinerId} 수리 대상 발견이 돼. 1순위 행동으로 전환됨");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log($"Infected Mariner {marinerId} 수리 대상 미발견으로 2순위 행동 유지");
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    private void StoreItemsAndReturnToBase()
    {
        Debug.Log("승문원이 감염됨으로 아이템 없음");
    }

    public IEnumerator MoveToTarget(Vector3 destination, float stoppingDistance = 2f)
    {
        float speed = 2f;
        while (Vector3.Distance(transform.position, destination) > stoppingDistance)
        {
            Vector3 direction = (destination - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 밤 로직
    /// </summary>

    private IEnumerator NightBehaviorRoutine() // 혼란 상태
    {
        if (isNightBehaviorStarted) yield break;
        isNightBehaviorStarted = true;

        isConfused = true;
        Debug.Log("혼란상태입니다.");

        float elapsed = 0f;
        float confusionSpeed = 3f;

        float angle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        while (elapsed < nightConfusionDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += direction * confusionSpeed * Time.deltaTime;
            yield return null;
        }

        isConfused = false;
        Debug.Log("혼란 종료 후 좀비 AI로 변경");

        ChangeToZombieAI();
    }

    private void ChangeToZombieAI()
    {
        Debug.Log("좀비 AI전환");
    }
}
