using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfectedMarinerAI : MonoBehaviour
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

    private void Start()
    {
        nightConfusionTime = Random.Range(0f, 30f);
        Debug.Log($"{marinerId} 밤 혼란 시드값 생성: {nightConfusionTime:F2}초");
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
            Debug.Log($"Infected Mariner {marinerId} 수리할 오브젝트 이름 : {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");
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
            Debug.Log("감염된 승무원 밤 도달 예외행동 시작");
            StoreItemsAndReturnToBase();
            yield break;
        }

        StartRepair();
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

        if (chosenIndex == -1) // 예외 처리 필요할까?
        {
            Debug.LogWarning("모든 승무원이 사용중 임으로 처음 위치로 이동함.");
            chosenIndex = fallbackIndex;
        }

        Transform targetSpawn = spawnPoints[chosenIndex].transform;
        yield return StartCoroutine(MoveToTarget(targetSpawn.position));

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
        isNightBehaviorStarted = true;
        isConfused = true;
        Debug.Log("혼란 상태 시작");

        float confusedSpeed = 3f;
        float escapedTime = 0;

        float angle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized; // 랜덤 방향 설정

        while (escapedTime < nightConfusionTime) {
            escapedTime += Time.deltaTime;
            transform.position += direction * confusedSpeed * Time.deltaTime;

            yield return null;
        }

        isConfused = false;
        Debug.Log("혼란 종료 후 좀비 AI로 변경");

        ChangeToZombieAI();
    }

    private void ChangeToZombieAI()
    {
        Debug.Log("좀비 AI전환");

        if (GetComponent<ZombieMarinerAI>() == null)
        {
            gameObject.AddComponent<ZombieMarinerAI>();
        }
        // 필요하다면 승무원 ID 전달? 필요없을거 같음
        // zombieAI.marinerId = this.marinerId;

        Destroy(this);
    }
}
