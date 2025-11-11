using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private GameObject pressKeyText;
    [SerializeField] private GameObject titleButton;
    [SerializeField] private GameObject optionUI;

    [Header("사운드 슬라이더")]
    public Slider bgmVolSlider;
    public Slider sfxVolSlider;

    private bool readyToStart = false;

    private void Start()
    {
        SetDeactiveTitleButton();

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
        if (!readyToStart && Input.GetKeyDown(KeyCode.Space)) // 스페이스바를 한번 누르면 켜지고 그 이후론 작동없음
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.OpenBox);

            SetActiveTitleButton();
            if (pressKeyText != null) pressKeyText.SetActive(false);

            readyToStart = true;
        }
    }

    public void StartGameActive()
    {
        Debug.Log("시작버튼 누름");
        SceneController.Instance.LoadScene(SceneController.Instance.sceneToLoad);
    }

    public void SetActiveTitleButton() => titleButton.SetActive(true);
    public void SetDeactiveTitleButton() => titleButton.SetActive(false);
    public void SetActivateOption() => optionUI.SetActive(true);
    public void SetDeactivateOption() => optionUI.SetActive(false);

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
