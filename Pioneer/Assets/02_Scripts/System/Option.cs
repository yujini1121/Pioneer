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
        SetDeactivateEscUI();
        SetDeactivateOptionUI();
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = escUI.activeInHierarchy;

            if (isActive)
            {
                if(!optionUI.activeInHierarchy)
                    SetDeactivateEscUI();
                // SetDeactivateOptionUI();
            }
            else
            {
                SetActivateEscUI();
                // SetActivateOptionUI();
            }
        }
    }

    public void SetActivateEscUI()
    {
        escUI.SetActive(true);
        Time.timeScale = 0f;
    }
    public void SetDeactivateEscUI()
    {
        escUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public void SetActivateOptionUI() => optionUI.SetActive(true);
    public void SetDeactivateOptionUI() => optionUI.SetActive(false);

    public void QuitGame()
    {
        Debug.Log("게임 종료 버튼 클릭!");

        // 유니티 에디터에서 테스트할 경우 (Play 모드 중지)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // 실제 빌드된 게임에서 실행할 경우 (어플리케이션 종료)
#else
        Application.Quit();
#endif
    }
}
