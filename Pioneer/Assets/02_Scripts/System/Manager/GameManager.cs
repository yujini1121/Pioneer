using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour, IBegin
{
    public static GameManager Instance;

    [Header("시간 설정")]
    public float currentGameTime = 0f;
    public bool IsDaytime = true;

    [Header("낮밤 순환 설정")]
    public Volume postProcessVolume;
    public Gradient dayToNightGradient;
    public Gradient nightToDayGradient;
    public AnimationCurve exposureCurve;
    public float dayDuration = 120f;
    public float nightDuration = 60f;

    private ColorAdjustments colorAdjustments;
    private float cycleTime = 0f;
    private float fullCycleDuration;
    private bool transitioningToNight = true;

    [Header("스포너 지점")]
    public GameObject[] spawnPoints;

    private List<MarinerAI> allMariners = new List<MarinerAI>();
    private bool infectionStarted = false;
    public float infectionStartTime = 180f;
    public float infectionInterval = 10f;

    [Header("승무원 스프라이트 지정")]
    public Sprite[] marinerSprites;

    [Header("미니언 에너미")]
    public GameObject minion;

    private List<GameObject> spawnedMinions = new List<GameObject>();

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

        postProcessVolume.profile.TryGet(out colorAdjustments);
        fullCycleDuration = dayDuration + nightDuration;
    }

    private void Start()
    {
        // InfectedMarinerAI가 붙어 있는 모든 오브젝트 초기화
        foreach (var infected in FindObjectsOfType<InfectedMarinerAI>())
        {
            infected.Init();
        }

        // MarinerAI가 붙어 있는 모든 오브젝트 초기화
        foreach (var mariner in FindObjectsOfType<MarinerAI>())
        {
            mariner.Init();
        }

        // UnitBase가 붙어 있는 모든 오브젝트 초기화
        foreach (var unit in FindObjectsOfType<UnitBase>())
        {
            unit.Init();
        }    
    }

    private void Update()
    {
        currentGameTime += Time.deltaTime;
        UpdateDayNightCycle();

        if (!infectionStarted && currentGameTime >= infectionStartTime)
        {
            infectionStarted = true;
            StartCoroutine(InfectMarinersOneByOne());
        }
    }

    private void UpdateDayNightCycle()
    {
        cycleTime += Time.deltaTime;
        float t;

        if (IsDaytime)
        {
            t = Mathf.Clamp01(cycleTime / dayDuration);
            colorAdjustments.colorFilter.value = Color.Lerp(dayToNightGradient.Evaluate(0f), dayToNightGradient.Evaluate(1f), t);
        }
        else
        {
            t = Mathf.Clamp01(cycleTime / nightDuration);
            colorAdjustments.colorFilter.value = Color.Lerp(nightToDayGradient.Evaluate(0f), nightToDayGradient.Evaluate(1f), t);
        }

        colorAdjustments.postExposure.value = exposureCurve.Evaluate(t);

        if (IsDaytime && cycleTime >= dayDuration)
        {
            IsDaytime = false;
            cycleTime = 0f;
            Debug.Log("밤이 되었습니다.");
            SpawnMinions(); // 밤에 미니언 생성
        }
        else if (!IsDaytime && cycleTime >= nightDuration)
        {
            IsDaytime = true;
            cycleTime = 0f;
            Debug.Log("아침이 되었습니다.");
            DespawnMinions(); // 아침에 미니언 제거
        }
    }

    private void SpawnMinions()
    {
        if (spawnPoints.Length == 0 || minion == null) return;

        int spawnIndex = Random.Range(0, spawnPoints.Length);   // 1~12까지 스폰포인트 탐색
        Transform spawnTransform = spawnPoints[spawnIndex].transform;

        int count = Random.Range(2, 8); // 2~7마리
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            GameObject m = Instantiate(minion, spawnTransform.position + offset, Quaternion.identity);
            spawnedMinions.Add(m);
        }

        Debug.Log($"{count}마리 미니언이 스폰되었습니다.");
    }

    private void DespawnMinions()
    {
        foreach (GameObject m in spawnedMinions)
        {
            if (m != null) Destroy(m);
        }

        spawnedMinions.Clear();
        Debug.Log("아침이 되어 미니언이 제거되었습니다.");
    }

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

    private void InfectMariner(MarinerAI mariner)
    {
        if (mariner == null) return;

        Debug.Log("감염 발생");

        int id = mariner.marinerId;
        GameObject go = mariner.gameObject;

        Destroy(mariner);
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
        if (IsDaytime)
            return Mathf.Max(0f, dayDuration - cycleTime);
        else
            return 0f;
    }

    public void CollectResource(string type)
    {
        Debug.Log($"자원 획득: {type}");
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
            Debug.Log("보관함 없음 ");
            mariner.StartCoroutine(mariner.StartSecondPriorityAction());
        }
    }

    public bool HasStorage()
    {
        return true;
    }

    public bool IsSpawnerOccupied(int index)
    {
        return occupiedSpawners.Contains(index);
    }

    public void OccupySpawner(int index)
    {
        occupiedSpawners.Add(index);
    }

    public void ReleaseSpawner(int index)
    {
        occupiedSpawners.Remove(index);
    }

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
