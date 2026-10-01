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
        }
        else
        {
            Destroy(gameObject);
            return;
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
            UtilityFunctions.Log("Screen Controller Instance Error");
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
        if (escUI == null || InGameUI.instance == null
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        InGameUI.instance.OpenUI(
            new List<GameObject>() { escUI },
            InGameUI.ID_ESC_OPTION,
            () =>
            {
                escUI.SetActive(false);
                if (GameManager.Instance == null || !GameManager.Instance.IsGameResultActive)
                    Time.timeScale = 1f;
            });


        escUI.SetActive(true);
        Time.timeScale = 0f;
    }
    public void SetDeactivateEscUI()
    {
        if (InGameUI.instance != null) InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION);
    }


    public void SetActivateHelpUI()
    {
        if (helpUI == null || InGameUI.instance == null) return;
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
    public void SetDeactivateHelpUI()
    {
        if (InGameUI.instance != null) InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION_HELP);
    }
    public void SetActivateOptionUI()
    {
        if (optionUI == null || InGameUI.instance == null) return;
        optionUI.SetActive(true);
        InGameUI.instance.OpenUI(
            new List<GameObject>() { optionUI },
            InGameUI.ID_ESC_OPTION_SETTINGS,
            () =>
            {
                optionUI.SetActive(false);
            });

    }
    public void SetDeactivateOptionUI()
    {
        if (InGameUI.instance != null) InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION_SETTINGS);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void QuitGame()
    {
        UtilityFunctions.Log("게임 종료 버튼 클릭!");

        // 유니티 에디터에서는 테스트를 위해 Play 모드를 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // 빌드된 게임에서는 애플리케이션을 종료
#else
        Application.Quit();
#endif
    }
}
