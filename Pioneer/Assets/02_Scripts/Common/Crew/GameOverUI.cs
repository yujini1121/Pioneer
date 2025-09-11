using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI crewStatsText;
    public Button restartButton;
    public Button titleButton;

    private void Start()
    {
        gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (titleButton != null)
            titleButton.onClick.AddListener(GoToTitle);
    }

    public void ShowGameOverScreen(int totalCrewMembers, int deadCrewMembers)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        UpdateGameOverTexts(totalCrewMembers, deadCrewMembers);
    }

    private void UpdateGameOverTexts(int totalCrewMembers, int deadCrewMembers)
    {
        // 시간 계산
        float totalGameTime = 0f;
        if (GameManager.Instance != null)
        {
            totalGameTime = GameManager.Instance.currentGameTime;
        }

        int totalDays = Mathf.FloorToInt(totalGameTime / (GameManager.Instance.dayDuration + GameManager.Instance.nightDuration));
        float remainingTime = totalGameTime % (GameManager.Instance.dayDuration + GameManager.Instance.nightDuration);
        int totalHours = Mathf.FloorToInt(remainingTime / 3600f * 24f);

        // 생존 시간 텍스트
        if (survivalTimeText != null)
        {
            if (totalDays > 0)
                survivalTimeText.text = $"당신은 {totalDays}일, {totalHours}시간 동안 항해했습니다.";
            else
                survivalTimeText.text = $"당신은 {totalHours}시간 동안 항해했습니다.";
        }

        // 승무원 통계 텍스트
        if (crewStatsText != null)
        {
            crewStatsText.text = $"당신은 항해하는 동안 승무원 총 {totalCrewMembers}명과 함께하고, {deadCrewMembers}명을 죽음으로 내몰았습니다.";
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }
}
