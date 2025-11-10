using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerManager : MonoBehaviour
{
    public static MarinerManager Instance;

    [Header("감염 설정")]
    public int firstInfectionDay = 4;
    public float infectionDelayWithinDay = 5f;

    private int lastInfectedDay = -1;
    private bool infectionRoutineRunning = false;

    private List<MarinerAI> allMariners = new List<MarinerAI>();

    // 리 타깃 컨테이너 
    // DefenseObject -> StructureBase
    private List<StructureBase> repairTargets = new List<StructureBase>();

    private HashSet<int> occupiedSpawners = new HashSet<int>();

    // 키는 StructureBase 인스턴스 ID
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

        while (!gm.IsDaytime) yield return null;

        int dayAtStart = gm.currentDay;

        if (infectionDelayWithinDay > 0f)
            yield return new WaitForSeconds(infectionDelayWithinDay);

        if (!gm.IsDaytime || gm.currentDay != dayAtStart)
        {
            infectionRoutineRunning = false;
            yield break;
        }

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

    private void InfectMariner(MarinerAI mariner)
    {
        if (mariner == null) return;

        Debug.Log($"감염 발생: 승무원 {mariner.marinerId}");

        int id = mariner.marinerId;
        var go = mariner.gameObject;

        mariner.enabled = false;
        mariner.StopAllCoroutines();

        var agent = go.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        var infected = go.GetComponent<InfectedMarinerAI>();
        if (infected == null)
            infected = go.AddComponent<InfectedMarinerAI>();

        infected.marinerId = id;

        Destroy(mariner);

        Debug.Log($"승무원 {id}: 감염 상태로 전환 완료 (InfectedMarinerAI 활성화)");
    }

    // ===== 승무원 등록 =====
    public void RegisterMariner(MarinerAI mariner)
    {
        if (!allMariners.Contains(mariner))
            allMariners.Add(mariner);
    }

    // 수리 대상 스캔
    public void UpdateRepairTargets()
    {
        repairTargets.Clear();

        // StructureBase를 전부 스캔
        StructureBase[] structures = FindObjectsOfType<StructureBase>();

        foreach (var obj in structures)
        {
            if (obj == null) continue;
            if (obj.IsDead) continue;

            // CommonBase 기반 체력 접근
            if (obj.CurrentHp < obj.maxHp * 0.5f)
            {
                repairTargets.Add(obj);
                Debug.Log($"수리 대상 추가: {obj.name} / HP: {obj.CurrentHp}/{obj.maxHp}");
            }
        }
    }

    // 수리 필요 목록 반환 
    public List<StructureBase> GetNeedsRepair()
    {
        List<StructureBase> needRepair = new List<StructureBase>();
        foreach (var obj in repairTargets)
        {
            if (obj == null) continue;
            if (obj.IsDead) continue;

            if (obj.CurrentHp < obj.maxHp * 0.5f)
                needRepair.Add(obj);
        }
        return needRepair;
    }

    // 수리 가능 체크
    public bool CanMarinerRepair(int marinerId, StructureBase target)
    {
        if (target == null || target.IsDead) return false;
        // 필요하면 여기서 타입/거리/권한 등 추가 조건
        return true;
    }

    // 점유 관리 (StructureBase)
    public bool IsRepairObjectOccupied(StructureBase obj)
    {
        if (obj == null) return false;
        return repairOccupancy.ContainsKey(obj.GetInstanceID());
    }

    public bool TryOccupyRepairObject(StructureBase obj, int marinerId)
    {
        if (obj == null) return false;

        int id = obj.GetInstanceID();
        if (!repairOccupancy.ContainsKey(id))
        {
            repairOccupancy[id] = marinerId;
            return true;
        }
        return false;
    }

    public void ReleaseRepairObject(StructureBase obj)
    {
        if (obj == null) return;

        int id = obj.GetInstanceID();
        if (repairOccupancy.ContainsKey(id))
            repairOccupancy.Remove(id);
    }
}
