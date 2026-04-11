using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

public class Option : MonoBehaviour, IBegin
{
    [SerializeField] private GameObject escUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject helpUI;

    public Slider bgmVolSlider;
    public Slider sfxVolSlider;

    public static Option instance;

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
        SetDeactivateEscUI();
        SetDeactivateOptionUI();
        SetDeactivateHelpUI();
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

        Time.timeScale = 1f;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    bool isActive = escUI.activeInHierarchy;

        //    if (isActive)
        //    {
        //        if(!optionUI.activeInHierarchy)
        //            SetDeactivateEscUI();
        //        // SetDeactivateOptionUI();
        //    }
        //    else
        //    {
        //        SetActivateEscUI();
        //        // SetActivateOptionUI();
        //    }
        //}
    }

    public void SetActivateEscUI()
    {
        InGameUI.instance.OpenUI(
            new List<GameObject>() { escUI },
            InGameUI.ID_ESC_OPTION,
            () =>
            {
                escUI.SetActive(false);
                Time.timeScale = 1f;
            });


        escUI.SetActive(true);
        Time.timeScale = 0f;
    }
    public void SetDeactivateEscUI()
    {
        InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION);
    }


    public void SetActivateHelpUI()
    {
        if (helpUI == null) return;
        if (InGameUI.instance.IsOpened(InGameUI.ID_ESC_OPTION_HELP)) return;

        helpUI.SetActive(true);

        InGameUI.instance.OpenUI(
            new List<GameObject>() { helpUI },
            InGameUI.ID_ESC_OPTION_HELP,
            () =>
            {
                helpUI.SetActive(false);
            });
    }
    public void SetDeactivateHelpUI() => InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION_HELP);
    public void SetActivateOptionUI()
    {
        optionUI.SetActive(true);
        InGameUI.instance.OpenUI(
            new List<GameObject>() { optionUI },
            InGameUI.ID_ESC_OPTION_SETTINGS,
            () =>
            {
                optionUI.SetActive(false);
            });

    }
    public void SetDeactivateOptionUI() => InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION_SETTINGS);

    public void QuitGame()
    {
        Debug.Log("���� ���� ��ư Ŭ��!");

        // ����Ƽ �����Ϳ��� �׽�Ʈ�� ��� (Play ��� ����)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // ���� ���� ���ӿ��� ������ ��� (���ø����̼� ����)
#else
        Application.Quit();
#endif
    }
}
