using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("시간 설정")]
    public bool IsDaytime = true;
    public float currentGameTime = 0f;
    public float nightStartTime = 50f; // 임시 값

    [Header("스포너 지점 임시 (0~1 index만 사용 중)")]
    public GameObject[] spawnPoints;

    private List<DefenseObject> repairTargets = new List<DefenseObject>();
    private HashSet<int> occupiedSpawners = new HashSet<int>();  

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        UpdateRepairTargets();
    }

    private void Update()
    {
        currentGameTime += Time.deltaTime;

        if (currentGameTime >= nightStartTime && IsDaytime)
        {
            IsDaytime = false;
            Debug.Log("밤이 되었습니다.");
        }
    }

    public void UpdateRepairTargets()
    {
        repairTargets.Clear();
        DefenseObject[] defenseObjects = FindObjectsOfType<DefenseObject>();

        foreach (var obj in defenseObjects)
        {
            if (obj.currentHP < obj.maxHP * 0.5f)
            {
                repairTargets.Add(obj);
                Debug.Log($"수리 대상 추가: {obj.name}/ HP: {obj.currentHP}/{obj.maxHP}");
            }
        }
    }

    public List<DefenseObject> GetNeedsRepair()
    {
        List<DefenseObject> needRepair = new List<DefenseObject>();
        foreach (var obj in repairTargets)
        {
            if (obj.currentHP < obj.maxHP * 0.5f)
                needRepair.Add(obj);
        }
        return needRepair;
    }

    public bool CanMarinerRepair(int marinerId, DefenseObject target)
    {
        return true;
    }

    public float TimeUntilNight()
    {
        return Mathf.Max(0f, nightStartTime - currentGameTime);
    }

    public void CollectResource(string type)
    {
        Debug.Log($" 자원 획득: {type}"); // 임시
    }

    public void StoreItemsAndReturnToBase(MarinerAI mariner)
    {
        Debug.Log($"승무원 {mariner.marinerId}] 아이템 저장 후 숙소 복귀");

        if (HasStorage())
        {
            Vector3 dormPosition = new Vector3(0f, 0f, 0f); // 예시 위치
            mariner.StartCoroutine(mariner.MoveToTarget(dormPosition));
        }
        else
        {
            Debug.Log("보관함 없음 "); // 임시.
            mariner.StartCoroutine(mariner.StartSecondPriorityAction());
        }
    }

    public bool HasStorage() // 보관함 임시로 항상 True 설정
    {
        return true;
    }

    public bool IsSpawnerOccupied(int index) // 점유중인지
    {
        return occupiedSpawners.Contains(index); // 점유시 true, 아니면 false
    }

    public void OccupySpawner(int index) // 점유 상태
    {
        occupiedSpawners.Add(index); // 추가해서 다른 승무원 사용 불가
    }

    public void ReleaseSpawner(int index) // 끝날 시
    {
        occupiedSpawners.Remove(index); // 삭제
    }
}
