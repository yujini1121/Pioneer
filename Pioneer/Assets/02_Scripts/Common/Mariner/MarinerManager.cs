using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerManager : MonoBehaviour
{
    public static MarinerManager Instance;

    [Header("감염 설정(수정 필요)")]
    public float infectionStartTime = 180f;
    public float infectionInterval = 10f;
    private bool infectionStarted = false;

    private List<MarinerAI> allMariners = new List<MarinerAI>();
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
        if (!infectionStarted && GameManager.Instance.currentGameTime >= infectionStartTime)
        {
            infectionStarted = true;
            StartCoroutine(InfectMarinersOneByOne());
        }
    }


    /// <summary>
    /// 승무원들을 하나씩 감염시키는 코루틴
    /// </summary>
    private IEnumerator InfectMarinersOneByOne()
    {
        Debug.Log("감염 프로세스 시작됨");

        var marinerQueue = new List<MarinerAI>(allMariners);

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

    /// <summary>
    /// 개별 승무원을 감염시키는 함수
    /// </summary>
    private void InfectMariner(MarinerAI mariner)
    {
        if (mariner == null) return;

        Debug.Log($"감염 발생: 승무원 {mariner.marinerId}");

        InfectedMarinerAI infected = mariner.GetComponent<InfectedMarinerAI>();
        if (infected != null)
        {
            infected.enabled = true;
            infected.marinerId = mariner.marinerId;
        }

        mariner.enabled = false;
    }

    /// <summary>
    /// 승무원을 등록하는 함수
    /// </summary>
    public void RegisterMariner(MarinerAI mariner)
    {
        if (!allMariners.Contains(mariner))
        {
            allMariners.Add(mariner);
        }
    }

    /// <summary>
    /// 수리 대상 목록을 업데이트하는 함수
    /// </summary>
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

    /// <summary>
    /// 수리가 필요한 오브젝트 목록을 반환하는 함수
    /// </summary>
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

    /// <summary>
    /// 승무원이 수리할 수 있는지 확인하는 함수
    /// </summary>
    public bool CanMarinerRepair(int marinerId, DefenseObject target)
    {
        return true;
    }

    /// <summary>
    /// 승무원이 아이템을 저장하고 숙소로 돌아가는 함수
    /// </summary>
    /*public void StoreItemsAndReturnToBase(MarinerAI mariner)
    {
        Debug.Log($"승무원 [{mariner.marinerId}] 아이템 저장 후 숙소 복귀");

        if (HasStorage())
        {
            Vector3 dormPosition = new Vector3(0f, 0f, 0f); // 예시
            mariner.StartCoroutine(mariner.MoveToThenReset(dormPosition));
        }
        else
        {
            Debug.Log("보관함 없음 ");
            mariner.StartCoroutine(mariner.StartSecondPriorityAction());
        }
    }*/

    /// <summary>
    /// 저장소가 있는지 확인하는 함수
    /// </summary>
    public bool HasStorage()
    {
        return true;
    }

   /* /// <summary>
    /// 스포너가 점유되어 있는지 확인하는 함수
    /// </summary>
    public bool IsSpawnerOccupied(int index)
    {
        return occupiedSpawners.Contains(index);
    }

    /// <summary>
    /// 스포너를 점유하는 함수
    /// </summary>
    public void OccupySpawner(int index)
    {
        occupiedSpawners.Add(index);
    }

    /// <summary>
    /// 스포너 점유를 해제하는 함수
    /// </summary>
    public void ReleaseSpawner(int index)
    {
        occupiedSpawners.Remove(index);
    }*/

    /// <summary>
    /// 수리 오브젝트가 점유되어 있는지 확인하는 함수
    /// </summary>
    public bool IsRepairObjectOccupied(DefenseObject obj)
    {
        return repairOccupancy.ContainsKey(obj.GetInstanceID());
    }

    /// <summary>
    /// 수리 오브젝트 점유를 시도하는 함수
    /// </summary>
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

    /// <summary>
    /// 수리 오브젝트 점유를 해제하는 함수
    /// </summary>
    public void ReleaseRepairObject(DefenseObject obj)
    {
        int id = obj.GetInstanceID();
        if (repairOccupancy.ContainsKey(id))
            repairOccupancy.Remove(id);
    }
}
