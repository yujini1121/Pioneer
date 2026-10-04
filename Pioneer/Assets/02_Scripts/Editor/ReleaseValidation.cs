#if UNITY_EDITOR
using System;
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

// Editor-only, repeatable smoke checks. No scenes, balance assets or preferences are saved by the tests.
[InitializeOnLoad]
public static class ReleaseValidation
{
    private const string ActiveKey = "Pioneer.ReleaseValidation.Active";
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

    static ReleaseValidation()
    {
        EditorApplication.delayCall += CheckRequest;
        if (SessionState.GetBool(ActiveKey, false))
        {
            report = JsonUtility.FromJson<Report>(SessionState.GetString(ReportKey, "{}"));
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Tick;
            Application.logMessageReceived += CaptureLog;
        }
    }

    private static void CheckRequest()
    {
        if (!File.Exists(RequestPath) || SessionState.GetBool(ActiveKey, false)) return;
        string request = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        if (request == "build") BuildWindows();
        else Run();
    }

    [MenuItem("Tools/Pioneer/Release validation/Run smoke checks")]
    public static void Run()
    {
        report = new Report { unityVersion = Application.unityVersion, status = "running" };
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            report.status = "blocked: save scenes and exit Play Mode before running validation";
            SaveReport();
            if (Application.isBatchMode) EditorApplication.Exit(2);
            return;
        }
        try
        {
            SessionState.SetString(SetupKey, JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
            Application.logMessageReceived -= CaptureLog;
            Application.logMessageReceived += CaptureLog;
            AuditScenes();
            SessionState.SetInt("Pioneer.ReleaseValidation.Unlock", PlayerPrefs.GetInt("InfiniteModeUnlocked", -1));
            SessionState.SetBool(ActiveKey, true);
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
            report.errors.Add(error.ToString()); report.status = "failed"; SaveReport();
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    private static void AuditScenes()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Require(scenes.SequenceEqual(new[] { "Assets/01_Scenes/Title.unity", "Assets/01_Scenes/1015 Main.unity" }), "Enabled scenes: Title + 1015 Main only");
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
        Require(!report.errors.Any(), "Build scenes and dependent prefabs contain no missing components");
    }

    private static void AuditHierarchy(GameObject root, string path)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            if (missing > 0) report.errors.Add(path + " / " + child.name + ": missing scripts=" + missing);
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
        int stage = SessionState.GetInt(StageKey, 0);
        if (stage == 100 && !EditorApplication.isPlayingOrWillChangePlaymode) { Finish(); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < nextTick) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Smoke check timed out at stage " + stage);
            switch (stage)
            {
                case 0:
                    Require(SceneManager.GetActiveScene().name == "Title", "Title enters Play Mode");
                    Require(Object.FindObjectsOfType<AudioManager>().Length == 1, "One persistent AudioManager");
                    GameModeState.StartNormalMode();
                    SceneController.Instance.LoadScene("1015 Main");
                    break;
                case 1:
                    if (SceneManager.GetActiveScene().name != "1015 Main" || (SceneController.Instance != null && SceneController.Instance.isLoading)) return;
                    break;
                case 2:
                    Require(GameManager.Instance != null && PlayerCore.Instance != null, "Normal Mode scene initializes");
                    Require(CreatureEffect.Instance != null, "Effect pool survives Title to Main");
                    var playerAgent = PlayerCore.Instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    Require(playerAgent != null && playerAgent.isOnNavMesh, "Player agent binds after the scene NavMesh is ready");
                    var balance = Resources.Load<GameBalanceSettings>("GameBalanceSettings");
                    Require(balance != null && GameManager.Instance.dayDuration == balance.dayDuration && GameManager.Instance.nightDuration == balance.nightDuration, "Current RC resource balance applies to the day / night cycle");
                    Require(!GameModeState.IsInfiniteMode && GameManager.Instance.currentDay == 1, "Normal Mode starts on day one");
                    int hp = PlayerCore.Instance.hp;
                    PlayerCore.Instance.TakeDamage(1, null);
                    Require(PlayerCore.Instance.hp == hp - 1, "Environmental damage accepts a null attacker");
                    InGameUI.instance.UseTab();
                    InGameUI.instance.UseTab();
                    InGameUI.instance.UseTab();
                    break;
                case 3:
                    Require(InGameUI.instance.IsPannelExpanded && InGameUI.instance.makeshiftCraftUI.activeSelf, "Rapid inventory close / reopen stays open");
                    Require(InGameUI.instance.uiChunkStack.Select(c => c.id).Distinct().Count() == InGameUI.instance.uiChunkStack.Count, "UI stack has no duplicate IDs");
                    InGameUI.instance.UseTab();
                    Option.instance.SetActivateEscUI();
                    CheckEscMenuLayout();
                    Option.instance.SetActivateOptionUI();
                    var helpButton = Object.FindObjectsOfType<UnityEngine.UI.Button>(true).First(b => b.name == "Help");
                    helpButton.onClick.Invoke();
                    break;
                case 4:
                    Require(Time.timeScale == 0, "ESC menu pauses gameplay");
                    var helpPanel = Get<GameObject>(Option.instance, "helpUI");
                    Require(helpPanel != null && helpPanel.activeInHierarchy && InGameUI.instance.IsOpened(InGameUI.ID_ESC_OPTION_HELP),
                        "ESC Help button opens the connected help panel");
                    Require(helpPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text.Contains("Space")), "Help includes the actual fishing controls");
                    helpPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == "CloseButton").onClick.Invoke();
                    InGameUI.instance.ShowActionFeedback("Important probe", 2);
                    InGameUI.instance.ShowActionFeedback("Ordinary probe", 0);
                    Require(Get<TMPro.TextMeshProUGUI>(InGameUI.instance, "actionFeedback").text == "Important probe", "Ordinary outcome cannot overwrite an important event notice");
                    Option.instance.SetDeactivateOptionUI();
                    Option.instance.SetDeactivateEscUI();
                    break;
                case 5:
                    Require(Time.timeScale == 1, "Unscaled menu close resumes gameplay");
                    Require(!Get<GameObject>(Option.instance, "helpUI").activeSelf, "Help close completes while paused");
                    Require(!Get<TMPro.TextMeshProUGUI>(InGameUI.instance, "actionFeedback").raycastTarget, "Outcome feedback never intercepts gameplay input");
                    CheckAudioAdmission();
                    AudioManager.instance.FadeOutForGameResult(0.1f);
                    Time.timeScale = 0;
                    break;
                case 6:
                    Require(Get<AudioSource>(AudioManager.instance, "bgmPlayer").volume < 0.001f, "Audio fade completes while paused");
                    AudioManager.instance.RestoreRuntimeVolumes();
                    Require(Get<AudioSource>(AudioManager.instance, "bgmPlayer").volume > 0, "Audio volume restores");
                    foreach (AudioSource source in Get<AudioSource[]>(AudioManager.instance, "sfxPlayers")) source.Stop();
                    Time.timeScale = 1;
                    CheckGameplayEconomy();
                    CheckInventoryAndCraft();
                    CheckFishingCancellation();
                    Set(GameManager.Instance, "cycleTime", GameManager.Instance.dayDuration);
                    break;
                case 7:
                    Require(!GameManager.Instance.IsDaytime, "Day transitions to night");
                    Require(Object.FindObjectsOfType<MinionAI>().Length >= 3, "Day-one minions spawn");
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
                    Require(navigationProbe != null && navigationProbe.isOnNavMesh
                        && Vector3.Distance(navigationProbeStart, navigationProbe.nextPosition) > 0.03f,
                        "Agent follows its path again across frames after knockback");
                    Object.Destroy(navigationProbe.gameObject);
                    Require(probeMariner != null, "Mariner spawns and initializes");
                    CheckCrewFishingEdge();
                    TestOceanStart<OceanEventSiren>();
                    var siren = (OceanEventSiren)OceanEventManager.instance.currentEvent;
                    OceanEventManager.instance.BeginCoroutine((System.Collections.IEnumerator)Call(siren, "CharmRoutine", probeMariner));
                    break;
                case 10:
                    Require(probeMariner != null && probeMariner.isCharmed, "Siren charm starts");
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(!probeMariner.isCharmed, "Siren end restores mariner AI (dead=" + probeMariner.IsDead + ", hp=" + probeMariner.hp + ")");
                    TestOceanStart<OceanEventThunder>();
                    break;
                case 11:
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(Get<float>(PlayerCore.Instance, "thunderSpeedMultiplier") == 1f, "Thunder end restores player speed");
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
                        "Repeated gusts cannot restart airborne movement or chain crowd control");
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(!airborne.IsAirborne, "Wind end cancels repeated airborne state");
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
                        "Wind landing completes across frames and restores the NavMesh agent");
                    Call(MarinerManager.Instance, "InfectMariner", probeMariner);
                    break;
                case 13:
                    var infected = Object.FindObjectOfType<InfectedMarinerAI>();
                    Require(infected != null, "Mariner infection transition");
                    Call(infected, "ChangeToZombieAI");
                    Time.timeScale = 10;
                    break;
                case 14:
                    if (Object.FindObjectOfType<ZombieMarinerAI>() == null) return;
                    Time.timeScale = 1;
                    Require(true, "Infected mariner becomes zombie");
                    var crew = Object.Instantiate(Get<GameObject>(GameManager.Instance, "marinerPrefab")).GetComponent<MarinerAI>();
                    int deathsBefore = Get<int>(GuiltySystem.instance, "deadCount");
                    crew.WhenDestroy();
                    Require(Get<int>(GuiltySystem.instance, "deadCount") == deathsBefore + 1, "One mariner death adds guilt once");
                    GameManager.Instance.TriggerGameClear();
                    break;
                case 15:
                    Require(GameManager.Instance.IsGameResultActive && Time.timeScale == 0, "Clear result pauses gameplay");
                    Require(GameManager.Instance.gameOverUI.gameOverPanel.activeInHierarchy, "Clear panel appears");
                    GameModeState.StartInfiniteMode();
                    GameManager.Instance.ResumeFromEndingToInfiniteMode();
                    break;
                case 16:
                    Require(!GameManager.Instance.IsGameResultActive && Time.timeScale == 1 && GameModeState.IsInfiniteMode, "Clear continues into Infinite Mode");
                    Require(!GameManager.Instance.gameOverUI.gameOverPanel.activeSelf, "Result panel hides on continue");
                    cameraBeforeResult = Camera.main.transform.position;
                    var hitSource = new GameObject("Knockback result probe");
                    hitSource.transform.position = PlayerCore.Instance.transform.position - Vector3.right;
                    PlayerCore.Instance.TakeDamage(1, hitSource);
                    Object.Destroy(hitSource);
                    playerBeforeResult = PlayerCore.Instance.transform.position;
                    GameManager.Instance.TriggerGameOver();
                    break;
                case 17:
                    if (!GameManager.Instance.gameOverUI.gameOverPanel.activeInHierarchy) return;
                    Require(Time.timeScale == 0, "Game Over presentation finishes while paused");
                    Require(!PlayerCore.Instance.IsKnockbackActive && Vector3.Distance(playerBeforeResult, PlayerCore.Instance.transform.position) < 0.02f,
                        "Game Over cancels knockback without moving the player");
                    Require(Vector3.Distance(cameraBeforeResult, Camera.main.transform.position) > 0.5f, "Result camera zoom survives Cinemachine LateUpdate");
                    Call(GameManager.Instance.gameOverUI, "RestartGame");
                    break;
                case 18:
                    Require(GameManager.Instance != null && !GameManager.Instance.IsGameResultActive && Time.timeScale == 1, "Restart restores gameplay state");
                    Require(Object.FindObjectsOfType<AudioManager>().Length == 1 && Object.FindObjectsOfType<Option>().Length == 1
                        && Object.FindObjectsOfType<CreatureEffect>().Length == 1, "Restart has no duplicate audio / option / effect objects");
                    Require(Camera.main.GetComponent<CinemachineBrain>().enabled, "Restart restores Cinemachine");
                    GameManager.Instance.gameOverUI.GoToTitle();
                    break;
                case 19:
                    Require(SceneManager.GetActiveScene().name == "Title", "Return to Title");
                    AudioSource bgm = Get<AudioSource>(AudioManager.instance, "bgmPlayer");
                    Require(bgm.clip == AudioManager.instance.bgmClips[(int)AudioManager.BGM.MainTitle] && bgm.volume > 0, "Title BGM and volume restore");
                    EndPlay();
                    return;
            }
            SessionState.SetInt(StageKey, stage + 1);
            nextTick = EditorApplication.timeSinceStartup + (stage == 16 ? 2.2 : 0.6);
            SaveReport();
        }
        catch (Exception error)
        {
            report.errors.Add("Stage " + stage + ": " + error);
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
            "ESC menu contains Continue, Settings, Help and Exit as four separate buttons");
        var corners = new Vector3[4];
        float previousBottom = float.PositiveInfinity;
        for (int i = 0; i < names.Length; i++)
        {
            var button = buttons.First(b => b.name == names[i]);
            ((RectTransform)button.transform).GetWorldCorners(corners);
            float top = corners.Max(c => c.y);
            float bottom = corners.Min(c => c.y);
            Require(top > bottom && top < previousBottom, "ESC button has its own vertical row: " + names[i]);
            previousBottom = bottom;
        }
    }

    private static void CheckAudioAdmission()
    {
        AudioManager audio = AudioManager.instance;
        probeClip = AudioClip.Create("Release validation probe", 44100 * 3, 1, 44100, false);
        var clips = Get<Dictionary<AudioManager.SFX, AudioClip>>(audio, "sfxDictionary");
        clips[AudioManager.SFX.Click] = probeClip;
        for (int i = 0; i < 100; i++) audio.PlaySfx(AudioManager.SFX.Click);
        var sources = Get<AudioSource[]>(audio, "sfxPlayers");
        Require(sources.Count(s => s.isPlaying && s.clip == probeClip) == 1, "100 same-frame UI clicks use one channel");
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].clip = probeClip;
            sources[i].Play();
            Get<int[]>(audio, "channelPriorities")[i] = 0;
            Get<AudioManager.SFX[]>(audio, "channelSfx")[i] = AudioManager.SFX.HeavyRain;
        }
        audio.PlaySfx(AudioManager.SFX.GameOver);
        Require(Get<AudioManager.SFX[]>(audio, "channelSfx").Contains(AudioManager.SFX.GameOver), "Important SFX can replace ambient channel under saturation");
        int[] priorities = Get<int[]>(audio, "channelPriorities");
        Require(sources.Where((s, i) => priorities[i] <= 1).All(s => s.volume <= audio.sfxVolume * 0.5f), "Crowded ordinary SFX mix has headroom");
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
        Require(sources.Count(s => s.isPlaying) <= 8, "Mixed crowd / trap / environment requests respect the global budget");
        var channelIds = Get<AudioManager.SFX[]>(audio, "channelSfx");
        Require(sources.Where((s, i) => s.isPlaying && probeSounds.Take(4).Contains(channelIds[i])).Count() <= 3,
            "Different ordinary enemy attacks share a concurrency limit");
        clips[AudioManager.SFX.Sanity29Down] = probeClip;
        times.Clear();
        audio.PlaySfx(AudioManager.SFX.Sanity29Down);
        audio.PlaySfx(AudioManager.SFX.Thunder);
        audio.PlaySfx(AudioManager.SFX.Hit);
        audio.PlaySfx(AudioManager.SFX.Click);
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Sanity29Down).Any(),
            "Player danger warning survives mixed SFX saturation");
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Thunder).Any(), "Event warning survives mixed SFX saturation");
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Hit).Any(), "Attack contact survives mixed SFX saturation");
        Require(sources.Where((s, i) => s.isPlaying && channelIds[i] == AudioManager.SFX.Click).Any(), "UI confirmation survives mixed SFX saturation");
        Require(sources.Count(s => s.isPlaying) <= 8, "Priority replacement does not grow the global budget");
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
        Require(inventory.Get(recipe.result.id) == before, "Craft refuses missing ingredients at completion");
        foreach (var input in recipe.input) inventory.Add(input.Copy());
        Require(ItemRecipeManager.Instance.CanCraftInInventory(recipe.result.id), "Gameplay recipe ingredients are available");
        CommonUI.instance.Craft(recipe, Array.Empty<GameObject>(), ui);
        Require(inventory.Get(recipe.result.id) >= before + recipe.result.amount, "Craft produces result with ingredients");
        Require(recipe.input.All(i => inventory.Get(i.id) < i.amount), "Craft consumes ingredients");

        int item = recipe.input[0].id;
        int max = ItemTypeManager.Instance.itemTypeSearch[item].maxStack;
        var sourceObject = new GameObject("Validation mariner inventory");
        var storageObject = new GameObject("Validation full storage");
        var source = sourceObject.AddComponent<MarinerInventory>();
        source.enabled = false;
        source.itemLists = new List<SItemStack> { new SItemStack(item, 5) };
        var storage = storageObject.AddComponent<InventoryBase>();
        storage.itemLists = new List<SItemStack> { new SItemStack(item, max - 2) };
        source.TransferAllItemsToStorage(null);
        source.TransferAllItemsToStorage(storage);
        Require(storage.Get(item) == max && source.Get(item) == 3, "Partial storage transfer preserves exact remaining count");
        source.TransferAllItemsToStorage(storage);
        Require(source.Get(item) == 3, "Retrying full storage does not duplicate items");
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
            "Every active recipe has reachable ingredients through fishing, treasure, enemy drops or crafting");
        var stats = PlayerStatsLevel.Instance;
        Require(stats.fishingList.Count > 5 && stats.fishingList[5].count > 0f
            && Enumerable.Range(1, 5).All(i => stats.fishingList[i].count >= stats.fishingList[i - 1].count
                && stats.fishingList[i].chest >= stats.fishingList[i - 1].chest
                && stats.craftingList[i] >= stats.craftingList[i - 1]
                && stats.combatList[i].attack >= stats.combatList[i - 1].attack),
            "All growth rewards are nondecreasing and Fishing Lv.5 keeps its bonuses");
        int oldLevel = stats.growStates[GrowStatType.Combat].level;
        float first = stats.CombatDamageMultiplier;
        stats.growStates[GrowStatType.Combat].level = 5;
        Require(Mathf.RoundToInt(15f * stats.CombatDamageMultiplier) > Mathf.RoundToInt(15f * first),
            "Combat growth raises actual equipped weapon damage");
        stats.growStates[GrowStatType.Combat].level = oldLevel;
        Require((int)Call(GameManager.Instance, "CalcMarinerEmbarkCount", 99, 5) == 0
            && (int)Call(GameManager.Instance, "CalcMarinerEmbarkCount", 99, 4) == 1,
            "Infinite Mode crew replenishes below, but never above, five members");
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
            // Preserve authored crawler scale, capsule and visual height; never enlarge its hitbox.
            crawler.transform.position = player.transform.position + Vector3.right * 1.4f + Vector3.up * 1.15f;
            Physics.SyncTransforms();
            Require(attack.HasEnemyInDirection(Vector3.right, 1.5f), "Crawler at weapon reach can start an attack despite its height");
            attack.transform.position = new Vector3(player.transform.position.x + 0.75f, player.AttackHeight, player.transform.position.z);
            attack.transform.rotation = Quaternion.LookRotation(Vector3.right);
            attack.SetAttackRange(1.5f);
            var box = (BoxCollider)attack.attackCollider;
            var hits = Physics.OverlapBox(box.transform.TransformPoint(box.center),
                Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f, box.transform.rotation,
                player.EnemyLayer, QueryTriggerInteraction.Ignore);
            Require(hits.Any(h => h.GetComponentInParent<CrawlerAI>() == ai),
                "Actual weapon box reaches the same crawler accepted by the input check");
            attack.damage = 1; attack.EnableAttackCollider();
            foreach (var hit in hits) Call(attack, "TryDealDamage", hit);
            Require(ai.hp == 49, "Crawler receives one damage application with its authored collider");
            attack.DisableAttackCollider();
            crawler.transform.position = player.transform.position + Vector3.right * 3f;
            Physics.SyncTransforms();
            Require(!attack.HasEnemyInDirection(Vector3.right, 1.5f), "Out-of-reach crawler does not pass the attack check");
            var trapObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/06_Modelings/50007 - SpikeTrap/PF_SpikeTrap.prefab"));
            try
            {
                var trap = trapObject.GetComponent<SpikeTrap>();
                Set(trap, "isTriggerd", true); Set(trap, "spikesRaised", true);
                int hp = ai.hp;
                Call(trap, "OnTriggerStay", crawler.GetComponent<Collider>());
                Call(trap, "OnTriggerStay", crawler.GetComponent<Collider>());
                Require(ai.hp == hp - 10, "Raised trap deals damage once per enemy per interval");
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
            "Crew edge test has a navigable deck");
        Vector3 oldPosition = probeMariner.transform.position;
        agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
        Vector3 edge = (Vector3)Call(probeMariner, "FindMyOwnEdgePoint");
        Require(edge != Vector3.zero, "Crew finds a reachable exterior platform edge");
        Vector3 sea = Get<Vector3>(probeMariner, "personalSeaDirection");
        Require(!Physics.Raycast(edge + sea * 1.2f + Vector3.up * 3f, Vector3.down, 6f, LayerMask.GetMask("Platform")),
            "Crew casts toward open sea rather than through a platform");
        agent.Warp(edge + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
        Require((bool)Call(probeMariner, "HasSeaAtFishingPoint"), "Crew validates its fishing direction after arriving");
        agent.Warp(oldPosition);
    }

    private static void CheckFishingCancellation()
    {
        var fishing = PlayerFishing.instance;
        Require(fishing != null && fishing.fishingEventUI != null, "Fishing QTE references are connected");
        fishing.StartFishingLoop();
        Coroutine first = Get<Coroutine>(fishing, "fishingLoopCoroutine");
        fishing.StartFishingLoop();
        Require(first != null && first == Get<Coroutine>(fishing, "fishingLoopCoroutine"), "Repeated fishing start keeps one coroutine");
        fishing.StopFishingLoop();
        var ui = fishing.fishingEventUI;
        float chance = Get<float>(fishing, "eventChance");
        Set(fishing, "eventChance", 0f); Set(fishing, "nonEventCount", 0);
        int beforeItems = InventoryManager.Instance.GetAllItem();
        var attempt = (System.Collections.IEnumerator)Call(fishing, "FishingLoop");
        Require(attempt.MoveNext() && attempt.Current is WaitForSeconds, "A cast waits before resolving");
        Require(!attempt.MoveNext() && PlayerCore.Instance.currentState == PlayerCore.PlayerState.Default
            && Get<Coroutine>(fishing, "fishingLoopCoroutine") == null && InventoryManager.Instance.GetAllItem() > beforeItems,
            "Normal cast awards its catch and ends without automatic recasting");
        Set(fishing, "eventChance", 1f);
        attempt = (System.Collections.IEnumerator)Call(fishing, "FishingLoop");
        attempt.MoveNext();
        Require(attempt.MoveNext() && attempt.Current is WaitForSeconds && !ui.fishingEvent_UI.activeSelf,
            "Special bite has a separate warning wait before the QTE appears");
        // Resolving an unsuccessful special bite must end this cast with no reward.
        int beforeFailure = InventoryManager.Instance.GetAllItem();
        Require(attempt.MoveNext() && attempt.Current is System.Collections.IEnumerator,
            "Special bite starts the QTE only after the warning");
        Require(!attempt.MoveNext() && InventoryManager.Instance.GetAllItem() == beforeFailure
            && PlayerCore.Instance.currentState == PlayerCore.PlayerState.Default,
            "Failed special bite gives no reward and returns to normal controls");
        fishing.StopFishingLoop();
        Set(fishing, "eventChance", chance);
        var treasure = TreasureBoxManager.instance;
        var rewards = Get<List<SItemStack>>(treasure, "rewardStack");
        while (rewards.Count > 0) treasure.Accept();
        var beforeReward = ItemTypeManager.Instance.itemTypeSearch.Keys.ToDictionary(id => id, id => InventoryManager.Instance.Get(id));
        treasure.GetSpecialBox();
        var granted = rewards[0].Copy();
        Require(granted.id != 30001 && InventoryManager.Instance.Get(granted.id) == beforeReward[granted.id] + granted.amount,
            "Special salvage grants a non-wood treasure reward immediately");
        treasure.Accept();
        Require(InventoryManager.Instance.Get(granted.id) == beforeReward[granted.id] + granted.amount,
            "Dismissing the treasure reveal cannot grant the reward twice");
        bool called = false;
        var qte = ui.StartQTE(success => called = true);
        Require(qte.MoveNext(), "Fishing QTE starts");
        float value = ui.slider.value;
        Time.timeScale = 0;
        qte.MoveNext();
        Require(ui.slider.value == value, "Fishing QTE freezes while paused");
        ui.CloseUI();
        qte.MoveNext();
        Time.timeScale = 1;
        Require(!called && !ui.fishingEvent_UI.activeSelf && Get<Coroutine>(fishing, "fishingLoopCoroutine") == null,
            "Fishing cancellation closes QTE without granting a catch");
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
        Require(target.hp == before - 1, "One attack damages a target once despite repeated trigger callbacks");
        attack.DisableAttackCollider();
        Call(attack, "TryDealDamage", collider);
        Require(target.hp == before - 1, "Disabled attack window cannot deal damage");
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
        var source = new GameObject("Knockback direction probe");
        Vector3 oldPlayerPosition = player.transform.position;
        Vector3 oldTargetPosition = target.transform.position;
        int oldPlayerHp = player.hp;
        int oldTargetHp = target.hp;
        bool oldStopped = agent.isStopped;
        bool oldPlayerStopped = playerAgent.isStopped;
        try
        {
            Require(UnityEngine.AI.NavMesh.SamplePosition(oldPlayerPosition - Vector3.up * playerAgent.baseOffset, out var center, 0.5f, playerAgent.areaMask), "Knockback test has a navigable deck");
            var body = player.GetComponent<Rigidbody>();
            source.transform.position = body.position - Vector3.right;
            Vector3 before = body.position;
            player.TakeDamage(1, source);
            Require(player.IsKnockbackActive, "Player damage starts a short knockback");
            StepKnockback(player, 0f);
            Require(Vector3.Distance(before, body.position) < 0.001f, "Paused knockback does not move");
            StepKnockback(player, 0.12f);
            Require(!player.IsKnockbackActive && Vector3.Distance(before, body.position) > 0.02f
                && Vector3.Distance(before, body.position) <= 0.19f, "Player knockback is short and finishes: from=" + before + " to=" + player.transform.position + " body=" + player.GetComponent<Rigidbody>().position + " agent=" + playerAgent.nextPosition + " baseOffset=" + playerAgent.baseOffset + " active=" + player.IsKnockbackActive);
            player.Move(Vector3.right);
            Require(player.GetComponent<Rigidbody>().velocity.x > 0 && playerAgent.isStopped == oldPlayerStopped,
                "Player movement resumes and agent stop ownership is preserved");
            player.StopHorizontalMovement();
            player.TakeDamage(1, null);
            Require(!player.IsKnockbackActive, "Environmental damage has no invented knockback direction");

            // Isolate ordinary movement from random shoreline spawn positions.
            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            agent.isStopped = true;
            source.transform.position = target.transform.position - Vector3.right;
            target.hp = 100;
            before = target.transform.position;
            for (int i = 0; i < 20; i++) target.TakeDamage(1, source);
            StepKnockback(target, 0.12f);
            Require(target.hp == 80 && !target.IsKnockbackActive && Vector3.Distance(before, target.transform.position) > 0.02f && Vector3.Distance(before, target.transform.position) <= 0.29f,
                "Repeated hits replace one knockback and preserve every damage application: hp=" + target.hp + " from=" + before + " to=" + target.transform.position + " active=" + target.IsKnockbackActive);
            Require(agent.isOnNavMesh && agent.isStopped && agent.updatePosition, "Enemy knockback preserves the agent and attack stop state");
            var stun = target.GetComponent<StunHandler>() ?? target.gameObject.AddComponent<StunHandler>();
            stun.ApplyStun(2f);
            target.TakeDamage(1, source);
            StepKnockback(target, 0.12f);
            Require(stun.IsStunned && agent.isStopped, "Knockback does not clear an existing stun");
            stun.ClearStun();
            Require(!agent.isStopped, "Stun still releases movement after knockback");
            agent.SetDestination(center.position);
            Require(agent.isOnNavMesh && agent.enabled && agent.updatePosition, "Enemy can navigate again after knockback");

            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            if (agent.Raycast(center.position + Vector3.right * 100f, out var edge))
            {
                agent.Warp(edge.position - Vector3.right * 0.03f + Vector3.up * agent.baseOffset);
                // Let the agent settle onto its surface after the test-only warp.
                agent.Move(Vector3.zero);
                before = agent.nextPosition;
                source.transform.position = before - Vector3.right;
                target.TakeDamage(1, source);
                StepKnockback(target, 0.12f);
                Require(agent.isOnNavMesh && !UnityEngine.AI.NavMesh.Raycast(before, agent.nextPosition, out _, agent.areaMask)
                    && Vector3.Distance(before, agent.nextPosition) <= 0.29f, "Deck boundary blocks knockback without crossing water");
            }
            else throw new Exception("Could not find deck boundary for knockback check");
            target.TakeDamage(1, source);
            target.IsDead = true;
            before = target.transform.position;
            StepKnockback(target, 0.12f);
            Require(!target.IsKnockbackActive && Vector3.Distance(before, target.transform.position) < 0.001f,
                "Death cancels an in-flight knockback");
            target.IsDead = false;
            agent.Warp(center.position + Vector3.up * (agent.baseOffset * Mathf.Abs(agent.transform.lossyScale.y)));
            var airborne = target.GetComponent<WindAirborne>() ?? target.gameObject.AddComponent<WindAirborne>();
            airborne.ApplyAirborne(0.1f, 0.1f, Vector3.right, 0f);
            target.TakeDamage(1, source);
            Require(!target.IsKnockbackActive, "Wind airborne movement owns position during a hit");
            airborne.CancelAirborne();
            var deathProbe = new GameObject("Lethal knockback probe");
            deathProbe.transform.position = center.position;
            deathProbe.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var creature = deathProbe.AddComponent<CreatureBase>();
            creature.hp = 2;
            creature.TakeDamage(1, source);
            creature.TakeDamage(1, source);
            before = creature.transform.position;
            StepKnockback(creature, 0.12f);
            Require(creature.IsDead && !creature.IsKnockbackActive && creature.transform.position == before,
                "Lethal damage ends knockback before deferred destruction");
            target.TakeDamage(1, source);
            target.enabled = false;
            Require(!target.IsKnockbackActive, "Disabling an enemy clears knockback state");
            target.enabled = true;
            var walker = new GameObject("Post-knockback navigation probe");
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
        Require(oceanEvent.IsRunning, typeof(T).Name + " starts");
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
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        SessionState.SetBool(ActiveKey, false);
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

    [MenuItem("Tools/Pioneer/Release validation/Build Windows candidate")]
    public static void BuildWindows() => BuildWindowsAt("Builds/ReleaseCandidate");

    [MenuItem("Tools/Pioneer/Release validation/Build Final Polish")]
    public static void BuildFinalPolish() => BuildWindowsAt("Builds/FinalPolish");

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
        File.WriteAllText("Logs/ReleaseValidation/build-result.txt", build.summary.result + "\nErrors: " + build.summary.totalErrors
            + "\nWarnings: " + build.summary.totalWarnings + "\nSize: " + build.summary.totalSize);
        File.WriteAllLines("Logs/ReleaseValidation/build-messages.txt", build.steps.SelectMany(s => s.messages)
            .Where(m => m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Warning)
            .Select(m => m.type + ": " + m.content));
        if (Application.isBatchMode) EditorApplication.Exit(build.summary.result == BuildResult.Succeeded && build.summary.totalErrors == 0 ? 0 : 1);
    }
}
#endif
