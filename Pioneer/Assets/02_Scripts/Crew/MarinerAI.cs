using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerAI : MonoBehaviour
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;

    [Header("오브젝트 수리 회복량 임시")]
    private int repairAmount = 30;

    private bool isSecondPriorityStarted = false;

    private void Update()
    {
        if (!isRepairing && GameManager.Instance.IsDaytime)
        {
            StartRepair();
        }
    }

    private void StartRepair()
    {
        List<DefenseObject> needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0]; // 임시로 index 0번 테스트 수리
            Debug.Log($"[Mariner {marinerId}] 수리할 오브젝트 이름 : {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log($"[Mariner {marinerId}] 수리를 시작합니다 : {targetRepairObject.name}");
                isRepairing = true;
                StartCoroutine(RepairProcess());
            }
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log($"[Mariner {marinerId}] 수리할 오브젝트가 없음으로 2순위 행동 시작");
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

        Debug.Log($"[Mariner {marinerId}] 수리 완료: {targetRepairObject.name}/ 수리량: {repairAmount}");
        targetRepairObject.Repair(repairAmount);

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"[Mariner {marinerId}] 수리 완료 직후 밤 도달 예외 행동 실행");
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        StartRepair();
    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log($"[Mariner {marinerId}] 2순위 행동 시작: 바다 쓰레기 파밍");

        GameObject[] spawnPoints = GameManager.Instance.spawnPoints;
        List<int> triedIndexes = new List<int>();
        int fallbackIndex = (marinerId % 2 == 0) ? 0 : 1; // 임시로 스포너는 0 과 1로 홀짝 구현은 나중에?
        int chosenIndex = -1;

        while (triedIndexes.Count < spawnPoints.Length)
        {
            int index = triedIndexes.Count == 0 ? fallbackIndex : Random.Range(0, spawnPoints.Length);
            // 현재 0과 1만 사용 중 나중에 스포너 범위 들어오면 수정

            if (triedIndexes.Contains(index)) continue; // 이미 시도한 스포너는 건뛰

            if (!GameManager.Instance.IsSpawnerOccupied(index)) // 비 점유 중
                // 선택된 스포너가 이미 다른 유닛이 선택했는가? 플로우차트 확인
            {
                GameManager.Instance.OccupySpawner(index);
                chosenIndex = index;
                Debug.Log($"[Mariner {marinerId}] 현재 선택된 스포너: {index}");
                
                break;
            }
            else // 점유중
            {
                triedIndexes.Add(index);
                float waitTime = Random.Range(0f, 1f);
                Debug.Log("다른 승무원이 점유 중이라 랜덤 시간 후 다시 탐색");
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (chosenIndex == -1)
        {
            Debug.LogWarning($"[Mariner {marinerId}] 모든 스포너가 점유 중, fallback 위치로 이동");
            chosenIndex = fallbackIndex; // 첫 위치로 이동
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        yield return StartCoroutine(MoveToTarget(targetSpawn.position));

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log($"[Mariner {marinerId}] 밤 예외 행동 진입");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log($"[Mariner {marinerId}] 파밍 시작 (10초)");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood"); // 출력만
        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log($"[Mariner {marinerId}] 수리 대상 발견이 돼. 1순위 행동으로 전환됨");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log($"[Mariner {marinerId}] 수리 대상 미발견으로 2순위 행동 유지");
            StartCoroutine(StartSecondPriorityAction());
        }
    }

    public IEnumerator MoveToTarget(Vector3 destination, float stoppingDistance = 2f) // 2,2,2?? 지점 멈춤?
    {
        float speed = 2f;
        while (Vector3.Distance(transform.position, destination) > stoppingDistance) // 2M 전까지 이동
        {
            Vector3 direction = (destination - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            yield return null;
        }
    }
}