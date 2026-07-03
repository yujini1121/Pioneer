using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

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
        }

        Init();
    }

    void Start()
    {
        PlayBgm(BGM.MainTitle);
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
        sfxPlayers = new AudioSource[sfxChannels];
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
            sfxPlayers[index].outputAudioMixerGroup = sfxMixer;
        }

        sfxDictionary = new Dictionary<SFX, AudioClip>();
        foreach (SoundSFX pair in sfxSoundList)
        {
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
        float volume;

        if (audioMixer.GetFloat("BGMVol", out volume))
        {
            bgmVolSlider.value = Mathf.Pow(10, volume / 20);
        }

        if (audioMixer.GetFloat("SFXVol", out volume))
        {
            sfxVolSlider.value = Mathf.Pow(10, volume / 20);
        }
    }

    public void InitListenerVolSliders()
    {
        // masterVolSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmVolSlider.onValueChanged.AddListener(SetBgmVolume);
        sfxVolSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    public void PlayBgm(BGM bgm)
    {
        int bgmIndex = (int)bgm;

        if (bgmIndex < 0 || bgmIndex >= bgmClips.Length)
        {
            Debug.LogWarning($"PlayBgm: {bgm.ToString()}에 해당하는 bgmClip이 없습니다.");
            return;
        }

        AudioClip newClip = bgmClips[bgmIndex];

        if (bgmPlayer.clip != newClip)
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
        bgmPlayer.Stop();
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
    }

    private IEnumerator FadeOutForGameResultCoroutine(float duration)
    {
        float elapsed = 0f;
        float startBgmVolume = bgmPlayer != null ? bgmPlayer.volume : 0f;
        float[] startSfxVolumes = GetSfxVolumes();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float volumeScale = 1f - SmoothStep01(t);

            if (bgmPlayer != null)
                bgmPlayer.volume = startBgmVolume * volumeScale;

            ApplySfxVolumeScale(startSfxVolumes, volumeScale);

            yield return null;
        }

        if (bgmPlayer != null)
            bgmPlayer.volume = 0f;

        ApplySfxVolumeScale(startSfxVolumes, 0f);
        gameResultFadeCoroutine = null;
    }

    private float[] GetSfxVolumes()
    {
        if (sfxPlayers == null)
            return new float[0];

        float[] volumes = new float[sfxPlayers.Length];
        for (int i = 0; i < sfxPlayers.Length; i++)
            volumes[i] = sfxPlayers[i] != null ? sfxPlayers[i].volume : 0f;

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
        if (!sfxDictionary.ContainsKey(sfx) || sfxDictionary[sfx] == null)
            return;

        AudioClip clipToPlay = sfxDictionary[sfx];

        for(int index = 0; index <sfxPlayers.Length; index++)
        {
            int loopIndex = (index + sfxChannelIndex) % sfxPlayers.Length;

            if(!sfxPlayers[loopIndex].isPlaying)
            {
                sfxChannelIndex = loopIndex;

                sfxPlayers[loopIndex].clip = clipToPlay;
                sfxPlayers[loopIndex].Play();

                break;
            }
        }
    }

    /// <summary>
    ///  슬라이더 값을 데시벨로 변경 및 저장
    /// </summary>
    /// <param name="volume"></param>
    public void SetVolume(string volumeName, float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat(volumeName, Mathf.Log10(volume) * 20);
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
        if (bgmVolSlider != null) bgmVolSlider.value = bgmVol;
        if (sfxVolSlider != null) sfxVolSlider.value = sfxVol;

        // SetMasterVolume(masterVol);
        SetBgmVolume(bgmVol);
        SetSfxVolume(sfxVol);
    }

    public void ClickUI()
    {
        PlaySfx(SFX.Click);
    }
}
