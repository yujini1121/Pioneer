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
        GameStartButton,
        SamshSound,
        GameOver,
        Die,
        Click,
        AfterAttack_Minion,
        AfterAttack_Titan,
        AfterAttack_Crawler,
        BeforeAttack_Minion,
        BeforeAttack_Titan,
        BeforeAttack_Crawler,
        Hunger,
        Sanity29Down,
        LevelUp,
        SelectQuickSlot,
        RemoveItem,
        ArrayItem,
        EatingFood,
        Drink,
        UseComsumpitem,
        BeforeFishing,
        GetFishing,
        OpenBox,
        SuccessCrafting,
        GreatSuccessCrafting,
        InstallingObject,
        InstallObject,
        RotateInstallTypeObject,
        DestroyedObject,
        Hit_Object,
        BalistaAttack,
        ActivatedSpiketrap,
        BeforeAttack_BlackFog,
        AfterAttack_BlackFog,
        Scream2,
        CantESCNoise,
        LaughSaren,
        Hurricane,
        HeavyRain,
        Thunder,
        FortifyObject,
        ItemGet,
        ToNight,
        MeetEnemy,
        Punch1_Player,
        Hit,
        Take
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
}
