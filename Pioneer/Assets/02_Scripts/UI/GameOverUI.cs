using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject gameOverPanel;
    public GameObject[] otherUIPanels;
    [SerializeField] private GameObject infiniteModeHintText;

    [Header("텍스트")]
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI crewStatsText;

    [Header("버튼")]
    public Button continueButton;
    public Button titleButton;

    private bool voyageSucceeded;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(GoToTitle);
        }
    }

    public void ShowGameOverScreen(int totalCrewMembers, int deadCrewMembers)
    {
        ShowGameOverScreen(totalCrewMembers, deadCrewMembers, false);
    }

    public void ShowGameOverScreen(int totalCrewMembers, int deadCrewMembers, bool voyageSucceeded)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        this.voyageSucceeded = voyageSucceeded;
        UpdateGameOverTexts(totalCrewMembers, deadCrewMembers, voyageSucceeded);
        ConfigureResultButton(voyageSucceeded);
        SetInfiniteModeHintActive(voyageSucceeded);

        if (voyageSucceeded)
            GameModeState.UnlockInfiniteMode();
    }

    public void HideGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void UpdateGameOverTexts(int totalCrewMembers, int deadCrewMembers, bool voyageSucceeded)
    {
        int days, hours;
        GameManager.Instance.GetGameTimeInfo(out days, out hours);
        string resultText = voyageSucceeded ? "항해에 성공했습니다." : "항해에 실패했습니다.";

        if (survivalTimeText != null)
        {
            if (days > 0)
                survivalTimeText.text = $"{resultText}\n당신은 {days}일 {hours}시간 동안 항해했습니다.";
            else
                survivalTimeText.text = $"{resultText}\n당신은 {hours}시간 동안 항해했습니다.";
        }

        if (crewStatsText != null)
        {
            crewStatsText.text = $"당신은 항해하는 동안 승무원 총 {totalCrewMembers}명과 함께하고, {deadCrewMembers}명을 죽음으로 내몰았습니다.";
        }
    }

    private void ConfigureResultButton(bool voyageSucceeded)
    {
        if (continueButton == null) return;

        continueButton.onClick.RemoveAllListeners();
        if (voyageSucceeded)
            continueButton.onClick.AddListener(ContinueInInfiniteMode);
        else
            continueButton.onClick.AddListener(RestartGame);

        TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null)
            buttonText.text = voyageSucceeded ? "무한모드로 계속하기" : "다시 시작하기";
    }

    private void SetInfiniteModeHintActive(bool isActive)
    {
        if (infiniteModeHintText == null)
            infiniteModeHintText = FindChildByName("InfiniteModeHintText");

        if (infiniteModeHintText != null)
            infiniteModeHintText.SetActive(isActive);
    }

    private GameObject FindChildByName(string childName)
    {
        if (gameOverPanel == null) return null;

        Transform[] children = gameOverPanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        if (AudioManager.instance != null)
            AudioManager.instance.RestoreRuntimeVolumes();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ContinueInInfiniteMode()
    {
        if (!voyageSucceeded) return;

        GameModeState.StartInfiniteMode();

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeFromEndingToInfiniteMode();
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        if (AudioManager.instance != null)
            AudioManager.instance.RestoreRuntimeVolumes();

        SceneManager.LoadScene("Title");
    }
}
