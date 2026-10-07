#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cinemachine;
using DG.Tweening;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


[InitializeOnLoad]
public static class ReleaseValidation
{
    private const string ActiveKey = "Pioneer.ReleaseValidation.Active";
    private const string OwnerKey = "Pioneer.ReleaseValidation.Owner";
    private const string RestorePendingKey = "Pioneer.ReleaseValidation.RestorePending";
    private static readonly int EditorProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
    private static readonly DateTime EditorStartedUtc = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
    private const string StageKey = "Pioneer.ReleaseValidation.Stage";
    private const string ReportKey = "Pioneer.ReleaseValidation.Report";
    private const string SetupKey = "Pioneer.ReleaseValidation.Setup";
    private const string RequestPath = "Library/PioneerReleaseValidation.request";
    private const string ReportPath = "Logs/ReleaseValidation/editor-validation.json";
    private static double nextTick;
    private static double deadline;
    private static AudioClip probeClip;
    private static MarinerAI probeMariner;
    private static Vector3 cameraBeforeResult;
    private static Vector3 playerBeforeResult;
    private static UnityEngine.AI.NavMeshAgent navigationProbe;
    private static Vector3 navigationProbeStart;
    private static int ordinaryCatchBefore;

    [Serializable] private class Report
    {
        public string unityVersion;
        public string status;
        public List<string> passed = new List<string>();
        public List<string> errors = new List<string>();
        public List<string> referenceWarnings = new List<string>();
    }
    [Serializable] private class Setup { public SceneSetup[] scenes; }
    private static Report report;
    public static bool IsRunning => SessionState.GetBool(ActiveKey, false)
        && SessionState.GetInt(OwnerKey, -1) == EditorProcessId
        && EditorApplication.isPlayingOrWillChangePlaymode;

    static ReleaseValidation()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.quitting -= ClearActiveSession;
        EditorApplication.quitting += ClearActiveSession;
        EditorApplication.delayCall += CheckRequest;
        if (SessionState.GetBool(ActiveKey, false))
        {
            if (!IsRunning) { ClearActiveSession(); return; }
            report = JsonUtility.FromJson<Report>(SessionState.GetString(ReportKey, "{}"));
            if (report == null || report.status != "running") { ClearActiveSession(); return; }
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureLog;
            Application.logMessageReceived += CaptureLog;
        }
        if (SessionState.GetBool(RestorePendingKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    private static void ClearActiveSession()
    {
        SessionState.SetBool(ActiveKey, false);
        SessionState.EraseInt(OwnerKey);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        LastPolishValidation.Reset();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(ActiveKey, false))
        {
            if (SessionState.GetInt(OwnerKey, -1) == EditorProcessId && report != null)
            {
                if (SessionState.GetInt(StageKey, 0) != 100)
                    report.errors.Add("Validation cancelled when its Play session ended.");
                SessionState.SetBool(RestorePendingKey, true);
                SaveReport();
            }
            ClearActiveSession();
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestorePendingKey, false)) Finish();
    }

    private static void CheckRequest()
    {
        if (!File.Exists(RequestPath)) return;

        DateTime requestedUtc = File.GetLastWriteTimeUtc(RequestPath);
        bool fresh = requestedUtc >= EditorStartedUtc && DateTime.UtcNow - requestedUtc < TimeSpan.FromMinutes(5);
        string request = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        if (!fresh || SessionState.GetBool(ActiveKey, false)
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (request == "build") BuildWindows();
        else if (request == "run") Run();
    }

    [MenuItem("Tools/Pioneer/출시 검증/기본 검사")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            if (IsRunning) return;
            report = new Report { unityVersion = Application.unityVersion };
            report.status = "검증 전에 씬을 저장하고 플레이를 종료하세요.";
            SaveReport();
            if (Application.isBatchMode) EditorApplication.Exit(2);
            return;
        }
        ClearActiveSession();
        report = new Report { unityVersion = Application.unityVersion, status = "running" };
        try
        {
            SessionState.SetString(SetupKey, JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
            Application.logMessageReceived -= CaptureLog;
            Application.logMessageReceived += CaptureLog;
            AuditScenes();
            SessionState.SetInt("Pioneer.ReleaseValidation.Unlock", PlayerPrefs.GetInt("InfiniteModeUnlocked", -1));
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(OwnerKey, EditorProcessId);
            SessionState.SetInt(StageKey, 0);
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureLog;
            Application.logMessageReceived += CaptureLog;
            SaveReport();
            EditorSceneManager.OpenScene("Assets/01_Scenes/Title.unity");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error)
        {
            ClearActiveSession();
            report.errors.Add(error.ToString()); report.status = "failed"; SaveReport();
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    private static void AuditScenes()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Require(scenes.SequenceEqual(new[] { "Assets/01_Scenes/Title.unity", "Assets/01_Scenes/1015 Main.unity" }), string.Empty);
        foreach (string path in scenes)
        {
            Scene preview = SceneManager.GetSceneByPath(path);
            bool openedForAudit = !preview.IsValid() || !preview.isLoaded;
            if (openedForAudit) preview = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in preview.GetRootGameObjects())
                    AuditHierarchy(root, path);
            }
            finally { if (openedForAudit) EditorSceneManager.CloseScene(preview, true); }
        }
        foreach (string path in AssetDatabase.GetDependencies(scenes, true).Where(p => p.EndsWith(".prefab")))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) AuditHierarchy(prefab, path);
        }
        Require(!report.errors.Any(), string.Empty);
    }

    private static void AuditHierarchy(GameObject root, string path)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            if (missing > 0) report.errors.Add(path + " / " + child.name + ": 누락된 스크립트=" + missing);
            foreach (MonoBehaviour component in child.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                using (var serialized = new SerializedObject(component))
                {
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            report.referenceWarnings.Add(path + " / " + child.name + " / " + property.propertyPath);
                }
            }
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (!IsRunning) { ClearActiveSession(); return; }
        int stage = SessionState.GetInt(StageKey, 0);
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < nextTick) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("검증 제한 시간이 초과되었습니다. 단계: " + stage);
            switch (stage)
            {
                case 0:
                    Require(SceneManager.GetActiveScene().name == "Title", string.Empty);
                    Require(Object.FindObjectsOfType<AudioManager>().Length == 1, string.Empty);
                    GameModeState.StartNormalMode();
                    SceneController.Instance.LoadScene("1015 Main");
                    break;
                case 1:
                    if (SceneManager.GetActiveScene().name != "1015 Main" || (SceneController.Instance != null && SceneController.Instance.isLoading)) return;
                    break;
                case 2:
                    Require(GameManager.Instance != null && PlayerCore.Instance != null, string.Empty);
                    Require(CreatureEffect.Instance != null, string.Empty);
                    var playerAgent = PlayerCore.Instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    Require(playerAgent != null && playerAgent.isOnNavMesh, string.Empty);
                    var balance = Resources.Load<GameBalanceSettings>("GameBalanceSettings");
                    Require(balance != null && GameManager.Instance.dayDuration == balance.dayDuration && GameManager.Instance.nightDuration == balance.nightDuration, string.Empty);
                    Require(!GameModeState.IsInfiniteMode && GameManager.Instance.currentDay == 1, string.Empty);
                    int hp = PlayerCore.Instance.hp;
                    PlayerCore.Instance.TakeDamage(1, null);
                    Require(PlayerCore.Instance.hp == hp - 1, string.Empty);
                    InGameUI.instance.UseTab();
                    InGameUI.instance.UseTab();
                    InGameUI.instance.UseTab();
                    break;
                case 3:
                    Require(InGameUI.instance.IsPannelExpanded && InGameUI.instance.makeshiftCraftUI.activeSelf, string.Empty);
                    Require(InGameUI.instance.uiChunkStack.Select(c => c.id).Distinct().Count() == InGameUI.instance.uiChunkStack.Count, string.Empty);
                    InGameUI.instance.UseTab();
                    Option.instance.SetActivateEscUI();
                    LastPolishValidation.BeginPauseCheck();
                    CheckEscMenuLayout();
                    Option.instance.SetActivateOptionUI();
                    var helpButton = Object.FindObjectsOfType<UnityEngine.UI.Button>(true).First(b => b.name == "Help");
                    helpButton.onClick.Invoke();
                    break;
                case 4:
                    Require(Time.timeScale == 0, string.Empty);
                    LastPolishValidation.EndPauseCheck(Require);
                    var helpPanel = Get<GameObject>(Option.instance, "helpUI");
                    Require(helpPanel != null && helpPanel.activeInHierarchy && InGameUI.instance.IsOpened(InGameUI.ID_ESC_OPTION_HELP),
                        string.Empty);
                    var helpController = helpPanel.GetComponentInChildren<HelpUIController>();
                    Require(helpController != null && Get<List<SHelpCategorySO>>(helpController, "categories").Count > 0,
                        string.Empty);
                    Call(helpController, "SelectCategory", 0);
                    Require(!string.IsNullOrEmpty(Get<TextMeshProUGUI>(helpController, "contentTitleText").text),
                        string.Empty);
                    helpPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == "CloseButton").onClick.Invoke();
                    InGameUI.instance.ShowActionFeedback("중요 알림 확인", 2);
                    InGameUI.instance.ShowActionFeedback("일반 알림 확인", 0);
                    Require(Get<TMPro.TextMeshProUGUI>(InGameUI.instance, "actionFeedback").text == "중요 알림 확인", string.Empty);
                    Option.instance.SetDeactivateOptionUI();
                    Option.instance.SetDeactivateEscUI();
                    break;
                case 5:
                    Require(Time.timeScale == 1, string.Empty);
                    Require(!Get<GameObject>(Option.instance, "helpUI").activeSelf, string.Empty);
                    Require(!Get<TMPro.TextMeshProUGUI>(InGameUI.instance, "actionFeedback").raycastTarget, string.Empty);
                    CheckAudioAdmission();
                    AudioManager.instance.FadeOutForGameResult(0.1f);
                    Time.timeScale = 0;
                    break;
                case 6:
                    if (!LastPolishValidation.HasStarted)
                    {
                    Require(Get<AudioSource>(AudioManager.instance, "bgmPlayer").volume < 0.001f, string.Empty);
                    AudioManager.instance.RestoreRuntimeVolumes();
                    Require(Get<AudioSource>(AudioManager.instance, "bgmPlayer").volume > 0, string.Empty);
                    foreach (AudioSource source in Get<AudioSource[]>(AudioManager.instance, "sfxPlayers")) source.Stop();
                    Time.timeScale = 1;
                    }
                    if (!LastPolishValidation.RunChecks(Require)) return;
                    CheckGameplayEconomy();
                    CheckInventoryAndCraft();
                    CheckFishingCancellation();
                    Set(GameManager.Instance, "cycleTime", GameManager.Instance.dayDuration);
                    break;
                case 7:
                    Require(!GameManager.Instance.IsDaytime, string.Empty);
                    Require(Object.FindObjectsOfType<MinionAI>().Length >= 3, string.Empty);
                    foreach (NavMeshSurface surface in Object.FindObjectsOfType<NavMeshSurface>()) surface.BuildNavMesh();
                    break;
                case 8:
                    if (!Object.FindObjectsOfType<MinionAI>().Any(m => m.isActiveAndEnabled && !m.IsDead)) return;
                    CheckGameplayHitGeometry();
                    CheckCombat();
                    CheckKnockback();
                    CheckCombat();
                    Call(GameManager.Instance, "SpawnMariner", 1);
                    break;
                case 9:
                    probeMariner = Object.FindObjectOfType<MarinerAI>();
                    if (probeMariner != null && !probeMariner.isActiveAndEnabled) return;
                    Require(InventoryManager.Instance.GetAllItem() > ordinaryCatchBefore,
                        string.Empty);
                    Require(navigationProbe != null && navigationProbe.isOnNavMesh
                        && Vector3.Distance(navigationProbeStart, navigationProbe.nextPosition) > 0.03f,
                        string.Empty);
                    Object.Destroy(navigationProbe.gameObject);
                    Require(probeMariner != null, string.Empty);
                    CheckCrewFishingEdge();
                    TestOceanStart<OceanEventSiren>();
                    var siren = (OceanEventSiren)OceanEventManager.instance.currentEvent;
                    OceanEventManager.instance.BeginCoroutine((System.Collections.IEnumerator)Call(siren, "CharmRoutine", probeMariner));
                    break;
                case 10:
                    Require(probeMariner != null && probeMariner.isCharmed, string.Empty);
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(!probeMariner.isCharmed, string.Empty);
                    TestOceanStart<OceanEventThunder>();
                    break;
                case 11:
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(Get<float>(PlayerCore.Instance, "thunderSpeedMultiplier") == 1f, string.Empty);
                    TestOceanStart<OceanEventFog>();
                    OceanEventManager.instance.EnterNight();
                    OceanEventManager.instance.EnterNight();
                    OceanEventManager.instance.EndCurrentEvent();
                    TestOceanStart<OceanEventWind>();
                    var airborne = probeMariner.GetComponent<WindAirborne>() ?? probeMariner.gameObject.AddComponent<WindAirborne>();
                    airborne.ApplyAirborne(1, 0.1f, Vector3.right, 0);
                    Coroutine gust = Get<Coroutine>(airborne, "airborneCoroutine");
                    airborne.ApplyAirborne(1, 0.1f, Vector3.right, 0);
                    Require(gust != null && gust == Get<Coroutine>(airborne, "airborneCoroutine") && !airborne.CanBeLifted,
                        string.Empty);
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(!airborne.IsAirborne, string.Empty);
                    Set(airborne, "nextAirborneTime", 0f);
                    var windAgent = probeMariner.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    windAgent.isStopped = false;
                    airborne.ApplyAirborne(0.8f, 0.45f, Vector3.right, 0.65f);
                    probeMariner.GetComponent<StunHandler>()?.ApplyStun(0.45f);
                    TestOceanStart<OceanEventWaterBloom>();
                    OceanEventManager.instance.EndCurrentEvent();
                    TestOceanStart<OceanEventNormal>();
                    OceanEventManager.instance.EndCurrentEvent();
                    break;
                case 12:
                    var landed = probeMariner.GetComponent<WindAirborne>();
                    var landedAgent = probeMariner.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (landed.IsAirborne || (probeMariner.GetComponent<StunHandler>()?.IsStunned ?? false)) return;
                    Require(landedAgent.isOnNavMesh && landedAgent.updatePosition,
                        string.Empty);
                    Call(MarinerManager.Instance, "InfectMariner", probeMariner);
                    break;
                case 13:
                    var infected = Object.FindObjectOfType<InfectedMarinerAI>();
                    Require(infected != null, string.Empty);
                    Call(infected, "ChangeToZombieAI");
                    Time.timeScale = 10;
                    break;
                case 14:
                    if (Object.FindObjectOfType<ZombieMarinerAI>() == null) return;
                    Time.timeScale = 1;
                    Require(true, string.Empty);
                    var crew = Object.Instantiate(Get<GameObject>(GameManager.Instance, "marinerPrefab")).GetComponent<MarinerAI>();
                    int deathsBefore = Get<int>(GuiltySystem.instance, "deadCount");
                    crew.WhenDestroy();
                    Require(Get<int>(GuiltySystem.instance, "deadCount") == deathsBefore + 1, string.Empty);
                    Set(GameManager.Instance, "cycleTime", GameManager.Instance.nightDuration);
                    Call(GameManager.Instance, "UpdateDayNightCycle");
                    Require(GameManager.Instance.IsDaytime && GameManager.Instance.currentDay == 2, string.Empty);
                    GameManager.Instance.TriggerGameClear();
                    break;
                case 15:
                    StatusHUDValidation.CheckCleared(Require, "GameClear");
                    Require(GameManager.Instance.IsGameResultActive && Time.timeScale == 0, string.Empty);
                    Require(GameManager.Instance.gameOverUI.gameOverPanel.activeInHierarchy, string.Empty);
                    GameModeState.StartInfiniteMode();
                    GameManager.Instance.ResumeFromEndingToInfiniteMode();
                    break;
                case 16:
                    Require(!GameManager.Instance.IsGameResultActive && Time.timeScale == 1 && GameModeState.IsInfiniteMode, string.Empty);
                    Require(!GameManager.Instance.gameOverUI.gameOverPanel.activeSelf, string.Empty);
                    cameraBeforeResult = Camera.main.transform.position;
                    var hitSource = new GameObject("결과 화면 밀림 확인");
                    hitSource.transform.position = PlayerCore.Instance.transform.position - Vector3.right;
                    PlayerCore.Instance.TakeDamage(1, hitSource);
                    Object.Destroy(hitSource);
                    playerBeforeResult = PlayerCore.Instance.transform.position;
                    GameManager.Instance.TriggerGameOver();
                    break;
                case 17:
                    if (!GameManager.Instance.gameOverUI.gameOverPanel.activeInHierarchy) return;
                    StatusHUDValidation.CheckCleared(Require, "GameOver");
                    Require(Time.timeScale == 0, string.Empty);
                    Require(!PlayerCore.Instance.IsKnockbackActive && Vector3.Distance(playerBeforeResult, PlayerCore.Instance.transform.position) < 0.02f,
                        string.Empty);
                    Require(Vector3.Distance(cameraBeforeResult, Camera.main.transform.position) > 0.5f, string.Empty);
                    Call(GameManager.Instance.gameOverUI, "RestartGame");
                    break;
                case 18:
                    Require(BuffUIManager.Instance != null && Object.FindObjectsOfType<BuffUIManager>().Length == 1,
                        string.Empty);
                    StatusHUDValidation.CheckFresh(Require);
                    Require(GameManager.Instance != null && !GameManager.Instance.IsGameResultActive && Time.timeScale == 1, string.Empty);
                    Require(Object.FindObjectsOfType<AudioManager>().Length == 1 && Object.FindObjectsOfType<Option>().Length == 1
                        && Object.FindObjectsOfType<CreatureEffect>().Length == 1, string.Empty);
                    Require(Camera.main.GetComponent<CinemachineBrain>().enabled, string.Empty);
                    GameManager.Instance.gameOverUI.GoToTitle();
                    break;
                case 19:
                    Require(SceneManager.GetActiveScene().name == "Title", string.Empty);
                    Require(BuffUIManager.Instance == null && PlayerCore.Instance == null,
                        string.Empty);
                    AudioSource bgm = Get<AudioSource>(AudioManager.instance, "bgmPlayer");
                    Require(bgm.clip == AudioManager.instance.bgmClips[(int)AudioManager.BGM.MainTitle] && bgm.volume > 0, string.Empty);
                    EndPlay();
                    return;
            }
            SessionState.SetInt(StageKey, stage + 1);
            nextTick = EditorApplication.timeSinceStartup + (stage == 16 ? 2.2 : 0.6);
            SaveReport();
        }
        catch (Exception error)
        {
            report.errors.Add("검증 단계 " + stage + ": " + error);
            EndPlay();
        }
    }

    private static void CheckEscMenuLayout()
    {
        Canvas.ForceUpdateCanvases();
        var menu = Get<GameObject>(Option.instance, "escUI");
        var buttons = menu.GetComponentsInChildren<UnityEngine.UI.Button>();
        string[] names = { "Continue", "Option", "Help", "Exit" };
        Require(buttons.Length == 4 && names.All(name => buttons.Count(b => b.name == name) == 1),
            string.Empty);
        var corners = new Vector3[4];
        float previousBottom = float.PositiveInfinity;
        for (int i = 0; i < names.Length; i++)
        {
            var button = buttons.First(b => b.name == names[i]);
            ((RectTransform)button.transform).GetWorldCorners(corners);
            float top = corners.Max(c => c.y);
            float bottom = corners.Min(c => c.y);
            Require(top > bottom && top < previousBottom, string.Empty);
            previousBottom = bottom;
        }
    }

    private static void CheckAudioAdmission()
    {
        AudioManager audio = AudioManager.instance;
        probeClip = AudioClip.Create("출시 검증 음원", 44100 * 3, 1, 44100, false);
        var clips = Get<Dictionary<AudioManager.SFX, AudioClip>>(audio, "sfxDictionary");
        clips[AudioManager.SFX.Click] = probeClip;
        for (int i = 0; i < 100; i++) audio.PlaySfx(AudioManager.SFX.Click);
        var sources = Get<AudioSource[]>(audio, "sfxPlayers");
        Require(sources.Count(s => s.isPlaying && s.clip == probeClip) == 1, string.Empty);
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].clip = probeClip;
            sources[i].Play();
            Get<int[]>(audio, "channelPriorities")[i] = 0;
            Get<AudioManager.SFX[]>(audio, "channelSfx")[i] = AudioManager.SFX.HeavyRain;
        }
        audio.PlaySfx(AudioManager.SFX.GameOver);
        Require(Get<AudioManager.SFX[]>(audio, "channelSfx").Contains(AudioManager.SFX.GameOver), string.Empty);
        int[] priorities = Get<int[]>(audio, "channelPriorities");
        Require(sources.Where((s, i) => priorities[i] <= 1).All(s => s.volume <= audio.sfxVolume * 0.5f), string.Empty);
        foreach (AudioSource source in sources) source.Stop();
        var times = Get<Dictionary<AudioManager.SFX, float>>(audio, "lastSfxTimes");
        times.Clear();
        var probeSounds = new[] { AudioManager.SFX.BeforeAttack_Minion, AudioManager.SFX.AfterAttack_Minion,
            AudioManager.SFX.BeforeAttack_Crawler, AudioManager.SFX.AfterAttack_Crawler,
            AudioManager.SFX.BalistaAttack, AudioManager.SFX.ActivatedSpiketrap,
            AudioManager.SFX.HeavyRain, AudioManager.SFX.Hurricane, AudioManager.SFX.Die, AudioManager.SFX.Hit_Object };
        foreach (var sound in probeSounds) clips[sound] = probeClip;
        for (int repeat = 0; repeat < 30; repeat++)
            foreach (var sound in probeSounds) { times.Clear(); audio.PlaySfx(sound); }
        Require(sources.Count(s => s.isPlaying) <= 8, string.Empty);
        var channelIds = Get<AudioManager.SFX[]>(audio, "channelSfx");
        Require(sources.Where((s, i) => s.isPlaying && probeSounds.Take(4).Contains(channelIds[i])).Count() <= 3,
            string.Empty);
        clips[AudioManager.SFX.Sanity29Down] = probeClip;
        times.Clear();
        audio.PlaySfx(AudioManager.SFX.Sanity29Down);
        audio.PlaySfx(AudioManager.SFX.Thunder);
        audio.PlaySfx(AudioManager.SFX.Hit);
        audio.PlaySfx(AudioManager.SFX.Click);
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Sanity29Down).Any(),
            string.Empty);
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Thunder).Any(), string.Empty);
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Hit).Any(), string.Empty);
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Click).Any(), string.Empty);
        Require(sources.Count(s => s.isPlaying) <= 8, string.Empty);
    }

    private static void CheckInventoryAndCraft()
    {
        var inventory = InventoryManager.Instance;
        var recipe = ItemRecipeManager.Instance.recipes.First(r => r.result.id >= 10000 && r.result.id != 40007 && r.input.Length > 0 && r.input.Length <= 3
            && r.input.All(i => i.id >= 30000 && i.id != r.result.id));
        var ui = Object.FindObjectsOfType<DefaultFabrication>(true).First(u => u.craftButton != null && u.timeLeft != null);
        foreach (var input in recipe.input) inventory.Remove(new SItemStack(input.id, inventory.Get(input.id)));
        int before = inventory.Get(recipe.result.id);
        CommonUI.instance.Craft(recipe, Array.Empty<GameObject>(), ui);
        Require(inventory.Get(recipe.result.id) == before, string.Empty);
        foreach (var input in recipe.input) inventory.Add(input.Copy());
        Require(ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id), string.Empty);
        CommonUI.instance.Craft(recipe, Array.Empty<GameObject>(), ui);
        Require(inventory.Get(recipe.result.id) >= before + recipe.result.amount, string.Empty);
        Require(recipe.input.All(i => inventory.Get(i.id) < i.amount), string.Empty);

        int item = recipe.input[0].id;
        int max = ItemTypeManager.Instance.itemTypeSearch[item].maxStack;
        var sourceObject = new GameObject("승무원 인벤토리 확인");
        var storageObject = new GameObject("가득 찬 보관함 확인");
        var source = sourceObject.AddComponent<MarinerInventory>();
        source.enabled = false;
        source.itemLists = new List<SItemStack> { new SItemStack(item, 5) };
        var storage = storageObject.AddComponent<InventoryBase>();
        storage.itemLists = new List<SItemStack> { new SItemStack(item, max - 2) };
        source.TransferAllItemsToStorage(null);
        source.TransferAllItemsToStorage(storage);
        Require(storage.Get(item) == max && source.Get(item) == 3, string.Empty);
        source.TransferAllItemsToStorage(storage);
        Require(source.Get(item) == 3, string.Empty);
        Object.Destroy(sourceObject); Object.Destroy(storageObject);
    }

    private static void CheckGameplayEconomy()
    {
        var obtainable = new HashSet<int>(PlayerFishing.instance.dropItemTable
            .Where(d => d.itemData != null && d.dropProbability > 0f).Select(d => d.itemData.id));
        foreach (RandomBox box in Get<RandomBox[]>(TreasureBoxManager.instance, "reward"))
            if (box.weight > 0f) obtainable.Add(box.reward.id);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03_Prefabs/Unit" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (ItemDropper dropper in prefab.GetComponentsInChildren<ItemDropper>(true))
                foreach (var drop in dropper.dropElements)
                    if (drop.weight > 0f) obtainable.Add(drop.item.id);
        }
        var recipes = ItemRecipeManager.Instance.recipes;
        for (int pass = 0; pass < recipes.Count; pass++)
            foreach (var recipe in recipes)
                if (recipe.input.All(i => obtainable.Contains(i.id))) obtainable.Add(recipe.result.id);
        Require(recipes.All(r => r.input.All(i => obtainable.Contains(i.id))),
            string.Empty);
        var stats = PlayerStatsLevel.Instance;
        Require(stats.fishingList.Count > 5 && stats.fishingList[5].count > 0f
            && Enumerable.Range(1, 5).All(i => stats.fishingList[i].count >= stats.fishingList[i - 1].count
                && stats.fishingList[i].chest >= stats.fishingList[i - 1].chest
                && stats.craftingList[i] >= stats.craftingList[i - 1]
                && stats.combatList[i].attack >= stats.combatList[i - 1].attack),
            string.Empty);
        int oldLevel = stats.growStates[GrowStatType.Combat].level;
        float first = stats.CombatDamageMultiplier;
        stats.growStates[GrowStatType.Combat].level = 5;
        Require(Mathf.RoundToInt(15f * stats.CombatDamageMultiplier) > Mathf.RoundToInt(15f * first),
            string.Empty);
        stats.growStates[GrowStatType.Combat].level = oldLevel;
        Require((int)Call(GameManager.Instance, "CalcMarinerEmbarkCount", 99, 5) == 0
            && (int)Call(GameManager.Instance, "CalcMarinerEmbarkCount", 99, 4) == 1,
            string.Empty);
    }

    private static void CheckGameplayHitGeometry()
    {
        var player = PlayerCore.Instance;
        var attack = player.PlayerAttack;
        Vector3 oldPosition = attack.transform.position;
        Quaternion oldRotation = attack.transform.rotation;
        Vector3 oldScale = attack.transform.localScale;
        int oldDamage = attack.damage;
        GameObject crawler = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/Unit/Crawler.prefab"));
        try
        {
            crawler.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            var ai = crawler.GetComponent<CrawlerAI>();
            ai.enabled = false; ai.hp = ai.maxHp = 50;

            crawler.transform.position = player.transform.position + Vector3.right * 1.4f + Vector3.up * 1.15f;
            Physics.SyncTransforms();
            Require(attack.HasEnemyInDirection(Vector3.right, 1.5f), string.Empty);
            attack.transform.position = new Vector3(player.transform.position.x + 0.75f, player.AttackHeight, player.transform.position.z);
            attack.transform.rotation = Quaternion.LookRotation(Vector3.right);
            attack.SetAttackRange(1.5f);
            var box = (BoxCollider)attack.attackCollider;
            var hits = Physics.OverlapBox(box.transform.TransformPoint(box.center),
                Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f, box.transform.rotation,
                player.EnemyLayer, QueryTriggerInteraction.Ignore);
            Require(hits.Any(h => h.GetComponentInParent<CrawlerAI>() == ai),
                string.Empty);
            attack.damage = 1; attack.EnableAttackCollider();
            foreach (var hit in hits) Call(attack, "TryDealDamage", hit);
            Require(ai.hp == 49, string.Empty);
            attack.DisableAttackCollider();
            crawler.transform.position = player.transform.position + Vector3.right * 3f;
            Physics.SyncTransforms();
            Require(!attack.HasEnemyInDirection(Vector3.right, 1.5f), string.Empty);
            var trapObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/06_Modelings/50007 - SpikeTrap/PF_SpikeTrap.prefab"));
            try
            {
                var trap = trapObject.GetComponent<SpikeTrap>();
                Set(trap, "isTriggerd", true); Set(trap, "spikesRaised", true);
                int hp = ai.hp;
                Call(trap, "OnTriggerStay", crawler.GetComponent<Collider>());
                Call(trap, "OnTriggerStay", crawler.GetComponent<Collider>());
                Require(ai.hp == hp - 10, string.Empty);
            }
            finally { Object.Destroy(trapObject); }
        }
        finally
        {
            Object.Destroy(crawler);
            attack.DisableAttackCollider(); attack.damage = oldDamage;
            attack.transform.SetPositionAndRotation(oldPosition, oldRotation);
            attack.transform.localScale = oldScale;
        }
    }

    private static void CheckCrewFishingEdge()
    {
        var agent = probeMariner.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var playerAgent = PlayerCore.Instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
        Require(UnityEngine.AI.NavMesh.SamplePosition(playerAgent.nextPosition, out var center, 3f, agent.areaMask),
            string.Empty);
        Vector3 oldPosition = probeMariner.transform.position;
        agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
        Vector3 edge = (Vector3)Call(probeMariner, "FindMyOwnEdgePoint");
        Require(edge != Vector3.zero, string.Empty);
        Vector3 sea = Get<Vector3>(probeMariner, "personalSeaDirection");
        Require(!Physics.Raycast(edge + sea * 1.2f + Vector3.up * 3f, Vector3.down, 6f, LayerMask.GetMask("Platform")),
            string.Empty);
        agent.Warp(edge + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
        Require((bool)Call(probeMariner, "HasSeaAtFishingPoint"), string.Empty);
        agent.Warp(oldPosition);
    }

    private static void CheckFishingCancellation()
    {
        var fishing = PlayerFishing.instance;
        Require(fishing != null && fishing.fishingEventUI != null, string.Empty);
        fishing.StartFishingLoop();
        Coroutine first = Get<Coroutine>(fishing, "fishingLoopCoroutine");
        fishing.StartFishingLoop();
        Require(first != null && first == Get<Coroutine>(fishing, "fishingLoopCoroutine"), string.Empty);
        fishing.StopFishingLoop();
        var ui = fishing.fishingEventUI;
        float chance = Get<float>(fishing, "eventChance");
        Set(fishing, "eventChance", 0f); Set(fishing, "nonEventCount", 0);
        int beforeItems = InventoryManager.Instance.GetAllItem();
        ordinaryCatchBefore = beforeItems;
        int beforeDrops = Object.FindObjectsOfType<DroppedItem>().Sum(d => d.itemValue != null ? d.itemValue.amount : 0);
        Vector3 oldDirection = Get<Vector3>(fishing, "fishingDirection");
        Set(fishing, "fishingDirection", Vector3.right);
        var attempt = (System.Collections.IEnumerator)Call(fishing, "FishingLoop");
        Require(attempt.MoveNext() && attempt.Current is WaitForSeconds, string.Empty);
        Require(!attempt.MoveNext() && PlayerCore.Instance.currentState == PlayerCore.PlayerState.Default
            && Get<Coroutine>(fishing, "fishingLoopCoroutine") == null
            && InventoryManager.Instance.GetAllItem() + Object.FindObjectsOfType<DroppedItem>().Sum(d => d.itemValue != null ? d.itemValue.amount : 0) > beforeItems + beforeDrops,
            string.Empty);
        Set(fishing, "fishingDirection", oldDirection);
        Set(fishing, "eventChance", 1f);
        attempt = (System.Collections.IEnumerator)Call(fishing, "FishingLoop");
        attempt.MoveNext();
        Require(attempt.MoveNext() && attempt.Current is WaitForSeconds && !ui.fishingEvent_UI.activeSelf,
            string.Empty);

        int beforeFailure = InventoryManager.Instance.GetAllItem();
        Require(attempt.MoveNext() && attempt.Current is System.Collections.IEnumerator,
            string.Empty);
        Require(!attempt.MoveNext() && InventoryManager.Instance.GetAllItem() == beforeFailure
            && PlayerCore.Instance.currentState == PlayerCore.PlayerState.Default,
            string.Empty);
        fishing.StopFishingLoop();
        Set(fishing, "eventChance", chance);
        var treasure = TreasureBoxManager.instance;
        var rewards = Get<List<SItemStack>>(treasure, "rewardStack");
        while (rewards.Count > 0) treasure.Accept();
        var beforeReward = ItemTypeManager.Instance.itemTypeSearch.Keys.ToDictionary(id => id, id => InventoryManager.Instance.Get(id));
        treasure.GetSpecialBox();
        var granted = rewards[0].Copy();
        Require(granted.id != 30001 && InventoryManager.Instance.Get(granted.id) == beforeReward[granted.id] + granted.amount,
            string.Empty);
        treasure.Accept();
        Require(InventoryManager.Instance.Get(granted.id) == beforeReward[granted.id] + granted.amount,
            string.Empty);
        bool called = false;
        var qte = ui.StartQTE(success => called = true);
        Require(qte.MoveNext(), string.Empty);
        float value = ui.slider.value;
        Time.timeScale = 0;
        qte.MoveNext();
        Require(ui.slider.value == value, string.Empty);
        ui.CloseUI();
        qte.MoveNext();
        Time.timeScale = 1;
        Require(!called && !ui.fishingEvent_UI.activeSelf && Get<Coroutine>(fishing, "fishingLoopCoroutine") == null,
            string.Empty);
    }

    private static void CheckCombat()
    {
        var attack = Object.FindObjectOfType<PlayerAttack>();
        var target = Object.FindObjectsOfType<MinionAI>().First(m => m.isActiveAndEnabled && !m.IsDead);
        var collider = target.GetComponentsInChildren<Collider>().First(c => c.enabled && c.gameObject.layer == LayerMask.NameToLayer("Enemy"));
        int before = target.hp;
        attack.damage = 1;
        attack.EnableAttackCollider();
        Call(attack, "TryDealDamage", collider);
        Call(attack, "TryDealDamage", collider);
        Require(target.hp == before - 1, string.Empty);
        attack.DisableAttackCollider();
        Call(attack, "TryDealDamage", collider);
        Require(target.hp == before - 1, string.Empty);
    }

    private static void StepKnockback(CreatureBase creature, float dt)
    {
        typeof(CreatureBase).GetMethod("TickHitKnockback", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(creature, new object[] { dt });
    }

    private static void CheckKnockback()
    {
        var player = PlayerCore.Instance;
        var playerAgent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var target = Object.FindObjectsOfType<MinionAI>().First(m => m.isActiveAndEnabled && !m.IsDead);
        var agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var source = new GameObject("밀림 방향 확인");
        Vector3 oldPlayerPosition = player.transform.position;
        Vector3 oldTargetPosition = target.transform.position;
        int oldPlayerHp = player.hp;
        int oldTargetHp = target.hp;
        bool oldStopped = agent.isStopped;
        bool oldPlayerStopped = playerAgent.isStopped;
        try
        {
            Require(UnityEngine.AI.NavMesh.SamplePosition(oldPlayerPosition - Vector3.up * playerAgent.baseOffset, out var center, 0.5f, playerAgent.areaMask), string.Empty);
            var body = player.GetComponent<Rigidbody>();
            source.transform.position = body.position - Vector3.right;
            Vector3 before = body.position;
            player.TakeDamage(1, source);
            Require(player.IsKnockbackActive, string.Empty);
            StepKnockback(player, 0f);
            Require(Vector3.Distance(before, body.position) < 0.001f, string.Empty);
            StepKnockback(player, 0.12f);
            Require(!player.IsKnockbackActive && Vector3.Distance(before, body.position) > 0.02f
                && Vector3.Distance(before, body.position) <= 0.19f, string.Empty);
            player.Move(Vector3.right);
            Require(player.GetComponent<Rigidbody>().velocity.x > 0 && playerAgent.isStopped == oldPlayerStopped,
                string.Empty);
            player.StopHorizontalMovement();
            player.TakeDamage(1, null);
            Require(!player.IsKnockbackActive, string.Empty);


            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            agent.isStopped = true;
            source.transform.position = target.transform.position - Vector3.right;
            target.hp = 100;
            before = target.transform.position;
            for (int i = 0; i < 20; i++) target.TakeDamage(1, source);
            StepKnockback(target, 0.12f);
            Require(target.hp == 80 && !target.IsKnockbackActive && Vector3.Distance(before, target.transform.position) > 0.02f && Vector3.Distance(before, target.transform.position) <= 0.29f,
                string.Empty);
            Require(agent.isOnNavMesh && agent.isStopped && agent.updatePosition, string.Empty);
            var stun = target.GetComponent<StunHandler>() ?? target.gameObject.AddComponent<StunHandler>();
            stun.ApplyStun(2f);
            target.TakeDamage(1, source);
            StepKnockback(target, 0.12f);
            Require(stun.IsStunned && agent.isStopped, string.Empty);
            stun.ClearStun();
            Require(!agent.isStopped, string.Empty);
            agent.SetDestination(center.position);
            Require(agent.isOnNavMesh && agent.enabled && agent.updatePosition, string.Empty);

            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            if (agent.Raycast(center.position + Vector3.right * 100f, out var edge))
            {
                agent.Warp(edge.position - Vector3.right * 0.03f + Vector3.up * agent.baseOffset);

                agent.Move(Vector3.zero);
                before = agent.nextPosition;
                source.transform.position = before - Vector3.right;
                target.TakeDamage(1, source);
                StepKnockback(target, 0.12f);
                Require(agent.isOnNavMesh && !UnityEngine.AI.NavMesh.Raycast(before, agent.nextPosition, out _, agent.areaMask)
                    && Vector3.Distance(before, agent.nextPosition) <= 0.29f, string.Empty);
            }
            else throw new Exception("밀림 검증에 사용할 갑판 경계를 찾지 못했습니다.");
            target.TakeDamage(1, source);
            target.IsDead = true;
            before = target.transform.position;
            StepKnockback(target, 0.12f);
            Require(!target.IsKnockbackActive && Vector3.Distance(before, target.transform.position) < 0.001f,
                string.Empty);
            target.IsDead = false;
            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            var airborne = target.GetComponent<WindAirborne>() ?? target.gameObject.AddComponent<WindAirborne>();
            airborne.ApplyAirborne(0.1f, 0.1f, Vector3.right, 0f);
            target.TakeDamage(1, source);
            Require(!target.IsKnockbackActive, string.Empty);
            airborne.CancelAirborne();
        var deathProbe = new GameObject("사망 시 밀림 확인");
            deathProbe.transform.position = center.position;
            deathProbe.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var creature = deathProbe.AddComponent<CreatureBase>();
            creature.hp = 2;
            creature.TakeDamage(1, source);
            creature.TakeDamage(1, source);
            before = creature.transform.position;
            StepKnockback(creature, 0.12f);
            Require(creature.IsDead && !creature.IsKnockbackActive && creature.transform.position == before,
                string.Empty);
            target.TakeDamage(1, source);
            target.enabled = false;
            Require(!target.IsKnockbackActive, string.Empty);
            target.enabled = true;
        var walker = new GameObject("밀림 이후 이동 확인");
            walker.transform.position = center.position;
            navigationProbe = walker.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navigationProbe.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
            navigationProbe.speed = 1f;
            var walkerCreature = walker.AddComponent<CreatureBase>();
            walkerCreature.hp = 10;
            walkerCreature.TakeDamage(1, source);
            StepKnockback(walkerCreature, 0.12f);
            navigationProbeStart = navigationProbe.nextPosition;
            navigationProbe.SetDestination(center.position + Vector3.forward * 0.5f);
        }
        finally
        {
            Object.Destroy(source);
            player.hp = oldPlayerHp;
            playerAgent.Warp(oldPlayerPosition);
            player.StopHorizontalMovement();
            target.hp = oldTargetHp;
            target.IsDead = false;
            agent.Warp(oldTargetPosition);
            agent.isStopped = oldStopped;
        }
    }

    private static void TestOceanStart<T>() where T : OceanEventBase
    {
        var manager = OceanEventManager.instance;
        manager.EndCurrentEvent();
        Set(manager, "enteredNight", false);
        var oceanEvent = Get<List<OceanEventBase>>(manager, "allEvents").OfType<T>().First();
        manager.currentEvent = oceanEvent;
        oceanEvent.EventRun();
        Require(oceanEvent.IsRunning, string.Empty);
    }

    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    private static void Require(bool success, string message)
    {
        if (!success) throw new Exception(message);
        report.passed.Add(message);
    }

    private static void CaptureLog(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            if (report.errors.Count < 100) report.errors.Add(message + "\n" + stack);
            SaveReport();
        }
    }

    private static void EndPlay()
    {
        int previous = SessionState.GetInt("Pioneer.ReleaseValidation.Unlock", -1);
        if (previous < 0) PlayerPrefs.DeleteKey("InfiniteModeUnlocked");
        else PlayerPrefs.SetInt("InfiniteModeUnlocked", previous);
        PlayerPrefs.Save();
        Time.timeScale = 1;
        SessionState.SetInt(StageKey, 100);
        SaveReport();
        EditorApplication.ExitPlaymode();
    }

    private static void Finish()
    {
        SessionState.SetBool(RestorePendingKey, false);
        ClearActiveSession();
        if (report == null) report = JsonUtility.FromJson<Report>(SessionState.GetString(ReportKey, "{}"));
        if (report == null) return;
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(SetupKey, "{}"));
        if (setup != null && setup.scenes != null && setup.scenes.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
            EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
        SaveReport();
        if (Application.isBatchMode) EditorApplication.Exit(report.errors.Count == 0 ? 0 : 1);
    }

    private static void SaveReport()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText(ReportPath, json);
        SessionState.SetString(ReportKey, json);
    }

    [MenuItem("Tools/Pioneer/출시 검증/윈도우 후보 빌드")]
    public static void BuildWindows() => BuildWindowsAt("Builds/ReleaseCandidate");

    [MenuItem("Tools/Pioneer/출시 검증/최종 빌드")]
    public static void BuildFinalPolish() => BuildWindowsAt("Builds/FinalPolish");

    [MenuItem("Tools/Pioneer/출시 검증/스토브 빌드")]
    public static void BuildStovePolish() => BuildWindowsAt("Builds/StovePolish");

    private static void BuildWindowsAt(string outputDirectory)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Directory.CreateDirectory(outputDirectory);
        BuildReport build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outputDirectory + "/Pioneer.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Directory.CreateDirectory("Logs/ReleaseValidation");
        File.WriteAllText("Logs/ReleaseValidation/build-result.txt", build.summary.result + "\n오류: " + build.summary.totalErrors
            + "\n경고: " + build.summary.totalWarnings + "\n크기: " + build.summary.totalSize);
        File.WriteAllLines("Logs/ReleaseValidation/build-messages.txt", build.steps.SelectMany(s => s.messages)
            .Where(m => m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Warning)
            .Select(m => m.type + ": " + m.content));
        if (Application.isBatchMode) EditorApplication.Exit(build.summary.result == BuildResult.Succeeded && build.summary.totalErrors == 0 ? 0 : 1);
    }
}

public static class LastPolishValidation
{
    private static bool started, complete;
    private static Coroutine runningChecks;
    private static GameManager checksOwner;
    private static IEnumerator checksIterator;
    public static bool HasStarted => started;
    public static void Reset()
    {
        if (checksOwner != null && runningChecks != null) checksOwner.StopCoroutine(runningChecks);
        // Explicitly dispose so saved gameplay state is restored even on cancellation.
        try { (checksIterator as IDisposable)?.Dispose(); }
        catch (Exception error) { Debug.LogWarning("Validation cleanup: " + error.Message); }
        runningChecks = null; checksOwner = null; checksIterator = null;
        started = complete = false; failure = null;
    }
    private static Exception failure;
    private static float pausedProgress;
    private static Vector3 pausedClockScale;

    public static void BeginPauseCheck()
    {
        pausedProgress = GameManager.Instance.CurrentPhaseProgress;
        pausedClockScale = Object.FindObjectOfType<SurvivalClockFace>().transform.localScale;
    }
    public static void EndPauseCheck(Action<bool, string> check)
    {
        check(GameManager.Instance.CurrentPhaseProgress == pausedProgress, string.Empty);
        check(Object.FindObjectOfType<SurvivalClockFace>().transform.localScale == pausedClockScale,
            string.Empty);
    }

    public static bool RunChecks(Action<bool, string> check)
    {
        if (!ReleaseValidation.IsRunning) throw new InvalidOperationException("Gameplay checks require an explicitly owned ReleaseValidation Play session.");
        if (failure != null) throw failure;
        if (complete) return true;
        if (!started)
        {
            started = true;
            checksOwner = GameManager.Instance;
            checksIterator = Checks(check);
            runningChecks = checksOwner.StartCoroutine(RunGuarded(checksIterator));
        }
        return false;
    }

    private static IEnumerator RunGuarded(IEnumerator checks)
    {
        while (true)
        {
            object current;
            try
            {
                if (!checks.MoveNext()) { complete = true; yield break; }
                current = checks.Current;
            }
            catch (Exception error) { failure = error; yield break; }
            yield return current;
        }
    }

    private static IEnumerator Checks(Action<bool, string> check)
    {
        var game = GameManager.Instance;
        var inventory = InventoryManager.Instance;
        var ui = InventoryUiMain.instance;
        var player = PlayerCore.Instance;
        var saved = inventory.itemLists.ToArray();
        var mouse = inventory.mouseInventory;
        int selected = inventory.selectedSlotIndex, hp = player.hp, fullness = player.currentFullness, mental = player.currentMental;
        float cycle = Get<float>(game, "cycleTime");
        float dayDuration = game.dayDuration, nightDuration = game.nightDuration;
        var clock = InGameUI.instance.gameObjectClock.GetComponent<InGameUiClockOut>();
        var face = clock.GetComponentInChildren<SurvivalClockFace>();
        try
        {
            check(clock != null && face != null && face.raycastTarget == false, string.Empty);
            check(ui.RegularSlotIndices.SequenceEqual(Enumerable.Range(9, 18)), string.Empty);
            foreach (float duration in new[] { 37f, 223f })
            {
                game.dayDuration = duration;
                Set(game, "cycleTime", duration * 0.5f);
                Call(clock, "LateUpdate");
                check(Mathf.Abs(game.CurrentPhaseProgress - 0.5f) < 0.001f && Mathf.Abs(face.Progress - 0.5f) < 0.001f
                    && Mathf.Abs(game.CurrentPhaseRemaining - duration * 0.5f) < 0.001f, string.Empty);
            }
            game.dayDuration = dayDuration;
            Set(InGameUI.instance, "actionFeedbackUntil", 0f);
            Set(game, "cycleTime", dayDuration * 0.89f);
            Call(clock, "LateUpdate");
            var warning = Get<Tween>(clock, "feedback");
            var notice = Get<TextMeshProUGUI>(InGameUI.instance, "actionFeedback");
            check(warning != null && notice.text == "곧 밤이 찾아옵니다.", string.Empty);
            for (int i = 0; i < 20; i++) Call(clock, "LateUpdate");
            check(warning == Get<Tween>(clock, "feedback"), string.Empty);
            Time.timeScale = 0;
            float phaseBefore = game.CurrentPhaseProgress;
            Vector3 pulseBefore = face.transform.localScale;
            yield return new WaitForSecondsRealtime(0.2f);
            check(game.CurrentPhaseProgress == phaseBefore && face.transform.localScale == pulseBefore, string.Empty);
            Time.timeScale = 1;
            game.IsDaytime = false;
            Set(game, "cycleTime", 0f);
            Call(clock, "LateUpdate");
            check(!warning.IsActive() && face.transform.localScale == Vector3.one, string.Empty);
            yield return new WaitForSecondsRealtime(0.3f);
            check(Vector3.Distance(face.transform.localScale, Vector3.one) < 0.001f, string.Empty);
            game.IsDaytime = true;
            Set(game, "cycleTime", cycle);
            Call(clock, "LateUpdate");

            var hud = InGameUI.instance;
            hud.UseESC(); hud.UseESC(); hud.UseESC();
            yield return new WaitForSecondsRealtime(0.25f);
            check(Time.timeScale == 0 && hud.IsOpened(InGameUI.ID_ESC_OPTION)
                && Get<GameObject>(Option.instance, "escUI").GetComponent<CanvasGroup>().alpha == 1f,
                string.Empty);
            for (int i = 0; i < 5; i++)
            {
                Option.instance.SetActivateOptionUI(); Option.instance.SetDeactivateOptionUI();
                Option.instance.SetActivateHelpUI(); Option.instance.SetDeactivateHelpUI();
            }
            yield return new WaitForSecondsRealtime(0.2f);
            check(!Get<GameObject>(Option.instance, "optionUI").activeSelf && !Get<GameObject>(Option.instance, "helpUI").activeSelf
                && hud.uiChunkStack.Count == 1 && Time.timeScale == 0, string.Empty);
            hud.UseESC();
            hud.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.2f);
            check(Time.timeScale == 1 && hud.uiChunkStack.Count == 0 && !Get<GameObject>(Option.instance, "escUI").activeSelf,
                string.Empty);

            var statusChecks = StatusHUDValidation.Checks(check);
            while (statusChecks.MoveNext()) yield return statusChecks.Current;

            ClearInventory();
            inventory.itemLists[0] = new SItemStack(30002, 4);
            inventory.itemLists[2] = new SItemStack(20001, 1, 57);
            inventory.itemLists[3] = new SItemStack(50001, 2);
            inventory.itemLists[4] = new SItemStack(40004, 3);
            inventory.itemLists[8] = new SItemStack(30001, 12);
            inventory.itemLists[9] = new SItemStack(30002, 98);
            inventory.itemLists[10] = new SItemStack(30002, 5);
            inventory.itemLists[11] = new SItemStack(30001, 7);
            inventory.itemLists[12] = new SItemStack(20001, 1, 19);
            inventory.itemLists[13] = new SItemStack(20001, 1, 83);
            inventory.itemLists[14] = new SItemStack(50001, 3);
            var hotbar = inventory.itemLists.Take(9).ToArray();
            var amounts = hotbar.Select(s => s?.amount ?? 0).ToArray();
            var totals = inventory.itemLists.Where(s => s != null).GroupBy(s => s.id).ToDictionary(g => g.Key, g => g.Sum(s => s.amount));
            ui.SelectSlot(2);
            ui.Sort();
            check(Enumerable.Range(0, 9).All(i => ReferenceEquals(inventory.itemLists[i], hotbar[i]) && (inventory.itemLists[i]?.amount ?? 0) == amounts[i]),
                string.Empty);
            check(inventory.SelectedSlotInventory == hotbar[2] && inventory.selectedSlotIndex == 2, string.Empty);
            check(totals.All(p => inventory.Get(p.Key) == p.Value), string.Empty);
            var regular = inventory.itemLists.Skip(9).Where(s => s != null).ToArray();
            check(regular.SequenceEqual(regular.OrderBy(s => s.itemBaseType.categories).ThenBy(s => s.itemBaseType.typeName,
                StringComparer.Create(new CultureInfo("ko-KR"), false))) && regular.All(s => s.amount <= s.itemBaseType.maxStack),
                string.Empty);
            check(regular.Where(s => s.id == 20001).Select(s => s.duability).OrderBy(v => v).SequenceEqual(new[] { 19, 83 }),
                string.Empty);

            for (int i = 0; i < 9; i++) ui.SelectSlot(i);
            for (int i = 0; i < 30; i++) ui.SelectSlot(i % 9);
            yield return new WaitForSecondsRealtime(0.3f);
            var slots = Get<List<GameObject>>(ui, "slotGameObjects").Select(g => g.GetComponent<ItemSlotUI>()).ToArray();
            check(slots.All(s => Vector3.Distance(s.transform.localScale, Vector3.one) < 0.001f), string.Empty);
            ui.IconRefresh();
            foreach (var category in new[] { EDataType.WeaponItem, EDataType.CommonResource, EDataType.NormalItem, EDataType.ConsumeItem, EDataType.BuildObject })
            {
                var item = ItemTypeManager.Instance.types.First(t => t.categories == category);
                inventory.itemLists[0] = new SItemStack(item.id, 1, 100);
                inventory.itemLists[9] = new SItemStack(item.id, 1, 100);
                ui.IconRefresh();
                var a = slots[0].GetComponentInChildren<SlotTypeBorder>(); var b = slots[9].GetComponentInChildren<SlotTypeBorder>();
                check(a != null && b != null && a.enabled && b.enabled && a.color == b.color && a.color == ItemPresentation.CategoryColor(category)
                    && slots[0].image.color == Color.white, string.Empty);
            }
            inventory.itemLists[0] = null; ui.IconRefresh();
            check(!slots[0].GetComponentInChildren<SlotTypeBorder>().enabled, string.Empty);

            var foods = new Dictionary<int, int> { {30002,5}, {30007,5}, {30008,5}, {30011,5}, {30016,5}, {40004,20}, {40010,20}, {40005,40}, {40006,70} };
            foreach (var food in foods)
            {
                ClearInventory();
                var type = ItemTypeManager.Instance.itemTypeSearch[food.Key] as SItemConsumeTypeSO;
                check(type != null && type.categories == EDataType.ConsumeItem, string.Empty);
                var used = new SItemStack(food.Key, 2); inventory.itemLists[0] = used;
                inventory.itemLists[9] = new SItemStack(food.Key, 7); inventory.SelectSlot(0);
                player.currentFullness = 20;
                foreach (var source in Get<AudioSource[]>(AudioManager.instance, "sfxPlayers")) source.Stop();
                Get<Dictionary<AudioManager.SFX, float>>(AudioManager.instance, "lastSfxTimes").Remove(AudioManager.SFX.EatingFood);
                Drain(type.Use(player, used));
                var eatingClip = Get<Dictionary<AudioManager.SFX, AudioClip>>(AudioManager.instance, "sfxDictionary")[AudioManager.SFX.EatingFood];
                check(Get<AudioSource[]>(AudioManager.instance, "sfxPlayers").Count(s => s.isPlaying && s.clip == eatingClip) == 1,
                    string.Empty);
                check(player.currentFullness == 20 + food.Value && used.amount == 1 && inventory.itemLists[9].amount == 7,
                    string.Empty);
                check(ItemPresentation.EffectSummary(type) == "허기 +" + food.Value, string.Empty);
                player.currentFullness = player.maxFullness - 1;
                Drain(type.Use(player, used));
                check(player.currentFullness == player.maxFullness && inventory.itemLists[0] == null && inventory.SelectedSlotInventory == null,
                    string.Empty);
                Drain(type.Use(player, used));
                check(inventory.itemLists[9].amount == 7, string.Empty);
            }
            foreach (int id in new[] {30005, 30006, 30017})
                check(!(ItemTypeManager.Instance.itemTypeSearch[id] is SItemConsumeTypeSO), string.Empty);
            for (int effect = 801; effect <= 810; effect++)
            {
                ConsumeEffectInfo.TryGet(effect, out var stat, out int amount);
                player.hp = player.currentMental = player.currentFullness = 20;
                ConsumeEffectInfo.Apply(effect, player);
                int actual = stat == ConsumeEffectInfo.Stat.Health ? player.hp : stat == ConsumeEffectInfo.Stat.Mental ? player.currentMental : player.currentFullness;
                check(actual == 20 + amount && ConsumeEffectInfo.Summary(effect).EndsWith("+" + amount), string.Empty);
            }
            var heal = ItemTypeManager.Instance.types.OfType<SItemConsumeTypeSO>().First(t => t.ConsumeEffect == 802);
            check(ItemPresentation.EffectSummary(heal) == "체력 +40", string.Empty);
            foreach (int id in new[] {30001, 30003, 30004})
                check(ItemPresentation.Description(ItemTypeManager.Instance.itemTypeSearch[id]) == ItemTypeManager.Instance.itemTypeSearch[id].infomation,
                    string.Empty);
            var weapon = (SItemWeaponTypeSO)ItemTypeManager.Instance.itemTypeSearch[20001];
            check(ItemPresentation.EffectSummary(weapon) == $"기본 공격력 {weapon.weaponDamage:0.##}", string.Empty);

            foreach (int raw in foods.Keys.Where(id => id < 40000))
            {
                ClearInventory();
                var recipe = ItemRecipeManager.Instance.recipes.First(r => r.input.Any(i => i.id == raw));
                foreach (var input in recipe.input) inventory.TryAdd(input.Copy(), out _);
                check(ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id), string.Empty);
                var craftUi = Object.FindObjectsOfType<DefaultFabrication>(true).First(u => u.craftButton != null && u.timeLeft != null);
                CommonUI.instance.Craft(recipe, Array.Empty<GameObject>(), craftUi);
                int produced = inventory.Get(recipe.result.id);
                int expectedRefund = produced == recipe.result.amount * 2
                    ? recipe.input.Where(i => i.id == raw).Sum(i => i.amount * 4 / 10) : 0;
                check(inventory.Get(raw) == expectedRefund && (produced == recipe.result.amount || produced == recipe.result.amount * 2),
                    string.Empty);
            }
            CheckFeedbackAndInstallation(check);

            ClearInventory();
            int[] display = {20001, 30001, 30003, 30002, 40004, 40005, 40006, 50001, 30006};
            for (int i = 0; i < display.Length; i++) inventory.itemLists[i] = new SItemStack(display[i], 3, 100);
            for (int i = 0; i < display.Length; i++) inventory.itemLists[9 + i] = new SItemStack(display[i], 4, 100);
            ui.IconRefresh();
            player.hp = hp; player.currentFullness = fullness; player.currentMental = mental;
            yield return new WaitForSecondsRealtime(3f);
            if (!InGameUI.instance.IsPannelExpanded) InGameUI.instance.UseTab();
            ui.currentSelectedSlot.Clear(); ui.currentSelectedSlot.Add(slots[6]);
            yield return new WaitForSecondsRealtime(0.25f);
            ui.ShowWindow();
            var lore = Get<TextMeshProUGUI>(ui, "windowMouseTextInfo");
            lore.ForceMeshUpdate();
            check(lore.text.Contains("허기 +70") && !lore.isTextOverflowing, string.Empty);
            var bounds = Get<GameObject>(ui, "windowMouse").transform.Find("BackGround1") as RectTransform;
            var canvasRect = Get<Canvas>(ui, "canvas").transform as RectTransform;
            var corners = new Vector3[4]; bounds.GetWorldCorners(corners);
            check(corners.All(c => canvasRect.rect.Contains((Vector2)canvasRect.InverseTransformPoint(c))),
                string.Empty);
            Capture("Logs/ReleaseValidation/last-polish-inventory.png");
            yield return new WaitForSecondsRealtime(0.4f);
            ui.currentSelectedSlot.Clear(); ui.HideWindow();
            InGameUI.instance.UseTab();
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("Logs/ReleaseValidation/last-polish-hud.png");
            yield return new WaitForSecondsRealtime(0.3f);
        }
        finally
        {
            inventory.itemLists = saved.ToList(); inventory.mouseInventory = mouse;
            inventory.SelectSlot(selected); ui.SelectSlot(selected); ui.IconRefresh();
            player.hp = hp; player.currentFullness = fullness; player.currentMental = mental;
            game.dayDuration = dayDuration; game.nightDuration = nightDuration; game.IsDaytime = true;
            Set(game, "cycleTime", cycle); Time.timeScale = 1;
            CreateObject.instance.ExitInstallMode();
        }
    }

    private static void CheckFeedbackAndInstallation(Action<bool, string> check)
    {
        var hit = Object.FindObjectOfType<PlayerHitScreen>();
        int hp = PlayerCore.Instance.hp;
        PlayerCore.Instance.TakeDamage(1, null);
        var tween = Get<Sequence>(hit, "feedback");
        tween.Goto(0.06f, false);
        check(hit.color.a > 0 && hit.color.a <= 0.26f && !hit.raycastTarget, string.Empty);
        hit.gameObject.SetActive(false);
        check(!tween.IsActive() && hit.color.a == 0f, string.Empty);
        hit.gameObject.SetActive(true); PlayerCore.Instance.hp = hp;

        var wall = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/06_Modelings/50003 - Wooden Wall/PF_WoodenWall.prefab"));
        try
        {
            var structure = wall.GetComponent<StructureBase>();
            var collider = wall.GetComponent<Collider>();
            var before = collider.bounds;
            var position = wall.transform.position; var scale = wall.transform.localScale;
            structure.TakeDamage(1, null);
            var flash = wall.GetComponent<StructureHitFeedback>();
            check(flash != null && wall.transform.position == position && wall.transform.localScale == scale && collider.bounds == before,
                string.Empty);
            var active = Get<Tween>(flash, "flash");
            wall.SetActive(false);
            check(!active.IsActive(), string.Empty);
        }
        finally { Object.Destroy(wall); }

        ClearInventory();
        var inventory = InventoryManager.Instance;
        var install = CreateObject.instance;
        var data = (SInstallableObjectDataSO)ItemTypeManager.Instance.itemTypeSearch[50003];
        inventory.itemLists[0] = new SItemStack(data.id, 2); inventory.SelectSlot(0);
        install.EnterInstallMode(data, new[] {new SItemStack(data.id, 1)});
        check(install.IsBuilding, string.Empty);
        install.ExitInstallMode();
        check(!install.IsBuilding && Get<GameObject>(install, "tempObj") == null && inventory.Get(data.id) == 2
            && Get<Image>(install, "ringFill").fillAmount == 0f, string.Empty);
        install.EnterInstallMode(data, new[] {new SItemStack(data.id, 1)});
        var placed = Object.Instantiate(data.prefab, Get<Transform>(install, "worldSpaceParent"));
        placed.transform.position = PlayerCore.Instance.transform.position + Vector3.right * 2f;
        Set(install, "tempObj", placed); Set(install, "installTimeSec", 0f);
        var routine = (IEnumerator)Call(install, "InstallCountdownRoutine");
        Drain(routine);
        check(inventory.Get(data.id) == 1 && Get<GameObject>(install, "tempObj") == null && !placed.GetComponent<Collider>().isTrigger
            && Get<Image>(install, "ringFill").fillAmount == 0f, string.Empty);
        Object.Destroy(placed); install.ExitInstallMode();
    }

    private static void ClearInventory()
    {
        var inventory = InventoryManager.Instance;
        for (int i = 0; i < inventory.itemLists.Count; i++) inventory.itemLists[i] = null;
        inventory.mouseInventory = null; inventory.UpdateSlot();
    }

    private static void Capture(string path)
    {
        var camera = Camera.main;
        var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var canvases = Object.FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var distances = canvases.Select(c => c.planeDistance).ToArray();
        Texture2D image = null;
        try
        {
            camera.targetTexture = rt;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.1f;
            }
            Canvas.ForceUpdateCanvases();

            InventoryUiMain.instance.ShowWindow();
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = previous; RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            if (image != null) Object.Destroy(image);
            Canvas.ForceUpdateCanvases();
        }
    }
    private static void Drain(IEnumerator iterator)
    {
        int guard = 0;
        while (iterator.MoveNext())
        {
            if (++guard > 1000) throw new Exception("즉시 검증 중 예상하지 못한 대기가 발생했습니다.");
            if (iterator.Current is IEnumerator child) Drain(child);
        }
    }
    private static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(o);
    private static void Set(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(o, value);
    private static object Call(object o, string method, params object[] values) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(o, values);
}
#endif

#if UNITY_EDITOR

public static class StatusHUDValidation
{
    private static Dictionary<EffectType, GameObject> Effects(BuffUIManager hud) => Get<Dictionary<EffectType, GameObject>>(hud, "activeEffects");
    public static void CheckFresh(Action<bool, string> check)
    {
        check(Effects(BuffUIManager.Instance).Keys.All(t => t == EffectType.Fullness_Full),
            string.Empty);
    }
    public static IEnumerator Checks(Action<bool, string> check)
    {
        var hud = BuffUIManager.Instance;
        var player = PlayerCore.Instance;
        int fullness = player.currentFullness, mental = player.currentMental;
        float duration = Get<float>(player, "drunkDuration");
        check(hud != null && hud.buffParent != null && hud.debuffParent != null
            && hud.transform.IsChildOf(InGameUI.instance.transform), string.Empty);
        try
        {
            check(Effects(hud).Keys.All(t => t == EffectType.Fullness_Full), string.Empty);
            Fullness(50); player.UpdateMental(100 - player.currentMental); player.RefreshStatusEffectUI();
            yield return new WaitForSecondsRealtime(0.3f);
            check(Effects(hud).Count == 0, string.Empty);
            Fullness(90); player.RefreshStatusEffectUI();
            var full = Effects(hud)[EffectType.Fullness_Full];
            check(full.GetComponent<CanvasGroup>().alpha < 1f && !Get<Text>(full.GetComponent<StatusEffectIconUI>(), "durationText").gameObject.activeSelf,
                string.Empty);
            yield return new WaitForSecondsRealtime(0.25f);
            check(full.GetComponent<CanvasGroup>().alpha == 1 && Vector3.Distance(full.transform.localScale, Vector3.one) < 0.001f,
                string.Empty);
            Fullness(50);
            check(Effects(hud).ContainsKey(EffectType.Fullness_Full) && full.GetComponent<StatusEffectIconUI>().IsRemoving,
                string.Empty);
            yield return new WaitForSecondsRealtime(0.25f);
            check(!Effects(hud).ContainsKey(EffectType.Fullness_Full), string.Empty);
            Fullness(20); yield return new WaitForSecondsRealtime(0.2f);
            check(Effects(hud).ContainsKey(EffectType.Fullness_Hungry), string.Empty);
            Fullness(0);
            for (int frame = 0; frame < 20; frame++)
            {
                check(Effects(hud).Keys.Count(IsFullness) <= 1, string.Empty);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.2f);
            check(Effects(hud).ContainsKey(EffectType.Fullness_Starving) && !Effects(hud).ContainsKey(EffectType.Fullness_Hungry),
                string.Empty);
            Fullness(50); yield return new WaitForSecondsRealtime(0.2f);
            player.UpdateMental(40 - player.currentMental); player.RefreshStatusEffectUI();
            float healthyDamage = player.AttackDamageCalculated;
            player.UpdateMental(-1); player.RefreshStatusEffectUI();
            check(player.IsMentalDebuff() && player.AttackDamageCalculated < healthyDamage && Effects(hud).ContainsKey(EffectType.Mental_Unstable),
                string.Empty);
            var unstable = Effects(hud)[EffectType.Mental_Unstable];
            for (int i = 0; i < 25; i++) { hud.EndUI(EffectType.Mental_Unstable); hud.EndUI(EffectType.Mental_Unstable); hud.BeginUI(EffectType.Mental_Unstable, false); }
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.25f);
            check(Effects(hud)[EffectType.Mental_Unstable] == unstable && unstable.GetComponent<CanvasGroup>().alpha == 1f
                && Vector3.Distance(unstable.transform.localScale, Vector3.one) < 0.001f,
                string.Empty);
            unstable.GetComponent<StatusEffectIconUI>().OnPointerEnter(null);
            check(Get<RectTransform>(unstable.GetComponent<StatusEffectIconUI>(), "tooltip").gameObject.activeSelf,
                string.Empty);
            unstable.GetComponent<StatusEffectIconUI>().OnPointerExit(null);
            Time.timeScale = 1f;
            player.UpdateMental(1); player.RefreshStatusEffectUI();
            yield return new WaitForSecondsRealtime(0.25f);
            check(!player.IsMentalDebuff() && !Effects(hud).ContainsKey(EffectType.Mental_Unstable), string.Empty);

            Set(player, "drunkDuration", 1.2f);
            var inventory = InventoryManager.Instance;
            var drink = new SItemStack(40009, 3); inventory.itemLists[0] = drink;
            var drinkType = ItemTypeManager.Instance.itemTypeSearch[40009] as SItemConsumeTypeSO;
            Drain(drinkType.Use(player, drink));
            var drunk = Effects(hud)[EffectType.Drunk];
            var timer = Get<Text>(drunk.GetComponent<StatusEffectIconUI>(), "durationText");
            check(player.IsDrunk() && drink.amount == 2 && timer.text == Mathf.CeilToInt(player.DrunkRemaining).ToString(),
                string.Empty);
            int mentalBefore = player.currentMental; player.UpdateMental(-5);
            check(player.currentMental == mentalBefore, string.Empty);
            yield return new WaitForSecondsRealtime(0.3f);
            float beforeRefresh = player.DrunkRemaining;
            Drain(drinkType.Use(player, drink));
            check(player.DrunkRemaining > beforeRefresh && Effects(hud)[EffectType.Drunk] == drunk && drink.amount == 1,
                string.Empty);
            Time.timeScale = 0f; player.RefreshStatusEffectUI();
            float pausedRemaining = player.DrunkRemaining; string pausedText = timer.text;
            yield return new WaitForSecondsRealtime(0.4f);
            check(player.DrunkRemaining == pausedRemaining && timer.text == pausedText, string.Empty);
            Time.timeScale = 1f; yield return new WaitForSecondsRealtime(0.3f);
            check(player.DrunkRemaining < pausedRemaining && player.IsDrunk(), string.Empty);
            yield return new WaitForSecondsRealtime(1.2f);
            check(!player.IsDrunk() && !Effects(hud).ContainsKey(EffectType.Drunk), string.Empty);
            foreach (var unused in new[] {EffectType.Confusion, EffectType.Charm, EffectType.Panic, EffectType.Disarm, EffectType.Lethargy})
            {
                hud.BeginUI(unused, false);
                check(!Effects(hud).ContainsKey(unused), string.Empty);
            }

            Set(player, "drunkDuration", duration); player.StartDrunk(); Fullness(90); player.UpdateMental(39 - player.currentMental);

            player.currentMental = 39; player.RefreshStatusEffectUI();
            yield return new WaitForSecondsRealtime(0.35f);
            var tooltipChecks = TooltipChecks(check);
            while (tooltipChecks.MoveNext()) yield return tooltipChecks.Current;
            Capture(1600, 900, check); Capture(1600, 1000, check);
            Fullness(0); yield return new WaitForSecondsRealtime(0.35f);
            Effects(hud)[EffectType.Fullness_Starving].GetComponent<StatusEffectIconUI>().OnPointerEnter(null);
            Capture(1600, 900, check, "-starving-tooltip");
            hud.gameObject.SetActive(false);
            check(BuffUIManager.Instance == null && Effects(hud).Count == 0, string.Empty);
            yield return null;
            hud.gameObject.SetActive(true); yield return new WaitForSecondsRealtime(0.25f);
            check(BuffUIManager.Instance == hud && Effects(hud).ContainsKey(EffectType.Drunk), string.Empty);
            Set(player, "drunkUntil", Time.time); yield return null; yield return new WaitForSecondsRealtime(0.25f);
        }
        finally
        {
            Time.timeScale = 1f;
            Set(player, "drunkDuration", duration);
            Set(player, "drunkUntil", Time.time);
            Fullness(fullness); player.currentMental = mental; player.RefreshStatusEffectUI();
        }
    }

    private static IEnumerator TooltipChecks(Action<bool, string> check)
    {
        var hud = BuffUIManager.Instance;
        var layer = Get<RectTransform>(hud, "tooltipLayer");
        var root = (RectTransform)hud.transform;
        var canvas = (RectTransform)hud.GetComponentInParent<Canvas>().rootCanvas.transform;
        var originalPosition = root.anchoredPosition;
        var fixtures = new List<StatusEffectIconUI>();
        try
        {
            check(layer != null && layer.GetComponent<LayoutGroup>() == null, string.Empty);
            foreach (var row in new[] { hud.buffParent, hud.debuffParent })
                for (int i = 0; i < 3; i++)
                {
                    var view = Object.Instantiate(hud.effectIconPrefab, row, false).GetComponent<StatusEffectIconUI>();
                    view.SetTooltipLayer(layer);
                    view.Show(EffectType.Drunk, hud.iconDrunk, "만취", "정신력 변화가 일시적으로 멈춥니다.", row == hud.buffParent);
                    fixtures.Add(view);
                }
            yield return new WaitForSecondsRealtime(0.25f);
            Canvas.ForceUpdateCanvases();
            var all = hud.GetComponentsInChildren<StatusEffectIconUI>();
            var positions = all.Select(v => ((RectTransform)v.transform).anchoredPosition).ToArray();
            var indices = all.Select(v => v.transform.GetSiblingIndex()).ToArray();
            foreach (var view in all)
            {
                view.OnPointerEnter(null);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var tip = Get<RectTransform>(view, "tooltip");
                int depth = tip.GetComponent<Image>().canvasRenderer.absoluteDepth;
                check(tip.parent == layer && layer.GetSiblingIndex() == root.childCount - 1
                    && all.All(v => v.GetComponentsInChildren<Graphic>().All(g => g.canvasRenderer.absoluteDepth < depth)),
                    string.Empty);
                check(all.Select((v,i) => v.transform.GetSiblingIndex() == indices[i]
                    && ((RectTransform)v.transform).anchoredPosition == positions[i]).All(v => v), string.Empty);
                view.OnPointerExit(null);
                check(tip.parent == view.transform && !tip.gameObject.activeSelf, string.Empty);
            }

            hud.buffParent.SetSiblingIndex(1);
            var first = all[0]; first.OnPointerEnter(null);
            yield return null;
            check(layer.GetSiblingIndex() == root.childCount - 1, string.Empty);
            hud.buffParent.SetSiblingIndex(0);
            Capture(1600, 900, check, "-left-tooltip-overlay", false);
            Capture(1600, 1000, check, "-left-tooltip-overlay", false);
            first.OnPointerExit(null);
            root.anchoredPosition = new Vector2(canvas.rect.width - 48f, 8f);
            first.OnPointerEnter(null); yield return null; Canvas.ForceUpdateCanvases();
            var tipBounds = Bounds(Get<RectTransform>(first, "tooltip"), canvas);
            var iconBounds = Bounds((RectTransform)first.transform, canvas);
            check(canvas.rect.Contains(tipBounds.min) && canvas.rect.Contains(tipBounds.max)
                && tipBounds.xMax < iconBounds.xMin && tipBounds.yMax < iconBounds.yMin,
                string.Empty);
            first.OnPointerExit(null);
            root.anchoredPosition = originalPosition;
            var last = fixtures.Last(); last.OnPointerEnter(null); last.gameObject.SetActive(false);
            check(!Get<RectTransform>(last, "tooltip").gameObject.activeSelf, string.Empty);
            last.gameObject.SetActive(true);
            last.Show(EffectType.Drunk, hud.iconDrunk, "만취", "정신력 변화가 일시적으로 멈춥니다.", false);
            check(layer.childCount == 0, string.Empty);
            yield return new WaitForSecondsRealtime(0.25f);
            last.OnPointerEnter(null); Object.Destroy(last.gameObject);
            yield return null; yield return null;
            check(layer.childCount == 0, string.Empty);
        }
        finally
        {
            root.anchoredPosition = originalPosition;
            foreach (var view in fixtures) if (view != null) { view.gameObject.SetActive(false); Object.Destroy(view.gameObject); }
        }
        yield return null;
    }

    public static void CheckCleared(Action<bool, string> check, string result)
    {
        var hud = Object.FindObjectsOfType<BuffUIManager>(true).Single();
        check(Effects(hud).Count == 0 && hud.GetComponentsInChildren<StatusEffectIconUI>(true).Length == 0
            && (BuffUIManager.Instance == null || (BuffUIManager.Instance == hud && hud.isActiveAndEnabled)),
            string.Empty);
    }
    private static void Fullness(int value) { PlayerCore.Instance.EatFoodFullness(value - PlayerCore.Instance.currentFullness); }
    private static bool IsFullness(EffectType type) => type == EffectType.Fullness_Full || type == EffectType.Fullness_Hungry || type == EffectType.Fullness_Starving;
    private static void Capture(int width, int height, Action<bool, string> check, string variant = "", bool checkHudObstacles = true)
    {
        var camera = Camera.main;
        var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var canvases = Object.FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray(); var distances = canvases.Select(c => c.planeDistance).ToArray();
        Texture2D image = null;
        try
        {
            camera.targetTexture = rt;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.1f; }
            Canvas.ForceUpdateCanvases();
            var hud = BuffUIManager.Instance; var canvasRect = (RectTransform)hud.GetComponentInParent<Canvas>().transform;
            var views = hud.GetComponentsInChildren<StatusEffectIconUI>();
            foreach (var view in views)
            {
                view.SendMessage("PositionTooltip");
                var tip = Get<RectTransform>(view, "tooltip");
                if (!tip.gameObject.activeSelf) continue;
                var tipBounds = Bounds(tip, canvasRect);
                check(canvasRect.rect.Contains(tipBounds.min) && canvasRect.rect.Contains(tipBounds.max),
                    string.Empty);
            }
            var bounds = views.Select(v => Bounds((RectTransform)v.transform, canvasRect)).ToArray();
            var stats = PlayerStatUI.Instance;
            var obstacles = new List<Rect> {Bounds((RectTransform)stats.hpBar.transform, canvasRect), Bounds((RectTransform)stats.mentalBar.transform, canvasRect),
                Bounds((RectTransform)stats.fullnessBar.transform, canvasRect), Bounds((RectTransform)stats.guiltyBar.transform, canvasRect),
                Bounds((RectTransform)InGameUI.instance.gameObjectClock.transform, canvasRect), Bounds(OceanEventManager.instance.currentEventName.rectTransform, canvasRect)};
            var notice = Get<TextMeshProUGUI>(InGameUI.instance, "actionFeedback");
            if (notice != null) obstacles.Add(Bounds(notice.rectTransform, canvasRect));
            foreach (var slot in Get<List<GameObject>>(InventoryUiMain.instance, "quickSlot")) obstacles.Add(Bounds((RectTransform)slot.transform, canvasRect));
            check(!checkHudObstacles || bounds.All(b => canvasRect.rect.Contains(b.min) && canvasRect.rect.Contains(b.max))
                && bounds.All(b => obstacles.All(o => !b.Overlaps(o))), string.Empty);
            check(bounds.SelectMany((a,i) => bounds.Skip(i+1).Select(b => !a.Overlaps(b))).All(v => v), string.Empty);
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt; image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs/StatusHUD"); File.WriteAllBytes("Logs/StatusHUD/hud-"+width+"x"+height+variant+".png", image.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
            camera.targetTexture = previous; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            if (image != null) Object.Destroy(image); Canvas.ForceUpdateCanvases();
        }
    }
    private static Rect Bounds(RectTransform rect, RectTransform canvas)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(canvas.InverseTransformPoint(corners[0]).x, canvas.InverseTransformPoint(corners[0]).y,
            canvas.InverseTransformPoint(corners[2]).x, canvas.InverseTransformPoint(corners[2]).y);
    }
    private static void Drain(IEnumerator iterator) { while (iterator.MoveNext()) if (iterator.Current is IEnumerator child) Drain(child); }
    private static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(o);
    private static void Set(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(o,value);
}
#endif
