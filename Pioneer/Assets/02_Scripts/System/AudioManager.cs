using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Mixer 설정")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixer;
    public AudioMixerGroup sfxMixer;

    [Header("BGM 설정")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    private AudioSource bgmPlayer;

    [Header("SFX 설정")]
    public AudioClip[] sfxClips;
    public float sfxVolume;
    public int sfxChannels;
    private AudioSource[] sfxPlayers;
    private int sfxChannelIndex;

    [Header("Vol UI")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider bgmVolSlider;
    [SerializeField] private Slider sfxVolSlider;

    /// <summary>
    /// 현재 효과음 종류 (인스펙터 창이랑 순서 꼭 맞추기)
    /// </summary>
    /// <returns></returns>
    public enum SFX
    {
        // Main_SFX
        BuyItem,
        Coin,
        NextDay,
        // MiniGame_SFX
        A_Cup,
        Failed,
        Finish
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

    /// <summary>
    /// 씬 이름에 따라 적절한 BGM 설정
    /// </summary>
    /// <param name="sceneName">현재 씬 이름</param>
    private void SetBgmForScene(string sceneName)
    {
        AudioClip selectedBgm = null;

        if (sceneName == "Joohun")
        {
            selectedBgm = bgmClips[0];
        }
        else if (sceneName == "MiniGameTest")
        {
            selectedBgm = bgmClips[1];
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
    }


    /// <summary>
    /// BGM 및 SFX 사운드 플레이어 초기화
    /// </summary>
    void Init()
    {
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.outputAudioMixerGroup = bgmMixer;

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
    }

    /// <summary>
    /// 효과음 플레이
    /// </summary>
    /// <param name="sfx"></param>
    public void PlaySfx(SFX sfx)
    {
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + sfxChannelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying)
            {
                continue;
            }

            sfxChannelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }

    /// <summary>
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
    }

}
