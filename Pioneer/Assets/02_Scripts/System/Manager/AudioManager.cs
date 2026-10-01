using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour, IBegin
{
    public static AudioManager instance;

    [System.Serializable]
    public class SoundSFX
    {
        public AudioManager.SFX sfx;
        public AudioClip sfxClips;
    }       

    /// <summary>
    /// 배경 음악 종류 (인스펙터 창이랑 순서 꼭 맞추기)
    /// </summary>
    public enum BGM
    {
        MainTitle,
        Morning,
        Night,
        MistOfTheDead,
        Siren,
        Thunderstorm,
    }

    /// <summary>
    /// 현재 효과음 종류 (인스펙터 창이랑 순서 꼭 맞추기)
    /// </summary>
    /// <returns></returns>
    public enum SFX
    {
        GameStartButton = 0,
        SamshSound = 1,
        GameOver = 2,
        Die = 3,
        Click = 4,
        AfterAttack_Minion = 5,
        AfterAttack_Titan = 6,
        AfterAttack_Crawler = 7,
        BeforeAttack_Minion = 8,
        BeforeAttack_Titan = 9,
        BeforeAttack_Crawler = 10,
        Hunger = 11,
        Sanity29Down = 12,
        LevelUp = 13,
        SelectQuickSlot = 14,
        RemoveItem = 15,
        ArrayItem = 16,
        EatingFood = 17,
        Drink = 18,
        UseComsumpitem = 19,
        BeforeFishing = 20,
        GetFishing = 21,
        OpenBox = 22,
        SuccessCrafting = 23,
        GreatSuccessCrafting = 24,
        InstallingObject = 25,
        InstallObject = 26,
        RotateInstallTypeObject = 27,
        DestroyedObject = 28,
        Hit_Object = 29,
        BalistaAttack = 30,
        ActivatedSpiketrap = 31,
        BeforeAttack_BlackFog = 32,
        AfterAttack_BlackFog = 33,
        Scream2 = 34,
        CantESCNoise = 35,
        LaughSaren = 36,
        Hurricane = 37,
        HeavyRain = 38,
        Thunder = 39,
        FortifyObject = 40,
        ItemGet = 41,
        ToNight = 42,
        MeetEnemy = 43,
        Punch1_Player = 44,
        Hit = 45,
        Take = 46,
        Hit2 = 48,
        GreatSuccessCrafting2 = 49,
        meetEnemy2 = 50,
        Punch3_Player = 51,
        SuccessCrafting2 = 52,
        To_night2 = 53,
        grunt_effort_struggle_male_b_17 = 54
    }

    [Header("Vol UI")]
    // public Slider masterVolSlider;
    public Slider bgmVolSlider;
    public Slider sfxVolSlider;

    [Header("Audio Mixer 설정")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixer;
    public AudioMixerGroup sfxMixer;

    [Header("BGM 설정")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    private AudioSource bgmPlayer;

    [Header("SFX 설정 !! 사운드 추가는 인스펙터와 SFX Enum에 둘 다 추가해야합니다 !!")]
    //public AudioClip[] sfxClips;
    public List<SoundSFX> sfxSoundList;
    public float sfxVolume;
    public int sfxChannels;
    private AudioSource[] sfxPlayers;
    private int sfxChannelIndex;

    private Dictionary<SFX, AudioClip> sfxDictionary;
    private Coroutine gameResultFadeCoroutine;
    private float runtimeBgmVolumeBeforeFade;
    private float[] runtimeSfxVolumesBeforeFade;
    private bool hasRuntimeVolumesBeforeFade;
    private readonly Dictionary<SFX, float> lastSfxTimes = new Dictionary<SFX, float>();
    private SFX[] channelSfx;
    private int[] channelPriorities;
    private float[] channelStartTimes;
    private float sfxFadeScale = 1f;
    private const int GlobalSfxBudget = 8;
    // Runtime categories only; serialized SFX enum values are unchanged.
    private enum SfxGroup { General, PlayerAlert, PlayerAttack, EnemyAttack, Trap, UI, Environment, Event, Interaction }

    private static SfxGroup GetSfxGroup(SFX sfx)
    {
        switch (sfx)
        {
            case SFX.Sanity29Down: return SfxGroup.PlayerAlert;
            case SFX.SamshSound: case SFX.Punch1_Player: case SFX.Punch3_Player:
            case SFX.Hit: case SFX.Hit2: return SfxGroup.PlayerAttack;
            case SFX.BeforeAttack_Minion: case SFX.AfterAttack_Minion:
            case SFX.BeforeAttack_Crawler: case SFX.AfterAttack_Crawler:
            case SFX.BeforeAttack_Titan: case SFX.AfterAttack_Titan:
            case SFX.BeforeAttack_BlackFog: case SFX.AfterAttack_BlackFog: return SfxGroup.EnemyAttack;
            case SFX.BalistaAttack: case SFX.ActivatedSpiketrap: return SfxGroup.Trap;
            case SFX.Click: case SFX.SelectQuickSlot: case SFX.RotateInstallTypeObject:
            case SFX.RemoveItem: case SFX.ArrayItem: return SfxGroup.UI;
            case SFX.Hurricane: case SFX.HeavyRain: return SfxGroup.Environment;
            case SFX.GameOver: case SFX.GameStartButton: case SFX.ToNight: case SFX.To_night2:
            case SFX.Hunger: case SFX.MeetEnemy: case SFX.meetEnemy2:
            case SFX.Scream2: case SFX.LaughSaren: case SFX.Thunder: return SfxGroup.Event;
            case SFX.SuccessCrafting: case SFX.SuccessCrafting2: case SFX.GreatSuccessCrafting:
            case SFX.GreatSuccessCrafting2: case SFX.InstallObject: case SFX.FortifyObject:
            case SFX.GetFishing: case SFX.OpenBox: case SFX.LevelUp: return SfxGroup.Interaction;
            default: return SfxGroup.General;
        }
    }

    private static int GetGroupLimit(SfxGroup group)
    {
        if (group == SfxGroup.PlayerAlert) return 1;
        return group == SfxGroup.PlayerAttack || group == SfxGroup.EnemyAttack ? 3 : 2;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        LoadVolumes();
        if (SceneManager.GetActiveScene().name == "Title")
            PlayBgm(BGM.MainTitle);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        foreach (AudioSource source in sfxPlayers)
            if (source != null) source.Stop();
        lastSfxTimes.Clear();
        RestoreRuntimeVolumes();
        if (scene.name == "Title") PlayBgm(BGM.MainTitle);
    }

    private void LateUpdate()
    {
        UpdateSfxMix();
    }

    private void UpdateSfxMix()
    {
        if (sfxPlayers == null) return;
        int playing = 0;
        bool important = false;
        for (int i = 0; i < sfxPlayers.Length; i++)
            if (sfxPlayers[i] != null && sfxPlayers[i].isPlaying)
            {
                playing++;
                important |= channelPriorities[i] >= 3;
            }

        // Keep a little headroom when a crowd attacks, without changing the mixer preference.
        float gain = 1f / Mathf.Sqrt(Mathf.Max(1f, playing / 4f));
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            if (sfxPlayers[i] == null) continue;
            float volume = hasRuntimeVolumesBeforeFade && runtimeSfxVolumesBeforeFade != null
                ? runtimeSfxVolumesBeforeFade[i] : sfxVolume;
            float channelGain = channelPriorities[i] >= 3 ? 1f : gain;
            if (important && channelPriorities[i] <= 1) channelGain *= 0.65f;
            sfxPlayers[i].volume = volume * sfxFadeScale * channelGain;
        }
    }

    /// <summary>
    /// 오디오 플레이어 초기화
    /// </summary>
    /// <returns></returns>
    void Init()
    {
        // BGM Player 초기화
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.outputAudioMixerGroup = bgmMixer;

        // SFX Player 초기화
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxChannels = Mathf.Clamp(sfxChannels, 1, 64);
        sfxPlayers = new AudioSource[sfxChannels];
        channelSfx = new SFX[sfxChannels];
        channelPriorities = new int[sfxChannels];
        channelStartTimes = new float[sfxChannels];
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
            sfxPlayers[index].outputAudioMixerGroup = sfxMixer;
        }

        sfxDictionary = new Dictionary<SFX, AudioClip>();
        if (sfxSoundList == null) return;
        foreach (SoundSFX pair in sfxSoundList)
        {
            if (pair == null) continue;
            if (!sfxDictionary.ContainsKey(pair.sfx))
            {
                sfxDictionary.Add(pair.sfx, pair.sfxClips);
            }
            else
            {
                Debug.LogWarning($"AudioManager: {pair.sfx} 키가 sfxSoundList에 중복으로 존재합니다.");
            }
        }
    }

    /// <summary>
    /// 슬라이더 초기화 (오디오 믹서의 현재 값 반영)
    /// </summary>
    /// <returns></returns>
    public void InitSliders()
    {
        if (audioMixer == null) return;
        float volume;

        if (bgmVolSlider != null && audioMixer.GetFloat("BGMVol", out volume))
        {
            bgmVolSlider.SetValueWithoutNotify(Mathf.Pow(10, volume / 20));
        }

        if (sfxVolSlider != null && audioMixer.GetFloat("SFXVol", out volume))
        {
            sfxVolSlider.SetValueWithoutNotify(Mathf.Pow(10, volume / 20));
        }
    }

    public void InitListenerVolSliders()
    {
        // masterVolSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmVolSlider != null)
        {
            bgmVolSlider.onValueChanged.RemoveListener(SetBgmVolume);
            bgmVolSlider.onValueChanged.AddListener(SetBgmVolume);
        }
        if (sfxVolSlider != null)
        {
            sfxVolSlider.onValueChanged.RemoveListener(SetSfxVolume);
            sfxVolSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    public void PlayBgm(BGM bgm)
    {
        int bgmIndex = (int)bgm;

        if (bgmPlayer == null || bgmClips == null || bgmIndex < 0 || bgmIndex >= bgmClips.Length)
        {
            Debug.LogWarning($"PlayBgm: {bgm.ToString()}에 해당하는 bgmClip이 없습니다.");
            return;
        }

        AudioClip newClip = bgmClips[bgmIndex];
        if (newClip == null) return;

        if (bgmPlayer.clip != newClip || !bgmPlayer.isPlaying)
        {
            bgmPlayer.Stop();
            bgmPlayer.clip = newClip;
            bgmPlayer.Play();
        }
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 멈춥니다.
    /// </summary>
    public void StopBgm()
    {
        if (bgmPlayer != null) bgmPlayer.Stop();
    }

    public void FadeOutForGameResult(float duration)
    {
        if (!hasRuntimeVolumesBeforeFade)
        {
            runtimeBgmVolumeBeforeFade = bgmPlayer != null ? bgmPlayer.volume : bgmVolume;
            runtimeSfxVolumesBeforeFade = GetSfxVolumes();
            hasRuntimeVolumesBeforeFade = true;
        }

        if (gameResultFadeCoroutine != null)
            StopCoroutine(gameResultFadeCoroutine);

        gameResultFadeCoroutine = StartCoroutine(FadeOutForGameResultCoroutine(Mathf.Max(0.01f, duration)));
    }

    public void RestoreRuntimeVolumes()
    {
        sfxFadeScale = 1f;
        if (gameResultFadeCoroutine != null)
        {
            StopCoroutine(gameResultFadeCoroutine);
            gameResultFadeCoroutine = null;
        }

        if (bgmPlayer != null)
            bgmPlayer.volume = hasRuntimeVolumesBeforeFade ? runtimeBgmVolumeBeforeFade : bgmVolume;

        if (sfxPlayers == null)
        {
            runtimeSfxVolumesBeforeFade = null;
            hasRuntimeVolumesBeforeFade = false;
            return;
        }

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            if (sfxPlayers[i] != null)
            {
                bool hasStoredVolume = hasRuntimeVolumesBeforeFade
                    && runtimeSfxVolumesBeforeFade != null
                    && i < runtimeSfxVolumesBeforeFade.Length;

                sfxPlayers[i].volume = hasStoredVolume ? runtimeSfxVolumesBeforeFade[i] : sfxVolume;
            }
        }

        runtimeSfxVolumesBeforeFade = null;
        hasRuntimeVolumesBeforeFade = false;
        UpdateSfxMix();
    }

    private IEnumerator FadeOutForGameResultCoroutine(float duration)
    {
        float elapsed = 0f;
        float startBgmVolume = bgmPlayer != null ? bgmPlayer.volume : 0f;
        float startSfxScale = sfxFadeScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float volumeScale = 1f - SmoothStep01(t);

            if (bgmPlayer != null)
                bgmPlayer.volume = startBgmVolume * volumeScale;

            sfxFadeScale = startSfxScale * volumeScale;
            UpdateSfxMix();

            yield return null;
        }

        if (bgmPlayer != null)
            bgmPlayer.volume = 0f;

        sfxFadeScale = 0f;
        UpdateSfxMix();
        gameResultFadeCoroutine = null;
    }

    private float[] GetSfxVolumes()
    {
        if (sfxPlayers == null)
            return new float[0];

        float[] volumes = new float[sfxPlayers.Length];
        for (int i = 0; i < sfxPlayers.Length; i++)
            volumes[i] = sfxVolume;

        return volumes;
    }

    private void ApplySfxVolumeScale(float[] startVolumes, float volumeScale)
    {
        if (sfxPlayers == null || startVolumes == null)
            return;

        int count = Mathf.Min(sfxPlayers.Length, startVolumes.Length);
        for (int i = 0; i < count; i++)
        {
            if (sfxPlayers[i] != null)
                sfxPlayers[i].volume = startVolumes[i] * volumeScale;
        }
    }

    private static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// SFX 재생 
    /// </summary>
    /// <returns></returns>
    public void PlaySfx(SFX sfx)
    {
        if (sfxDictionary == null || sfxPlayers == null || sfxPlayers.Length == 0
            || !sfxDictionary.TryGetValue(sfx, out AudioClip clipToPlay) || clipToPlay == null)
            return;

        GetSfxLimits(sfx, out float cooldown, out int maxConcurrent, out int priority);
        float now = Time.unscaledTime;
        if (lastSfxTimes.TryGetValue(sfx, out float lastTime) && now - lastTime < cooldown)
            return;

        SfxGroup group = GetSfxGroup(sfx);
        int playing = 0;
        int groupCount = 0;
        int groupReplace = -1;
        int sameReplace = -1;
        int concurrent = 0;
        int free = -1;
        int replace = -1;
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + sfxChannelIndex) % sfxPlayers.Length;
            AudioSource source = sfxPlayers[loopIndex];
            if (source == null) continue;
            if (!source.isPlaying)
            {
                if (free < 0) free = loopIndex;
                continue;
            }
            playing++;
            if (channelSfx[loopIndex] == sfx)
            {
                concurrent++;
                if (sameReplace < 0 || channelStartTimes[loopIndex] < channelStartTimes[sameReplace]) sameReplace = loopIndex;
            }
            if (GetSfxGroup(channelSfx[loopIndex]) == group)
            {
                groupCount++;
                if (channelPriorities[loopIndex] <= priority && (groupReplace < 0
                    || channelPriorities[loopIndex] < channelPriorities[groupReplace]
                    || (channelPriorities[loopIndex] == channelPriorities[groupReplace]
                        && channelStartTimes[loopIndex] < channelStartTimes[groupReplace]))) groupReplace = loopIndex;
            }
            if (channelPriorities[loopIndex] < priority && (replace < 0
                || channelPriorities[loopIndex] < channelPriorities[replace]
                || (channelPriorities[loopIndex] == channelPriorities[replace]
                    && channelStartTimes[loopIndex] < channelStartTimes[replace])))
                replace = loopIndex;
        }
        int selected;
        if (concurrent >= maxConcurrent)
        {
            if (priority < 3) return;
            selected = sameReplace;
        }
        else if (groupCount >= GetGroupLimit(group)) selected = groupReplace;
        else selected = playing < GlobalSfxBudget && free >= 0 ? free : replace;
        if (selected < 0) return;

        sfxPlayers[selected].Stop();
        channelSfx[selected] = sfx;
        channelPriorities[selected] = priority;
        channelStartTimes[selected] = now;
        lastSfxTimes[sfx] = now;
        sfxChannelIndex = (selected + 1) % sfxPlayers.Length;
        sfxPlayers[selected].clip = clipToPlay;
        sfxPlayers[selected].Play();
        UpdateSfxMix();
    }

    private static void GetSfxLimits(SFX sfx, out float cooldown, out int maxConcurrent, out int priority)
    {
        cooldown = 0.05f;
        maxConcurrent = 2;
        priority = 1;
        switch (sfx)
        {
            case SFX.Click:
            case SFX.SelectQuickSlot:
            case SFX.RotateInstallTypeObject:
                cooldown = 0.06f; maxConcurrent = 1; priority = 2; break;
            case SFX.SamshSound:
            case SFX.Punch1_Player:
            case SFX.Punch3_Player:
            case SFX.Hit:
            case SFX.Hit2:
                cooldown = 0.025f; maxConcurrent = 3; priority = 2; break;
            case SFX.BeforeAttack_Minion:
            case SFX.BeforeAttack_Crawler:
            case SFX.AfterAttack_Minion:
            case SFX.AfterAttack_Crawler:
            case SFX.ActivatedSpiketrap:
                cooldown = 0.035f; maxConcurrent = 3; break;
            case SFX.BeforeAttack_Titan:
            case SFX.AfterAttack_Titan:
            case SFX.BalistaAttack:
                cooldown = 0.06f; maxConcurrent = 2; priority = 2; break;
            case SFX.GameStartButton:
            case SFX.LevelUp:
            case SFX.ToNight:
            case SFX.To_night2:
            case SFX.MeetEnemy:
            case SFX.meetEnemy2:
                cooldown = 0.12f; maxConcurrent = 1; priority = 3; break;
            case SFX.GameOver:
            case SFX.Sanity29Down:
                cooldown = 0.08f; maxConcurrent = 1; priority = 4; break;
            case SFX.Scream2:
            case SFX.LaughSaren:
            case SFX.Thunder:
            case SFX.Hunger:
                cooldown = 0.15f; maxConcurrent = 1; priority = 3; break;
            case SFX.SuccessCrafting:
            case SFX.SuccessCrafting2:
            case SFX.GreatSuccessCrafting:
            case SFX.GreatSuccessCrafting2:
            case SFX.InstallObject:
            case SFX.FortifyObject:
            case SFX.GetFishing:
            case SFX.OpenBox:
                cooldown = 0.06f; maxConcurrent = 1; priority = 2; break;
            case SFX.Hurricane:
            case SFX.HeavyRain:
                cooldown = 0.25f; maxConcurrent = 1; priority = 0; break;
        }
    }

    /// <summary>
    ///  슬라이더 값을 데시벨로 변경 및 저장
    /// </summary>
    /// <param name="volume"></param>
    public void SetVolume(string volumeName, float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(volumeName, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(volumeName, volume);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float volume) => SetVolume("BGMVol", volume);
    public void SetSfxVolume(float volume) => SetVolume("SFXVol", volume);

    public void LoadVolumes()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVol", 1f);
        float bgmVol = PlayerPrefs.GetFloat("BGMVol", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);

        // if (masterVolSlider != null) masterVolSlider.value = masterVol;
        if (bgmVolSlider != null) bgmVolSlider.SetValueWithoutNotify(bgmVol);
        if (sfxVolSlider != null) sfxVolSlider.SetValueWithoutNotify(sfxVol);

        // SetMasterVolume(masterVol);
        SetBgmVolume(bgmVol);
        SetSfxVolume(sfxVol);
    }

    public void ClickUI()
    {
        PlaySfx(SFX.Click);
    }
}
