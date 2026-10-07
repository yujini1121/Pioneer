using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;

#region 임시 능력치
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

    [Header("밸런스 설정")]
    [SerializeField] private GameBalanceSettings balanceSettings;
    [SerializeField] private bool loadDefaultBalanceSettings = true;

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
    public float dayDuration = 150f;
    public float nightDuration = 50f;
    [Tooltip("해질녘/새벽에 밝기가 전환되는 시간 (초)")]
    [SerializeField, Min(0.1f)] private float lightingTransitionDuration = 8f;
    private float oneDayDuration;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private Coroutine gameResultPresentationCoroutine;
    private readonly Dictionary<Canvas, bool> canvasStatesBeforeResult = new Dictionary<Canvas, bool>();
    private CinemachineBrain resultCameraBrain;
    private Camera resultCamera;
    private Vector3 resultCameraStartPosition;
    private bool restoreCameraBrain;
    private float cycleTime = 0f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public float LightingCycleTime => cycleTime;
    public float LightingTransitionDuration => Mathf.Min(GetPhaseDuration(), Mathf.Max(0.1f, lightingTransitionDuration));
    public float IntendedNightBlend { get; private set; }
    public Color IntendedColorFilter { get; private set; }
    public float IntendedPostExposure { get; private set; }
    public ColorAdjustments RuntimeColorAdjustments => colorAdjustments;
#endif
    public float CurrentPhaseDuration => GetPhaseDuration();
    public float CurrentPhaseRemaining => Mathf.Max(0f, CurrentPhaseDuration - cycleTime);
    public float CurrentPhaseProgress => Mathf.Clamp01(cycleTime / CurrentPhaseDuration);

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
    public bool IsGameResultActive { get; private set; }

    [Header("게임 결과 연출")]
    [SerializeField] private float gameOverPresentationDuration = 1.8f;
    [SerializeField] private float gameOverAudioFadeDuration = 1.6f;
    [SerializeField] private float gameOverCameraZoomDistance = 1.8f;
    [SerializeField] private float gameOverCameraShakeAmount = 0.08f;
    [SerializeField] private float gameOverVignetteIntensity = 0.55f;
    [SerializeField] private float gameOverVignetteSmoothness = 0.6f;

    [Header("동적 스폰 지점")]
    [SerializeField] private EnemySpawnerFinder spawnerFinder;
    [SerializeField] private float spawnLiftY = 0.05f;                   // 살짝 띄워서 스폰
    [SerializeField] private string spawnRootName = "__SPAWNPOINTS__";   // 하이어라키 정리용
    private Transform spawnRoot;                                         // 스폰 포인트 부모

    [Header("바다이벤트")]
    [SerializeField] private OceanEventManager oceanEventManager;

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
    public DayEnemyRow[] enemySpawnTable =
    {
        new DayEnemyRow { total = 3, minion = 3, crawler = 0, titan = 0 },
        new DayEnemyRow { total = 5, minion = 4, crawler = 1, titan = 0 },
        new DayEnemyRow { total = 8, minion = 5, crawler = 2, titan = 1 },
        new DayEnemyRow { total = 9, minion = 6, crawler = 2, titan = 1 },
        new DayEnemyRow { total = 12, minion = 7, crawler = 3, titan = 2 },
    };

    [Header("일차별 능력치 강화 표 (1~5일차)")]
    public EnemyScaleRow[] enemyScaleTable =
    {
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 10f },
        new EnemyScaleRow { attackPercent = 10f, hpPercent = 15f },
        new EnemyScaleRow { attackPercent = 20f, hpPercent = 25f },
    };

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
        else { Destroy(gameObject); return; }

        LoadDefaultBalanceSettingsIfNeeded();
        ApplyBalanceSettings();

        if (postProcessVolume != null && postProcessVolume.profile != null)
            postProcessVolume.profile.TryGet(out colorAdjustments);

        if (postProcessVolume != null && postProcessVolume.profile != null)
            postProcessVolume.profile.TryGet(out vignette);
    }

    private DayUI dayUI;
    public bool HasMorningBriefingUI => dayUI != null && dayUI.isActiveAndEnabled && dayUI.HasMorningBriefing;

    private void PresentMorning()
    {
        AudioManager.instance?.PlaySfx(AudioManager.SFX.MorningBell);
        if (dayUI != null && oceanEventManager != null)
            dayUI.ShowMorningBriefing(currentDay, oceanEventManager.currentEvent);
    }

    private void Start()
    {
        oneDayDuration = dayDuration + nightDuration;
        UpdateDayNightLighting();
        dayUI = FindObjectOfType<DayUI>(true);
        if (oceanEventManager == null) oceanEventManager = OceanEventManager.instance;
        PresentMorning();


        if (AudioManager.instance != null)
            AudioManager.instance.PlayBgm(AudioManager.BGM.Morning);

        if (InventoryUiMain.instance != null)
            InventoryUiMain.instance.Start();
        else
            ;
    }

    private void Update()
    {
        if (IsGameResultActive || Time.timeScale <= 0f) return;
        currentGameTime += Time.deltaTime;
        cycleTime += Time.deltaTime;

        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        while (cycleTime >= GetPhaseDuration())
        {
            cycleTime -= GetPhaseDuration();
            if (IsDaytime)
            {
                // 낮 -> 밤 전환
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(AudioManager.SFX.NightBell);

                if (AudioManager.instance != null)
                    AudioManager.instance.PlayBgm(AudioManager.BGM.Night);

                IsDaytime = false;
                InGameUI.instance?.ShowActionFeedback("밤이 찾아왔습니다. 배를 지키세요.", 1);
                OnNightStart();
            }
            else
            {
                // 밤 -> 낮 전환
                IsDaytime = true;
                currentDay++;

                if (AudioManager.instance != null)
                    AudioManager.instance.PlayBgm(AudioManager.BGM.Morning);

                OnNightEnd();

                // 일반 모드일 때만 6일차 엔딩 발생
                if (!GameModeState.IsInfiniteMode && currentDay >= 6)
                {
                    UpdateDayNightLighting();
                    TriggerGameClear();
                    return;
                }
            }
        }
        UpdateDayNightLighting();
    }

    private float GetPhaseDuration()
    {
        return Mathf.Max(0.01f, IsDaytime ? dayDuration : nightDuration);
    }

    private void UpdateDayNightLighting()
    {
        if (colorAdjustments == null) return;

        float duration = GetPhaseDuration();
        float transition = Mathf.Min(duration, Mathf.Max(0.1f, lightingTransitionDuration));
        float progress = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((cycleTime - (duration - transition)) / transition));
        float nightBlend = IsDaytime ? progress : 1f - progress;

        Gradient gradient = dayToNightGradient;
        Color filter = gradient != null ? gradient.Evaluate(nightBlend)
            : nightToDayGradient != null ? nightToDayGradient.Evaluate(1f - nightBlend) : Color.white;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = filter;
        colorAdjustments.postExposure.overrideState = true;
        if (exposureCurve != null)
            colorAdjustments.postExposure.value = exposureCurve.Evaluate(1f - nightBlend);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        IntendedNightBlend = nightBlend;
        IntendedColorFilter = filter;
        IntendedPostExposure = colorAdjustments.postExposure.value;
#endif
    }

    private void OnNightStart()
    {
        if (oceanEventManager != null)
            oceanEventManager.EnterNight();

        RefreshSpawnPointsFromFinder();

        var s = GetScaleRowForDay(currentDay);
        SpawnEnemiesForCurrentDay();
    }

    private void OnNightEnd()
    {
        if (oceanEventManager != null)
            oceanEventManager.EnterDay();

        if (GameModeState.IsInfiniteMode || currentDay < 6) PresentMorning();
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
        TriggerGameResult(false);
    }

    public void TriggerGameClear()
    {
        TriggerGameResult(true);
    }

    private void TriggerGameResult(bool voyageSucceeded)
    {
        if (IsGameResultActive)
            return;

        IsGameResultActive = true;
        BuffUIManager.Instance?.ClearAll();

        if (PlayerController.instance != null)
            PlayerController.instance.LockForGameResult();

        if (PlayerCore.Instance != null)
        {
            PlayerCore.Instance.StopHorizontalMovement();

            if (!voyageSucceeded)
                PlayerCore.Instance.SetState(PlayerCore.PlayerState.Dead);
        }

        Time.timeScale = 0f;
        HideAllUI();

        if (gameResultPresentationCoroutine != null)
            StopCoroutine(gameResultPresentationCoroutine);

        if (voyageSucceeded)
        {
            ShowGameResultScreen(voyageSucceeded);
            return;
        }

        gameResultPresentationCoroutine = StartCoroutine(PlayGameOverPresentation(voyageSucceeded));
    }

    private IEnumerator PlayGameOverPresentation(bool voyageSucceeded)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.FadeOutForGameResult(gameOverAudioFadeDuration);

        Camera cam = Camera.main;
        resultCamera = cam;
        resultCameraBrain = cam != null ? cam.GetComponent<CinemachineBrain>() : null;
        restoreCameraBrain = resultCameraBrain != null && resultCameraBrain.enabled;
        if (restoreCameraBrain) resultCameraBrain.enabled = false;
        Vector3 startCameraPosition = cam != null ? cam.transform.position : Vector3.zero;
        resultCameraStartPosition = startCameraPosition;
        Vector3 targetCameraPosition = startCameraPosition;

        if (cam != null)
            targetCameraPosition = startCameraPosition + cam.transform.forward * gameOverCameraZoomDistance;

        float startVignetteIntensity = vignette != null ? vignette.intensity.value : 0f;
        float startVignetteSmoothness = vignette != null ? vignette.smoothness.value : 0f;
        bool hadVignette = vignette != null && vignette.active;

        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
        }

        float duration = Mathf.Max(0.01f, gameOverPresentationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = SmoothStep01(t);

            if (cam != null)
            {
                Vector2 shake = Random.insideUnitCircle * gameOverCameraShakeAmount * (1f - eased);
                Vector3 shakeOffset = cam.transform.right * shake.x + cam.transform.up * shake.y;
                cam.transform.position = Vector3.Lerp(startCameraPosition, targetCameraPosition, eased) + shakeOffset;
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, gameOverVignetteIntensity, eased);
                vignette.smoothness.value = Mathf.Lerp(startVignetteSmoothness, gameOverVignetteSmoothness, eased);
            }

            yield return null;
        }

        if (cam != null)
            cam.transform.position = targetCameraPosition;

        if (vignette != null)
        {
            vignette.intensity.value = gameOverVignetteIntensity;
            vignette.smoothness.value = gameOverVignetteSmoothness;
            vignette.active = hadVignette || gameOverVignetteIntensity > 0f;
        }

        ShowGameResultScreen(voyageSucceeded);
        gameResultPresentationCoroutine = null;
    }

    private void ShowGameResultScreen(bool voyageSucceeded)
    {
        if (gameOverUI != null)
            gameOverUI.ShowGameOverScreen(totalMarinerMembers, deadMarinerMembers, voyageSucceeded);
    }

    private static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void HideAllUI()
    {
        canvasStatesBeforeResult.Clear();
        if (allUICanvas == null) return;
        foreach (Canvas canvas in allUICanvas)
        {
            if (canvas == null || (gameOverUI != null && gameOverUI.transform.IsChildOf(canvas.transform)))
                continue;
            canvasStatesBeforeResult[canvas] = canvas.gameObject.activeSelf;
            canvas.gameObject.SetActive(false);
        }
    }

    void ShowAllUI()
    {
        foreach (var entry in canvasStatesBeforeResult)
        {
            if (entry.Key != null)
                entry.Key.gameObject.SetActive(entry.Value);
        }
        canvasStatesBeforeResult.Clear();
    }

    private void SpawnEnemiesForCurrentDay()
    {

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.meetEnemy2);

        DayEnemyRow row = GetSpawnRowForDay(currentDay);
        EnemyScaleRow scale = GetScaleRowForDay(currentDay);

        SpawnOf(minion, row.minion, scale);     // 미니언
        SpawnOf(crawler, row.crawler, scale);   // 크롤러
        SpawnOf(titan, row.titan, scale);       // 타이탄

        int spawnedCount = row.minion + row.crawler + row.titan;
    }

    // 바다이벤트 : 안개 낮 효과 -> 미니언 추가 스폰
    public void SpawnFogMinions(int count)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (minion == null || count <= 0) return;

        EnemyScaleRow scale = GetScaleRowForDay(currentDay);

        SpawnOf(minion, count, scale);
    }

    public void ResumeFromEndingToInfiniteMode()
    {
        if (gameResultPresentationCoroutine != null)
        {
            StopCoroutine(gameResultPresentationCoroutine);
            gameResultPresentationCoroutine = null;
        }
        RestoreResultCamera();
        Time.timeScale = 1f;
        IsGameResultActive = false;

        if (AudioManager.instance != null)
            AudioManager.instance.RestoreRuntimeVolumes();

        if (PlayerController.instance != null)
            PlayerController.instance.UnlockFromGameResult();

        if (PlayerCore.Instance != null)
            PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);

        // 플레이어 다시 보이게
        if (ThisIsPlayer.Player != null)
        {
            Renderer playerRenderer = ThisIsPlayer.Player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                Color color = playerRenderer.material.color;
                color.a = 1f;
                playerRenderer.material.color = color;
            }
        }

        ShowAllUI();

        // 게임오버 패널 닫기
        if (gameOverUI != null)
            gameOverUI.HideGameOverScreen();

        PresentMorning();

    }

    // 일차별 공격력 적용된 에너미 생성
    private void SpawnOf(GameObject prefab, int count, EnemyScaleRow scale)
    {
        DayEnemyRow row = GetSpawnRowForDay(currentDay);

        if (prefab == null || count <= 0) return;

        // 부모 컨테이너
        EnsureEnemyRoot();

        for (int i = 0; i < count; i++)
        {
            if (spawnPoints == null || activeSpawnCount == 0) { Debug.LogWarning("활성 스폰 지점이 없습니다."); return; }

            int spIndex = -1;
            for (int safe = 0; safe < 16; safe++)
            {
                int tryIdx = Random.Range(0, Mathf.Max(4, spawnPoints.Length));
                if (tryIdx < spawnPoints.Length && spawnPoints[tryIdx] != null && spawnPoints[tryIdx].activeSelf)
                {
                    spIndex = tryIdx; break;
                }
            }
            if (spIndex == -1) { Debug.LogWarning("활성 스폰 지점을 선택하지 못했습니다."); return; }

            Transform p = spawnPoints[spIndex].transform;
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

            GameObject e = Instantiate(prefab, p.position + offset, Quaternion.identity);

            UnitFadeController fade = EnsureUnitFadeController(e);
            if (fade != null)
                fade.PlaySpawnFade();

            if (enemyRoot != null) e.transform.SetParent(enemyRoot);
            e.name = $"{prefab.name}_Day{currentDay}_#{i + 1}";

            spawnedEnemies.Add(e);

            if (e.TryGetComponent(out EnemyBase enemy))
                enemy.SetSpawnScaling(scale.attackPercent, scale.hpPercent);
        }
    }

    private void DespawnAllEnemies()
    {

        foreach (GameObject one in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (one == null) continue;


            UnitFadeController fade = EnsureUnitFadeController(one);
            if (fade != null)
                fade.PlayDespawnFadeAndDestroy();
            else
                Destroy(one);
        }

        spawnedEnemies.Clear();

    }

    private DayEnemyRow GetSpawnRowForDay(int day)
    {
        if (enemySpawnTable == null || enemySpawnTable.Length == 0)
            return new DayEnemyRow { total = 0, minion = 0, crawler = 0, titan = 0 };

        // 1~5일차는 기존 표 그대로 사용
        if (day <= enemySpawnTable.Length)
        {
            int idx = Mathf.Clamp(day - 1, 0, enemySpawnTable.Length - 1);
            return enemySpawnTable[idx];
        }

        // 무한 모드가 아니면 마지막(5일차) 값 유지
        if (!GameModeState.IsInfiniteMode)
            return enemySpawnTable[enemySpawnTable.Length - 1];

        // 무한 모드 6일차 이상:
        // 5일차 값을 기준으로 매일 미니언/크롤러/타이탄 각각 +1
        DayEnemyRow baseRow = enemySpawnTable[enemySpawnTable.Length - 1];
        int extraDays = day - enemySpawnTable.Length; // 6일차=1, 7일차=2, ...

        DayEnemyRow result = new DayEnemyRow
        {
            minion = baseRow.minion + extraDays,
            crawler = baseRow.crawler + extraDays,
            titan = baseRow.titan + extraDays
        };

        result.total = result.minion + result.crawler + result.titan;
        return result;
    }

    public EnemyScaleRow GetScaleRowForDay(int day)
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
        int add = CalcMarinerEmbarkCount(currentDay, Mathf.Max(0, totalMarinerMembers - deadMarinerMembers));
        if (add <= 0)
        {
            return;
        }

        SpawnMariner(add);
    }

    // 1일차 0명, 2일차 1명, 3일차 2명, 4일차 3명,
    // 5일차: 현재 승무원 수 ≤3 → 4명, 현재 승무원 수 ≥4 → 5명
    private int CalcMarinerEmbarkCount(int day, int marinerNow)
    {
        if (marinerNow >= 5) return 0;
        switch (Mathf.Clamp(day, 1, 5))
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 1;
            case 4: return 1;
            case 5: return 1;
            default:
                // 6일차 이상은 마지막 값을 유지하거나, 필요 시 규칙 확장
                return 1;
        }
    }

    public float TimeUntilNight()
    {
        if (IsDaytime) return Mathf.Max(0f, dayDuration - cycleTime);
        else return 0f;
    }

    public void CollectResource(string type)
    {
        UtilityFunctions.Log($"자원 획득: {type}");
    }
    #endregion

    public void SetBalanceSettings(GameBalanceSettings settings, bool applyNow = true)
    {
        balanceSettings = settings;

        if (applyNow)
            ApplyBalanceSettings();
    }

    [ContextMenu("밸런스 설정 적용")]
    public void ApplyBalanceSettings()
    {
        if (balanceSettings == null)
            return;

        dayDuration = Mathf.Max(0.01f, balanceSettings.dayDuration);
        nightDuration = Mathf.Max(0.01f, balanceSettings.nightDuration);
        oneDayDuration = dayDuration + nightDuration;

        enemySpawnTable = ToGameManagerRows(balanceSettings.enemySpawnTable);
        enemyScaleTable = ToGameManagerRows(balanceSettings.enemyScaleTable);
    }

    public void CopyCurrentBalanceTo(GameBalanceSettings settings)
    {
        if (settings == null)
            return;

        settings.dayDuration = dayDuration;
        settings.nightDuration = nightDuration;
        settings.enemySpawnTable = ToSettingsRows(enemySpawnTable);
        settings.enemyScaleTable = ToSettingsRows(enemyScaleTable);
        settings.NormalizeTotals();
    }

    private void LoadDefaultBalanceSettingsIfNeeded()
    {
        if (!loadDefaultBalanceSettings || balanceSettings != null)
            return;

        balanceSettings = Resources.Load<GameBalanceSettings>(GameBalanceSettings.DefaultResourceName);
    }

    private static DayEnemyRow[] ToGameManagerRows(GameBalanceSettings.EnemySpawnRow[] source)
    {
        if (source == null)
            return null;

        DayEnemyRow[] result = new DayEnemyRow[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = new DayEnemyRow
            {
                total = source[i].total,
                minion = source[i].minion,
                crawler = source[i].crawler,
                titan = source[i].titan
            };
        }

        return result;
    }

    private static EnemyScaleRow[] ToGameManagerRows(GameBalanceSettings.EnemyScaleRow[] source)
    {
        if (source == null)
            return null;

        EnemyScaleRow[] result = new EnemyScaleRow[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = new EnemyScaleRow
            {
                attackPercent = source[i].attackPercent,
                hpPercent = source[i].hpPercent
            };
        }

        return result;
    }

    private static GameBalanceSettings.EnemySpawnRow[] ToSettingsRows(DayEnemyRow[] source)
    {
        if (source == null)
            return null;

        GameBalanceSettings.EnemySpawnRow[] result = new GameBalanceSettings.EnemySpawnRow[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = new GameBalanceSettings.EnemySpawnRow
            {
                total = source[i].total,
                minion = source[i].minion,
                crawler = source[i].crawler,
                titan = source[i].titan
            };
        }

        return result;
    }

    private static GameBalanceSettings.EnemyScaleRow[] ToSettingsRows(EnemyScaleRow[] source)
    {
        if (source == null)
            return null;

        GameBalanceSettings.EnemyScaleRow[] result = new GameBalanceSettings.EnemyScaleRow[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = new GameBalanceSettings.EnemyScaleRow
            {
                attackPercent = source[i].attackPercent,
                hpPercent = source[i].hpPercent
            };
        }

        return result;
    }

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

         private bool RefreshSpawnPointsFromFinder()
    {
        if (spawnerFinder == null)
        {
            Debug.LogWarning("동적 스폰 지점 탐색기가 연결되지 않았습니다.");
            activeSpawnCount = 0;
            return false;
        }

        // 최신 플랫폼 배치 반영
        bool ok = spawnerFinder.Refresh();

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
            Debug.LogWarning("사용 가능한 동적 스폰 지점이 없습니다. 플랫폼을 설치하세요.");

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
            Debug.LogWarning("승무원 프리팹이 연결되지 않았습니다.");
            return;
        }

        if (spawnPoints == null || activeSpawnCount == 0)
        {
            Debug.LogWarning("승무원의 활성 스폰 지점이 없습니다.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            int spIndex = -1;
            for (int safe = 0; safe < 16; safe++)
            {
                int tryIdx = Random.Range(0, Mathf.Max(4, spawnPoints.Length));
                if (tryIdx < spawnPoints.Length && spawnPoints[tryIdx] != null && spawnPoints[tryIdx].activeSelf)
                {
                    spIndex = tryIdx;
                    break;
                }
            }

            if (spIndex == -1)
            {
                Debug.LogWarning("승무원의 활성 스폰 지점을 선택하지 못했습니다.");
                return;
            }

            Transform p = spawnPoints[spIndex].transform;
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            Vector3 pos = p.position + marinerSpawnOffset + offset;

            var go = Instantiate(marinerPrefab, pos, Quaternion.identity);
            go.name = $"Mariner_Day{currentDay}_#{totalMarinerMembers + 1}";

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

    private UnitFadeController EnsureUnitFadeController(GameObject target)
    {
        if (target == null) return null;

        UnitFadeController fade = target.GetComponent<UnitFadeController>();
        if (fade == null)
            fade = target.AddComponent<UnitFadeController>();

        return fade;
    }

    private void RestoreResultCamera()
    {
        if (resultCamera != null) resultCamera.transform.position = resultCameraStartPosition;
        if (resultCameraBrain != null && restoreCameraBrain) resultCameraBrain.enabled = true;
        resultCamera = null;
        resultCameraBrain = null;
        restoreCameraBrain = false;
    }

    private void OnDestroy()
    {
        RestoreResultCamera();
        if (Instance == this) Instance = null;
    }
}
