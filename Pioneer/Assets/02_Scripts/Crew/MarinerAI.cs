using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerAI : MonoBehaviour
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;
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
        List<DefenseObject> needRepairList = GameManager.Instance.GetNeedsRepair();

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0]; // 임시로 index 0번 테스트 수리

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log("승무원 수리 중");
                isRepairing = true;
                StartCoroutine(RepairProcess());
            }
            Debug.Log($"Mariner {marinerId} 수리된 오브젝트 : {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log("수리 오브젝트 없음으로 2순위 행동 시작");
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

        Debug.Log($"Mariner {marinerId} 수리 완료: {targetRepairObject.name}/ 수리량: {repairAmount}");
        targetRepairObject.Repair(repairAmount);

        isRepairing = false;
        GameManager.Instance.UpdateRepairTargets();

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("일반 승무원 밤 도달 예외행동 시작");
            GameManager.Instance.StoreItemsAndReturnToBase(this); // 임시 수정 필요
            yield break;
        }

        StartRepair();
    }

    public IEnumerator StartSecondPriorityAction()
    {
        Debug.Log("일반 승무원 2순위 낮 행동 시작");

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
                Debug.Log("현재 다른 승무원이 사용중 인 스포너");
                
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

        if (chosenIndex == -1) // 예외 처리 필요할까?
        {
            Debug.LogWarning("모든 승무원이 사용중 임으로 처음 위치로 이동함.");
            chosenIndex = fallbackIndex; // 첫 위치로 이동
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        yield return StartCoroutine(MoveToTarget(targetSpawn.position));

        if (GameManager.Instance.TimeUntilNight() <= 30f)
        {
            Debug.Log("승무원 밤 행동 시작");
            GameManager.Instance.ReleaseSpawner(chosenIndex);
            GameManager.Instance.StoreItemsAndReturnToBase(this);
            yield break;
        }

        Debug.Log("승무원 10초 동안 수리");
        yield return new WaitForSeconds(10f);

        GameManager.Instance.CollectResource("wood"); // 출력만
        GameManager.Instance.ReleaseSpawner(chosenIndex);

        var needRepairList = GameManager.Instance.GetNeedsRepair();
        if (needRepairList.Count > 0)// 수리대상 확인
        {
            Debug.Log("승무원 수리 대상 발견으로 1순위 행동 실행");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log("승무원 수리 대상 미발견으로 2순위 행동 실행");
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