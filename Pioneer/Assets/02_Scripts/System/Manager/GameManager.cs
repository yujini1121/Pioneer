using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#region 임시 Stats
public class EnemyStats : MonoBehaviour
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
#endregion

public class GameManager : MonoBehaviour, IBegin
{
    public static GameManager Instance;

    [Header("시간 설정")]
    public float currentGameTime = 0f;

    [Header("낮밤 체크 및 일차수 확인")]
    public bool IsDaytime = true;
    public int currentDay = 1; // 1일차 시작

    [Header("낮밤 순환 설정")]
    public Volume postProcessVolume;
    public Gradient dayToNightGradient;
    public Gradient nightToDayGradient;
    public AnimationCurve exposureCurve;
    public float dayDuration = 270f;
    public float nightDuration = 90f;
    private float oneDayDuration;

    private ColorAdjustments colorAdjustments;
    private float cycleTime = 0f;

    [Header("스포너 지점")]
    public GameObject[] spawnPoints;

    [Header("승무원 스프라이트 지정")]
    public Sprite[] marinerSprites;

    [Header("에너미 프리팹")]
    public GameObject minion;
    public GameObject crawler;
    public GameObject titan;

    [Header("게임오버 관리")]
    public int totalMarinerMembers = 0;
    public int deadMarinerMembers = 0;
    public GameOverUI gameOverUI;
    public Canvas[] allUICanvas;

    [Header("동적 스포너(EnemySpawnerFinder 연동)")]
    [SerializeField] private EnemySpawnerFinder spawnerFinder;          // Inspector에서 할당
    [SerializeField] private float spawnLiftY = 0.05f;                   // 살짝 띄워서 스폰
    [SerializeField] private string spawnRootName = "__SPAWNPOINTS__";   // 하이어라키 정리용
    private Transform spawnRoot;                                         // 스폰 포인트 부모

    // EnemySpawnerFinder에서 찾은 스폰 포인트 수
    private int activeSpawnCount = 0;

    // 생성된 에너미 리스트
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Transform enemyRoot;

    [System.Serializable]
    public struct DayEnemyRow
    {
        [Tooltip("총 출현 수 = 미니언 + 크룰러 + 타이탄")]
        public int total;   // 총 출현수
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

    [Header("승무원 스폰")]
    [SerializeField] private GameObject marinerPrefab;   
    [SerializeField] private Transform mast;            
    [SerializeField] private Vector3 marinerSpawnOffset = Vector3.zero;

    [Header("전체 둥지 개수 체크")]
    public int checkTotalNest;

    #region 임시 정리 
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        postProcessVolume.profile.TryGet(out colorAdjustments);
    }

    private void Start()
    {
        oneDayDuration = dayDuration + nightDuration;

        Debug.Log($">> GameManager.Start()");

        AudioManager.instance.PlayBgm(AudioManager.BGM.Morning);

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
        // 현재 페이즈(낮/밤)에 맞는 설정을 한 번에 가져옴
        bool isDay = IsDaytime;
        float duration = isDay ? dayDuration : nightDuration;
        Gradient grad = isDay ? dayToNightGradient : nightToDayGradient;

        // 진행도 0~1f
        float t = Mathf.Clamp01(cycleTime / duration);

        // 컬러/노출 보정
        colorAdjustments.colorFilter.value = grad.Evaluate(t);
        colorAdjustments.postExposure.value = exposureCurve.Evaluate(t);

        // 아직 페이즈가 끝나지 않았으면 리턴
        if (cycleTime < duration) return;

        // 페이즈 종료 처리
        cycleTime = 0f;

        if (isDay)
        {
            // 낮 -> 밤 전환
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.ToNight);

            AudioManager.instance.PlayBgm(AudioManager.BGM.Night);

            Debug.Log($"밤이 되었습니다. (Day {currentDay})");
            IsDaytime = false;
            OnNightStart();
        }
        else
        {
            // 밤 -> 낮 전환
            IsDaytime = true;
            currentDay++;

            AudioManager.instance.PlayBgm(AudioManager.BGM.Morning);

            OnNightEnd();
            Debug.Log($"아침이 되었습니다. (Day {currentDay})");
        }
    }

    private void OnNightStart()
    {
        RefreshSpawnPointsFromFinder();

        var s = GetScaleRowForDay(currentDay);
        Debug.Log($"[ScaleTable] Day {currentDay} -> ATK +{s.attackPercent}%, HP +{s.hpPercent}%");
        SpawnEnemiesForCurrentDay();
    }

    private void OnNightEnd()
    {
        if(currentDay >= 6)
        {
            gameOverUI.ShowGameClearScreen(totalMarinerMembers, deadMarinerMembers);
        }
        DespawnAllEnemies();
        ApplyMarinerEmbarkRule();
    }

    public void GetGameTimeInfo(out int days, out int hours)
    {
        days = Mathf.FloorToInt(currentGameTime / oneDayDuration);
        float remainingTime = currentGameTime % oneDayDuration;
        hours = Mathf.FloorToInt((remainingTime / oneDayDuration) * 24f);
    }

    public void AddMarinerMember() { totalMarinerMembers++; }
    public void MarinerDiedCount() { deadMarinerMembers++; }

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
            gameOverUI.ShowGameOverScreen(totalMarinerMembers, deadMarinerMembers);
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
        Debug.Log("Spawn Enemies");

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.MeetEnemy);

        DayEnemyRow row = GetSpawnRowForDay(currentDay);
        EnemyScaleRow scale = GetScaleRowForDay(currentDay);

        SpawnOf(minion, row.minion, scale);     // 미니언
        SpawnOf(crawler, row.crawler, scale);   // 크롤러
        SpawnOf(titan, row.titan, scale);       // 타이탄

        int spawnedCount = row.minion + row.crawler + row.titan;
        Debug.Log($"[Spawn] Day {currentDay}: Minion {row.minion}, Crawler {row.crawler}, Titan {row.titan} (총 {spawnedCount})");
    }

    // 일차별 공격력 적용된 에너미 생성
    private void SpawnOf(GameObject prefab, int count, EnemyScaleRow scale)
    {
        DayEnemyRow row = GetSpawnRowForDay(currentDay);
        Debug.Log($"[Table] Day{currentDay} -> M:{row.minion}, C:{row.crawler}, T:{row.titan}");

        if (prefab == null || count <= 0) return;

        // 부모 컨테이너 
        EnsureEnemyRoot();

        for (int i = 0; i < count; i++)
        {
            if (spawnPoints == null || activeSpawnCount == 0) { Debug.LogWarning("[Spawn] 활성 스폰 포인트 없음"); return; }

            // 활성화 된 것만 대상으로 랜덤(Found=true인 인덱스 선택)
            int spIndex = -1;
            for (int safe = 0; safe < 16; safe++)
            {
                int tryIdx = Random.Range(0, Mathf.Max(4, spawnPoints.Length));
                if (tryIdx < spawnPoints.Length && spawnPoints[tryIdx] != null && spawnPoints[tryIdx].activeSelf)
                {
                    spIndex = tryIdx; break;
                }
            }
            if (spIndex == -1) { Debug.LogWarning("[Spawn] 활성 스폰 포인트 선택 실패"); return; }

            Transform p = spawnPoints[spIndex].transform;
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

            GameObject e = Instantiate(prefab, p.position + offset, Quaternion.identity);

            if (enemyRoot != null) e.transform.SetParent(enemyRoot);
            e.name = $"{prefab.name}_Day{currentDay}_#{i + 1}";

            spawnedEnemies.Add(e);

            if (e.TryGetComponent(out EnemyStats stats))
            {
                float atkMul = 1f + (scale.attackPercent * 0.01f);
                float hpMul = 1f + (scale.hpPercent * 0.01f);
                stats.ApplyScaling(atkMul, hpMul);

                Debug.Log($"[Scale] Day {currentDay} {e.name} ATK {stats.baseATK}→{stats.atk} (x{atkMul:0.00}), HP {stats.baseHP}→{stats.hp} (x{hpMul:0.00})");
            }
        }
    }

    private void DespawnAllEnemies()
    {
		/*
        foreach (GameObject e in spawnedEnemies)
            if (e != null) Destroy(e);

        spawnedEnemies.Clear();*/
		Debug.Log($"DespawnAllEnemies 들어옴 / {GameObject.FindGameObjectsWithTag("Enemy").Length}");

		foreach (GameObject one in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Debug.Log($"DespawnAllEnemies : {one.name}");
            Destroy(one);
			Debug.Log($"DespawnAllEnemies one 삭제");
		}


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

    // ==========================
    // 아침 승무원 스폰 규칙 적용
    // ==========================
    private void ApplyMarinerEmbarkRule()
    {
        int add = CalcMarinerEmbarkCount(currentDay, totalMarinerMembers);
        if (add <= 0)
        {
            Debug.Log($"[Mariner] Day {currentDay} 아침: 승선 0명 → 총 {totalMarinerMembers}명");
            return;
        }

        SpawnMariner(add);
        Debug.Log($"[Mariner] Day {currentDay} 아침: 승선 {add}명 → 총 {totalMarinerMembers}명");
    }

    // 1일차 0명, 2일차 1명, 3일차 2명, 4일차 3명,
    // 5일차: 현재 승무원 수 ≤3 → 4명, 현재 승무원 수 ≥4 → 5명
    private int CalcMarinerEmbarkCount(int day, int marinerNow)
    {
        switch (Mathf.Clamp(day, 1, 5))
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 3;
            case 5: return (marinerNow <= 3) ? 4 : 5;
            default:
                // 6일차 이상은 마지막 값을 유지하거나, 필요 시 규칙 확장
                return (marinerNow <= 3) ? 4 : 5;
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
    #endregion

    private void EnsureSpawnRoot()
    {
        if (spawnRoot == null)
        {
            var go = GameObject.Find(spawnRootName);
            spawnRoot = (go != null) ? go.transform : new GameObject(spawnRootName).transform;
        }
    }

    private GameObject CreateOrGetSpawnPoint(int index)
    {
        // 기존 public GameObject[] spawnPoints 를 그대로 사용
        if (spawnPoints == null || spawnPoints.Length < 4)
        {
            // 길이가 4가 아니면 4로 맞춰 재할당(기존 값은 유지 불가 → 새로 채움)
            spawnPoints = new GameObject[4];
        }

        if (spawnPoints[index] == null)
        {
            EnsureSpawnRoot();
            var go = new GameObject($"SP_{index}"); // 임시 큐브 대신 빈 오브젝트로 관리
            go.transform.SetParent(spawnRoot);
            spawnPoints[index] = go;
        }
        return spawnPoints[index];
    }

    /// <summary>
    /// EnemySpawnerFinder의 4방향 결과를 읽어와 spawnPoints를 ‘현재 플랫폼 상태’로 동기화.
    /// 4개 전부 성공하면 true, 일부만 있으면 false(있는 것만 활성).
    /// </summary>
    private bool RefreshSpawnPointsFromFinder()
    {
        if (spawnerFinder == null)
        {
            Debug.LogWarning("[Spawner] EnemySpawnerFinder가 할당되지 않았습니다.");
            activeSpawnCount = 0;
            return false;
        }

        // 최신 플랫폼 배치 반영
        bool ok = spawnerFinder.Refresh();

        // Finder에서 찾은 방향들만 반영(최대 4)
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (!spawnerFinder.found[i]) continue;

            var sp = CreateOrGetSpawnPoint(i);
            Vector3 pos = spawnerFinder.resultPos[i];
            pos.y += spawnLiftY;
            sp.transform.position = pos;
            count++;
        }

        // 못 찾은 방향은 null 처리(스폰 대상에서 제외)
        for (int i = 0; i < 4; i++)
        {
            if (!spawnerFinder.found[i] && spawnPoints != null && i < spawnPoints.Length)
            {
                // 굳이 삭제까진 안 해도 되지만, 실수 스폰 방지용으로 비활성화 가능
                if (spawnPoints[i] != null) spawnPoints[i].SetActive(false);
            }
            else if (spawnerFinder.found[i] && spawnPoints[i] != null)
            {
                spawnPoints[i].SetActive(true);
            }
        }

        activeSpawnCount = count;
        if (count == 0)
            Debug.LogWarning("[Spawner] 사용 가능한 동적 스폰 포인트가 없습니다. 플랫폼을 설치하세요.");

        // 4개 모두 채워졌는지 반환
        return ok;
    }

    // 어디서든 호출 가능: 플랫폼 배치 변경 후 스폰 포인트 즉시 갱신
    public void NotifyPlatformLayoutChanged()
    {
        RefreshSpawnPointsFromFinder();
    }

    #region 하이어라키창에서 보기 쉽게 정리 (부모 보장)
    private void EnsureEnemyRoot()
    {
        if (enemyRoot == null)
        {
            var go = GameObject.Find("__ENEMIES__");
            enemyRoot = (go != null) ? go.transform : new GameObject("__ENEMIES__").transform;
        }
    }
    #endregion

    // ==========================
    // 실제 승무원 생성 로직
    // ==========================
    private void SpawnMariner(int count)
    {
        if (count <= 0) return;

        if (marinerPrefab == null)
        {
            Debug.LogWarning("[Mariner] marinerPrefab이 비어 있습니다. 프리팹을 할당하세요.");
            return;
        }

        // 스폰 기준 위치 = mast
        Vector3 basePos = Vector3.zero;
        if (mast != null) basePos = mast.position;
        else if (ThisIsPlayer.Player != null) basePos = ThisIsPlayer.Player.transform.position; 

        for (int i = 0; i < count; i++)
        {
            // 겹침 방지
            Vector3 jitter = new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
            Vector3 pos = basePos + marinerSpawnOffset + jitter;

            var go = Instantiate(marinerPrefab, pos, Quaternion.identity);
            go.name = $"Mariner_Day{currentDay}_#{totalMarinerMembers + 1}";

            // 카운트 반영
            AddMarinerMember();
        }
    }

    // 전체 둥지 수 제한
    public bool LimitsNest()
    {
        if (checkTotalNest < 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
