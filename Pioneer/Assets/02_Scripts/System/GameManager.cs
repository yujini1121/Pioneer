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

    private List<MarinerAI> allMariners = new List<MarinerAI>();
    private bool infectionStarted = false;
    public float infectionStartTime = 180f;
    public float infectionInterval = 10f;

    private List<DefenseObject> repairTargets = new List<DefenseObject>();
    private HashSet<int> occupiedSpawners = new HashSet<int>();
    private Dictionary<int, int> repairOccupancy = new Dictionary<int, int>();

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

        if (!infectionStarted && currentGameTime >= infectionStartTime)
        {
            infectionStarted = true;
            StartCoroutine(InfectMarinersOneByOne());
        }

    }

    private IEnumerator InfectMarinersOneByOne()
    {
        Debug.Log("감염 프로세스 시작됨");

        var marinerQueue = new List<MarinerAI>(allMariners); // 사본 생성

        foreach (var mariner in marinerQueue)
        {
            if (mariner != null)
            {
                InfectMariner(mariner);
                yield return new WaitForSeconds(infectionInterval);
            }
        }

        Debug.Log("모든 승무원 감염 완료");
    }

    private void InfectMariner(MarinerAI mariner)
    {
        if (mariner == null) return;

        Debug.Log("감염 발생");

        int id = mariner.marinerId;
        GameObject go = mariner.gameObject;

        Destroy(mariner); // MarinerAI 제거

        InfectedMarinerAI infected = go.AddComponent<InfectedMarinerAI>();
        infected.marinerId = id;
    }

    public void RegisterMariner(MarinerAI mariner)
    {
        if (!allMariners.Contains(mariner))
        {
            allMariners.Add(mariner);
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
        Debug.Log($"승무원 [{mariner.marinerId}] 아이템 저장 후 숙소 복귀");

        if (HasStorage())
        {
            Vector3 dormPosition = new Vector3(0f, 0f, 0f); // 예시
            mariner.StartCoroutine(mariner.MoveToThenReset(dormPosition));
        }
        else
        {
            Debug.Log("보관함 없음 "); // 임시.
            mariner.StartCoroutine(mariner.StartSecondPriorityAction());
        }
    }

    private IEnumerator WaitUntilArrivalThenIdle(MarinerAI mariner)
    {
        while (!mariner.IsArrived())
        {
            yield return null;
        }

        Debug.Log($"승무원 [{mariner.marinerId}] 숙소 도착 후 대기 상태로 전환");
        // 필요시 상태 초기화 등의 추가 로직 작성 가능
    }


    public bool HasStorage() // 보관함 임시로 항상 True 설정
    {
        return true;
    }

    /// <summary>
    /// 파밍 스포너 점유 여부
    /// </summary>
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


    /// <summary>
    /// 수리 오브젝트 점유 여부
    /// </summary>
    

    public bool IsRepairObjectOccupied(DefenseObject obj)
    {
        return repairOccupancy.ContainsKey(obj.GetInstanceID());
    }

    public bool TryOccupyRepairObject(DefenseObject obj, int marinerId)
    {
        int id = obj.GetInstanceID();
        if (!repairOccupancy.ContainsKey(id))
        {
            repairOccupancy[id] = marinerId;
            return true;
        }
        return false;
    }

    public void ReleaseRepairObject(DefenseObject obj)
    {
        int id = obj.GetInstanceID();
        if (repairOccupancy.ContainsKey(id))
            repairOccupancy.Remove(id);
    }
}
