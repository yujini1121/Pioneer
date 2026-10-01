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
                    Require(GameManager.Instance.dayDuration == 270 && GameManager.Instance.nightDuration == 90, "Resource balance applies 270 / 90 seconds");
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
                    Option.instance.SetActivateOptionUI();
                    break;
                case 4:
                    Require(Time.timeScale == 0, "ESC menu pauses gameplay");
                    Option.instance.SetDeactivateOptionUI();
                    Option.instance.SetDeactivateEscUI();
                    break;
                case 5:
                    Require(Time.timeScale == 1, "Unscaled menu close resumes gameplay");
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
                    CheckCombat();
                    Call(GameManager.Instance, "SpawnMariner", 1);
                    break;
                case 9:
                    probeMariner = Object.FindObjectOfType<MarinerAI>();
                    if (probeMariner != null && !probeMariner.isActiveAndEnabled) return;
                    Require(probeMariner != null, "Mariner spawns and initializes");
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
                    airborne.ApplyAirborne(1, 0.1f, Vector3.right, 0);
                    OceanEventManager.instance.EndCurrentEvent();
                    Require(!airborne.IsAirborne, "Wind end cancels repeated airborne state");
                    TestOceanStart<OceanEventWaterBloom>();
                    OceanEventManager.instance.EndCurrentEvent();
                    TestOceanStart<OceanEventNormal>();
                    OceanEventManager.instance.EndCurrentEvent();
                    break;
                case 12:
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
                    GameManager.Instance.TriggerGameOver();
                    break;
                case 17:
                    if (!GameManager.Instance.gameOverUI.gameOverPanel.activeInHierarchy) return;
                    Require(Time.timeScale == 0, "Game Over presentation finishes while paused");
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
        Require(sources.All(s => s.volume <= audio.sfxVolume * 0.5f), "Crowded SFX mix has headroom");
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
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Directory.CreateDirectory("Builds/ReleaseCandidate");
        BuildReport build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/ReleaseCandidate/Pioneer.exe",
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
