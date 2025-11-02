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

    /*[Header("Vol UI")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider bgmVolSlider;
    [SerializeField] private Slider sfxVolSlider;*/

    [Header("Audio Mixer 설정")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixer;
    public AudioMixerGroup sfxMixer;

    [Header("BGM 설정")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    private AudioSource bgmPlayer;

    [Header("SFX 설정")]
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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetBgmForScene(scene.name);
    }

    /*/// <summary>
    /// 씬 이름에 따라 적절한 BGM 설정
    /// </summary>
    /// <param name="sceneName">현재 씬 이름</param>
    private void SetBgmForScene(string sceneName)
    {
        AudioClip selectedBgm = null;

        if (sceneName == "Title")
        {
            selectedBgm = bgmClips[(int)BFX.MainTitle];
        }
        else if (sceneName == "1015Main")
        {
            selectedBgm = bgmClips[(int)BFX.Morning];
        }
        else
        {
            Debug.LogWarning($"'{sceneName}'에 대한 BGM 설정이 없습니다. 기본값을 사용합니다.");
        }

        if (selectedBgm != null && bgmPlayer.clip != selectedBgm)
        {
            bgmPlayer.clip = selectedBgm;
            bgmPlayer.Play();
        }
    }*/

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
    /// 씬 이름에 따라 적절한 BGM 설정
    /// </summary>
    /// <param name="sceneName">현재 씬 이름</param>
    private void SetBgmForScene(string sceneName)
    {
        AudioClip selectedBgm = null;

        switch (sceneName)
        {
            case "Title":
                selectedBgm = bgmClips[(int)BGM.MainTitle];
                break;
            case "1015Main":
                selectedBgm = bgmClips[(int)BGM.Morning];
                break;
            default:
                return;
        }

        Debug.Log($"BGM : {selectedBgm.name}");

        if (sceneName == null)
        {
            Debug.LogWarning($"'{sceneName}'에 대한 BGM 설정이 없습니다. 기본값을 사용합니다.");
            selectedBgm = bgmClips[(int)BGM.Morning];
        }

        if (selectedBgm != null && bgmPlayer.clip != selectedBgm)
        {
            bgmPlayer.Stop();
            bgmPlayer.clip = selectedBgm;
            bgmPlayer.Play();
        }
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

        /*int sfxIndex = (int)sfx;

        if (sfxIndex < 0 || sfxIndex >= sfxClips.Length)
        {
            return;
        }

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + sfxChannelIndex) % sfxPlayers.Length;

            if (!sfxPlayers[loopIndex].isPlaying)
            {
                sfxChannelIndex = loopIndex;
                sfxPlayers[loopIndex].clip = sfxClips[sfxIndex];
                sfxPlayers[loopIndex].Play();
                break;
            }
        }*/

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

    /*/// <summary>
    ///  볼륨 믹서 셋팅 및 저장
    /// </summary>
    /// <param name="volume"></param>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat("MasterVol", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVol", volume);
    }

    public void SetBgmVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat("BGMVol", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("BGMVol", volume);
        Debug.Log("BGM Volume Set: " + volume);
    }

    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat("SFXVol", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVol", volume);
    }

    public float GetVolume(string volumeName)
    {
        audioMixer.GetFloat(volumeName, out float value);
        return Mathf.Pow(10, value / 20);
    }*/

}
