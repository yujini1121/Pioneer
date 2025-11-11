using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

public class Option : MonoBehaviour, IBegin
{
    [SerializeField] private GameObject optionUI;

    public Slider bgmVolSlider;
    public Slider sfxVolSlider;

    private static Option instance;

    private void Awake()
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
    }
    void Start()
    {
        SetDeactivateOption();
        if (AudioManager.instance != null)
        {
            AudioManager.instance.bgmVolSlider = bgmVolSlider;
            AudioManager.instance.sfxVolSlider = sfxVolSlider;

            AudioManager.instance.InitSliders();
            AudioManager.instance.InitListenerVolSliders();
            AudioManager.instance.LoadVolumes();
        }
        else
        {
            UnityEngine.Debug.Log("Screen Controller Instance Error");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = optionUI.activeInHierarchy;

            if (isActive)
            {
                SetDeactivateOption();
            }
            else
            {
                SetActivateOption();
            }
        }
    }

    public void SetActivateOption() => optionUI.SetActive(true);
    public void SetDeactivateOption()
    {
        optionUI.SetActive(false);
    }
}
