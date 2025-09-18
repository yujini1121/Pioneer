using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 임시로 만든 Stats
public class EnemyStats
{
    public float baseHP = 100f;
    public float baseATK = 10f;
    public float hp, atk;

    void Awake() { hp = baseHP; atk = baseATK; }

    public void ApplyScaling(float atkMul, float hpMul)
    {
        atk = baseATK * atkMul;
        hp = baseHP * hpMul;
    }
}

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
    public float dayDuration = 270f;
    public float nightDuration = 90f;
    public float oneDayDuration = 360f;

    private ColorAdjustments colorAdjustments;
    private float cycleTime = 0f;

    [Header("스포너 지점")]
    public GameObject[] spawnPoints;

    [Header("승무원 스프라이트 지정")]
    public Sprite[] marinerSprites;

    [Header("에너미 프리팹")]
    public GameObject minion;   // 미니언
    public GameObject crawler;  // 크롤러
    public GameObject titan;    // 타이탄

    [Header("게임오버 관리")]
    public int totalCrewMembers = 0;
    public int deadCrewMembers = 0;
    public GameOverUI gameOverUI;
    public Canvas[] allUICanvas;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    [System.Serializable]
    public struct DayEnemyRow
    {
        public int total;   // 총 출현 수(참고용)
        public int minion;  // 미니언 수
        public int crawler; // 크롤러 수
        public int titan;   // 타이탄 수
    }

    [System.Serializable]
    public struct EnemyScaleRow
    {
        [Range(0, 200)] public float attackPercent; // 공격력 증가 %
        [Range(0, 200)] public float hpPercent;     // 체력 증가 %
    }

    [Header("일차별 에너미 출현 표 (1~5일차)")]
    public DayEnemyRow[] enemySpawnTable = new DayEnemyRow[5];

    [Header("일차별 능력치 강화 표 (1~5일차)")]
    public EnemyScaleRow[] enemyScaleTable = new EnemyScaleRow[5];

    private int currentDay = 1; // 1일차 시작

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        postProcessVolume.profile.TryGet(out colorAdjustments);
    }

    private void Start()
    {
        Debug.Log($">> GameManager.Start()");

        if (InventoryUiMain.instance != null)
            InventoryUiMain.instance.Start();
        else
            Debug.Log($">> GameManager.Start() : InventoryUiMain 인스턴스가 없음");
    }

    private void Update()
    {
        if (Time.timeScale > 0)
        {
            currentGameTime += Time.deltaTime;
            cycleTime += Time.deltaTime;
        }

        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        float t;

        if (IsDaytime)
        {
            t = Mathf.Clamp01(cycleTime / dayDuration);
            colorAdjustments.colorFilter.value =
                Color.Lerp(dayToNightGradient.Evaluate(0f), dayToNightGradient.Evaluate(1f), t);

            // 낮 종료 → 밤 시작
            if (cycleTime >= dayDuration)
            {
                IsDaytime = false;
                cycleTime = 0f;
                Debug.Log($"밤이 되었습니다. (Day {currentDay})");
                OnNightStart();
            }
        }
        else
        {
            t = Mathf.Clamp01(cycleTime / nightDuration);
            colorAdjustments.colorFilter.value =
                Color.Lerp(nightToDayGradient.Evaluate(0f), nightToDayGradient.Evaluate(1f), t);

            // 밤 종료 → 아침 시작
            if (cycleTime >= nightDuration)
            {
                IsDaytime = true;
                cycleTime = 0f;
                Debug.Log($"아침이 되었습니다. (Day {currentDay})");
                OnNightEnd();
                currentDay++; // 밤이 끝나면 다음 일차로 넘어감
            }
        }

        colorAdjustments.postExposure.value = exposureCurve.Evaluate(t);
    }

    private void OnNightStart()
    {
        SpawnEnemiesForCurrentDay();
    }

    private void OnNightEnd()
    {
        DespawnAllEnemies();
        ApplyCrewEmbarkRule(); 
    }

    public void GetGameTimeInfo(out int days, out int hours)
    {
        days = Mathf.FloorToInt(currentGameTime / oneDayDuration);
        float remainingTime = currentGameTime % oneDayDuration;
        hours = Mathf.FloorToInt((remainingTime / oneDayDuration) * 24f);
    }

    public void AddCrewMember() { totalCrewMembers++; }
    public void MarinerDiedCount() { deadCrewMembers++; }

    public void TriggerGameOver()
    {
        Time.timeScale = 0f;

        if (ThisIsPlayer.Player != null)
        {
            Renderer playerRenderer = ThisIsPlayer.Player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                Color color = playerRenderer.material.color;
                color.a = 0f;
                playerRenderer.material.color = color;
            }
        }

        HideAllUI();

        if (gameOverUI != null)
            gameOverUI.ShowGameOverScreen(totalCrewMembers, deadCrewMembers);
    }

    void HideAllUI()
    {
        foreach (Canvas canvas in allUICanvas)
        {
            if (canvas != null && canvas != gameOverUI.GetComponent<Canvas>())
                canvas.gameObject.SetActive(false);
        }
    }

    private void SpawnEnemiesForCurrentDay()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        DayEnemyRow row = GetSpawnRowForDay(currentDay);
        EnemyScaleRow scale = GetScaleRowForDay(currentDay);

        // 미니언
        SpawnOf(minion, row.minion, scale);
        // 크롤러
        SpawnOf(crawler, row.crawler, scale);
        // 타이탄
        SpawnOf(titan, row.titan, scale);

        int spawnedCount = row.minion + row.crawler + row.titan;
        Debug.Log($"[Spawn] Day {currentDay}: Minion {row.minion}, Crawler {row.crawler}, Titan {row.titan} (총 {spawnedCount})");
    }

    private void SpawnOf(GameObject prefab, int count, EnemyScaleRow scale)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            int sp = Random.Range(0, spawnPoints.Length);
            Transform p = spawnPoints[sp].transform;
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

            GameObject e = Instantiate(prefab, p.position + offset, Quaternion.identity);
            spawnedEnemies.Add(e);


            if (e.TryGetComponent(out EnemyStats stats))
            {
                float atkMul = 1f + (scale.attackPercent * 0.01f);
                float hpMul = 1f + (scale.hpPercent * 0.01f);
                stats.ApplyScaling(atkMul, hpMul);
            }
        }
    }

    private void DespawnAllEnemies()
    {
        foreach (GameObject e in spawnedEnemies)
            if (e != null) Destroy(e);

        spawnedEnemies.Clear();
        Debug.Log("[Despawn] 밤 종료로 모든 에너미 제거");
    }

    private DayEnemyRow GetSpawnRowForDay(int day)
    {
        if (enemySpawnTable != null && enemySpawnTable.Length > 0)
        {
            int idx = Mathf.Clamp(day - 1, 0, enemySpawnTable.Length - 1);
            return enemySpawnTable[idx];
        }
        return new DayEnemyRow { total = 0, minion = Random.Range(2, 8), crawler = 0, titan = 0 };
    }

    private EnemyScaleRow GetScaleRowForDay(int day)
    {
        if (enemyScaleTable != null && enemyScaleTable.Length > 0)
        {
            int idx = Mathf.Clamp(day - 1, 0, enemyScaleTable.Length - 1);
            return enemyScaleTable[idx];
        }
        return new EnemyScaleRow { attackPercent = 0, hpPercent = 0 };
    }

    private void ApplyCrewEmbarkRule()
    {
        int add = CalcCrewEmbarkCount(currentDay, totalCrewMembers);
        for (int i = 0; i < add; i++) AddCrewMember();

        if (add > 0)
            Debug.Log($"[Crew] Day {currentDay} 아침: 승선 {add}명 → 총 {totalCrewMembers}명");
    }

    // 1일차 0명, 2일차 1명, 3일차 2명, 4일차 3명,
    // 5일차: 현재 승무원 수 ≤3 → 4명, ≥4 → 5명
    private int CalcCrewEmbarkCount(int day, int crewNow)
    {
        switch (Mathf.Clamp(day, 1, 5))
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 3;
            case 5: return (crewNow <= 3) ? 4 : 5;
            default:
                // 6일차 이상은 마지막 값을 유지하거나, 필요 시 규칙 확장
                return (crewNow <= 3) ? 4 : 5;
        }
    }

    public float TimeUntilNight()
    {
        if (IsDaytime) return Mathf.Max(0f, dayDuration - cycleTime);
        else return 0f;
    }

    public void CollectResource(string type)
    {
        Debug.Log($"자원 획득: {type}");
    }
}
