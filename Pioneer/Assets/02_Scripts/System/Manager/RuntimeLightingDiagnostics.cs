#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class RuntimeLightingDiagnostics : MonoBehaviour
{
    [Serializable]
    public struct Sample
    {
        public int frame, day, profileId, sharedProfileId, cameraId, quality;
        public bool daytime, transition, entirePhaseIsTransition, result, volumeEnabled, volumeActive, postProcessing;
        public bool runtimeActive, filterOverride, exposureOverride;
        public float realtime, cycleTime, phaseDuration, transitionDuration, nightBlend, targetExposure, runtimeExposure, stackExposure;
        public float volumeWeight, volumePriority, lightIntensity;
        public Color targetFilter, runtimeFilter, stackFilter, lightColor;
        public string pipeline;
    }

    public static event Action<Sample, string> Sampled;
    public static int AnomalyCount { get; private set; }
    private const float ColorThreshold = 0.08f, ExposureThreshold = 0.2f;
    private Sample previous;
    private GameManager previousManager;
    private bool hasPrevious, previousMismatch;
    private Light mainLight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Sampled = null; AnomalyCount = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject("Runtime Lighting Diagnostics (development only)");
        DontDestroyOnLoad(host);
        host.AddComponent<RuntimeLightingDiagnostics>();
    }

    private void OnEnable() => RenderPipelineManager.endCameraRendering += OnCameraRendered;
    private void OnDisable() => RenderPipelineManager.endCameraRendering -= OnCameraRendered;

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        GameManager game = GameManager.Instance;
        if (game == null || camera != Camera.main || game.postProcessVolume == null) return;
        Volume volume = game.postProcessVolume;
        ColorAdjustments runtime = game.RuntimeColorAdjustments;
        ColorAdjustments stack = VolumeManager.instance.stack.GetComponent<ColorAdjustments>();
        if (runtime == null || stack == null) return;
        camera.TryGetComponent(out UniversalAdditionalCameraData cameraData);
        if (previousManager != game)
        {
            hasPrevious = false;
            previousMismatch = false;
            previousManager = game;
            mainLight = RenderSettings.sun;
            if (mainLight == null)
                foreach (Light light in FindObjectsOfType<Light>())
                    if (light.type == LightType.Directional) { mainLight = light; break; }
        }
        var profile = EffectiveProfile(volume);
        var s = new Sample
        {
            frame = Time.frameCount, realtime = Time.realtimeSinceStartup,
            day = game.currentDay, daytime = game.IsDaytime, cycleTime = game.LightingCycleTime,
            phaseDuration = game.CurrentPhaseDuration, transitionDuration = game.LightingTransitionDuration,
            transition = game.LightingCycleTime >= game.CurrentPhaseDuration - game.LightingTransitionDuration,
            entirePhaseIsTransition = game.LightingTransitionDuration >= game.CurrentPhaseDuration,
            result = game.IsGameResultActive, nightBlend = game.IntendedNightBlend,
            targetFilter = game.IntendedColorFilter, targetExposure = game.IntendedPostExposure,
            runtimeFilter = runtime.colorFilter.value, runtimeExposure = runtime.postExposure.value,
            runtimeActive = runtime.active, filterOverride = runtime.colorFilter.overrideState,
            exposureOverride = runtime.postExposure.overrideState,
            stackFilter = stack.colorFilter.value, stackExposure = stack.postExposure.value,
            volumeEnabled = volume.enabled, volumeActive = volume.gameObject.activeInHierarchy,
            volumeWeight = volume.weight, volumePriority = volume.priority,
            profileId = profile != null ? profile.GetInstanceID() : 0,
            sharedProfileId = volume.sharedProfile != null ? volume.sharedProfile.GetInstanceID() : 0,
            cameraId = camera.GetInstanceID(), postProcessing = cameraData != null && cameraData.renderPostProcessing,
            lightIntensity = mainLight != null ? mainLight.intensity : 0,
            lightColor = mainLight != null ? mainLight.color : Color.white,
            quality = QualitySettings.GetQualityLevel(),
            pipeline = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in"
        };
        string reason = null;
        bool mismatch = ColorDelta(s.targetFilter, s.runtimeFilter) > ColorThreshold
            || Mathf.Abs(s.targetExposure - s.runtimeExposure) > ExposureThreshold
            || ColorDelta(s.targetFilter, s.stackFilter) > ColorThreshold
            || Mathf.Abs(s.targetExposure - s.stackExposure) > ExposureThreshold
            || !s.postProcessing || !s.volumeEnabled || !s.volumeActive || !s.runtimeActive
            || !s.filterOverride || !s.exposureOverride;
        if (hasPrevious)
        {
            // Expected transitions are excluded only when the phase clock actually advances normally.
            bool phaseWrap = previous.daytime != s.daytime && previous.phaseDuration - previous.cycleTime < 0.5f
                && s.cycleTime < 0.5f;
            float expectedAdvance = Time.deltaTime;
            bool clockJump = !phaseWrap && (previous.daytime != s.daytime
                || Mathf.Abs(s.cycleTime - previous.cycleTime - expectedAdvance) > 0.15f
                || Mathf.Abs(s.phaseDuration - previous.phaseDuration) > 0.01f);
            bool stable = !previous.transition && !s.transition;
            if ((stable || clockJump) && (ColorDelta(previous.targetFilter, s.targetFilter) > ColorThreshold
                || Mathf.Abs(previous.targetExposure - s.targetExposure) > ExposureThreshold))
                reason = "A: GameManager target changed outside normal transition / clock was externally changed";
            else if ((stable || clockJump) && (ColorDelta(previous.stackFilter, s.stackFilter) > ColorThreshold
                || Mathf.Abs(previous.stackExposure - s.stackExposure) > ExposureThreshold))
                reason = "B: final stack changed";
            else if ((stable || clockJump) && (ColorDelta(previous.runtimeFilter, s.runtimeFilter) > ColorThreshold
                || Mathf.Abs(previous.runtimeExposure - s.runtimeExposure) > ExposureThreshold))
                reason = "B: runtime ColorAdjustments changed";
            else if (mismatch != previousMismatch)
                reason = mismatch ? "B: target/runtime/final stack or render configuration mismatch" : "B: mismatch recovered";
            else if (previous.profileId != s.profileId || previous.volumeEnabled != s.volumeEnabled
                || previous.volumeActive != s.volumeActive || previous.postProcessing != s.postProcessing
                || Mathf.Abs(previous.volumeWeight - s.volumeWeight) > 0.05f
                || Mathf.Abs(previous.volumePriority - s.volumePriority) > 0.01f
                || Mathf.Abs(previous.lightIntensity - s.lightIntensity) > 0.05f
                || ColorDelta(previous.lightColor, s.lightColor) > ColorThreshold
                || previous.quality != s.quality || previous.pipeline != s.pipeline)
                reason = "Rendering configuration changed";
        }
        else if (mismatch) reason = "Initial target/runtime/final stack mismatch";
        if (reason != null)
        {
            AnomalyCount++;
            Debug.LogWarning("[LightingDiagnostics] " + reason + "\nprevious=" + JsonUtility.ToJson(previous)
                + "\ncurrent=" + JsonUtility.ToJson(s) + "\n" + DescribeVolumes(cameraData), game);
        }
        Sampled?.Invoke(s, reason);
        previous = s; previousMismatch = mismatch; hasPrevious = true;
    }

    public static VolumeProfile EffectiveProfile(Volume volume) => volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
    private static float ColorDelta(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));

    public static string DescribeVolumes(UniversalAdditionalCameraData cameraData = null)
    {
        var text = new StringBuilder("Active Volumes (including volumes excluded by Main Camera mask):");
        foreach (var volume in FindObjectsOfType<Volume>())
        {
            if (!volume.isActiveAndEnabled) continue;
            var profile = EffectiveProfile(volume);
            text.Append($"\n{volume.name} global={volume.isGlobal} layer={volume.gameObject.layer} priority={volume.priority} weight={volume.weight} profile={profile?.name}#{profile?.GetInstanceID()} cameraMask={cameraData?.volumeLayerMask.value}");
            if (profile != null)
                foreach (var component in profile.components) text.Append($" {component.GetType().Name}(active={component.active})");
        }
        return text.ToString();
    }
}
#endif
