using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

public class Option : MonoBehaviour, IBegin
{
    [Header("Vol Slider")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider bgmVolSlider;
    [SerializeField] private Slider sfxVolSlider;

    [Header("Audio Mixer ¼³Á¤")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixer;
    public AudioMixerGroup sfxMixer;

    [Header("Option UI")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Button xButton;
    [SerializeField] private GameObject optionUI;

    private void Start()
    {
        if (optionButton != null && xButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClicked);
            xButton.onClick.AddListener(OnXButtonClicked);
        }

        InitSliders();

        masterVolSlider.onValueChanged.AddListener(AudioManager.instance.SetMasterVolume);
        bgmVolSlider.onValueChanged.AddListener(AudioManager.instance.SetBgmVolume);
        sfxVolSlider.onValueChanged.AddListener(AudioManager.instance.SetSfxVolume);
    }

    private void OnOptionButtonClicked()
    {
        optionUI.SetActive(true);

        Time.timeScale = 0;

        optionUI.transform.SetAsLastSibling();
    }

    private void OnXButtonClicked()
    {
        Time.timeScale = 1;
        optionUI.SetActive(false);
    }

    void InitSliders()
    {
        masterVolSlider.value = AudioManager.instance.GetVolume("MasterVol");
        bgmVolSlider.value = AudioManager.instance.GetVolume("BGMVol");
        sfxVolSlider.value = AudioManager.instance.GetVolume("SFXVol");
    }
}
