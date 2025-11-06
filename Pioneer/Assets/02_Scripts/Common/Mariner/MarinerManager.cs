using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerManager : MonoBehaviour
{
    public static MarinerManager Instance;

    [Header("감염 설정")]
    public int firstInfectionDay = 4;          // 4일차 이후부터 감염 시작 왜 4일차에 3명이 소환되지?
    public float infectionDelayWithinDay = 5f; // 낮 시작 후 감염까지 지연

    private int lastInfectedDay = -1;          // 하루 1회 감염 제한
    private bool infectionRoutineRunning = false; // 중복 실행 방지

    private List<MarinerAI> allMariners = new List<MarinerAI>();
    private List<DefenseObject> repairTargets = new List<DefenseObject>();
    private HashSet<int> occupiedSpawners = new HashSet<int>();
    private Dictionary<int, int> repairOccupancy = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        UpdateRepairTargets();
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (allMariners.Count > 0)
            allMariners.RemoveAll(m => m == null);

        if (gm.IsDaytime && gm.currentDay >= firstInfectionDay
            && gm.currentDay != lastInfectedDay
            && !infectionRoutineRunning)
        {
            infectionRoutineRunning = true;
            StartCoroutine(InfectOneRandomMarinerAtDaytime());
        }
    }

    private IEnumerator InfectOneRandomMarinerAtDaytime()
    {
        var gm = GameManager.Instance;
        if (gm == null) { infectionRoutineRunning = false; yield break; }

        // 혹시 시작 시점이 밤이면 낮까지 대기
        while (!gm.IsDaytime) yield return null;

        int dayAtStart = gm.currentDay;

        // 지연
        if (infectionDelayWithinDay > 0f)
            yield return new WaitForSeconds(infectionDelayWithinDay);

        if (!gm.IsDaytime || gm.currentDay != dayAtStart)
        {
            infectionRoutineRunning = false;
            yield break;
        }

        // 후보 수집
        var candidates = new List<MarinerAI>();
        foreach (var m in allMariners)
            if (m != null && m.GetComponent<InfectedMarinerAI>() == null)
                candidates.Add(m);

        if (candidates.Count > 0)
        {
            var pick = candidates[Random.Range(0, candidates.Count)];
            InfectMariner(pick);
            Debug.Log("낮 시간 랜덤 1명 감염 완료");
        }
        else
        {
            Debug.Log("감염 가능한 승무원이 없습니다.");
        }
        lastInfectedDay = dayAtStart;
        infectionRoutineRunning = false;
    }

    /// <summary>
    /// 감염 전환 mariner -> infected
    /// </summary>
    /// <param name="mariner"></param>
    private void InfectMariner(MarinerAI mariner)
    {
        if (mariner == null) return;

        Debug.Log($"감염 발생: 승무원 {mariner.marinerId}");

        int id = mariner.marinerId;
        var go = mariner.gameObject;

        // 기존 AI 즉시 중지
        mariner.enabled = false;
        mariner.StopAllCoroutines();

        // 이동 중이면 멈춤
        var agent = go.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        // 새 감염 AI 부착(중복 방지)
        var infected = go.GetComponent<InfectedMarinerAI>();
        if (infected == null)
            infected = go.AddComponent<InfectedMarinerAI>();

        infected.marinerId = id;

        var anim = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>(true);
        infected.SetAnimator(anim);

        // 기존 AI는 프레임 끝에서 제거
        Destroy(mariner);

        Debug.Log($"승무원 {id}: 감염 상태로 전환 완료 (InfectedMarinerAI 활성화)");
    }

    /*private IEnumerator AttachInfectedAI(Transform marinerTransform, int id)
    {
        yield return null; // 한 프레임 대기 (Destroy 반영 대기)

        InfectedMarinerAI infected = marinerTransform.gameObject.AddComponent<InfectedMarinerAI>();
        infected.marinerId = id;

        Debug.Log($"승무원 {id}: 감염 상태로 전환 완료 (InfectedMarinerAI 활성화)");
    }*/

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
