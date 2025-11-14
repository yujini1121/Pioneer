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
    public GameObject[] otherUIPanels; // 숨길 다른 UI 패널들

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
        // GameManager에서 일수와 시간 가져오기
        int days, hours;
        GameManager.Instance.GetGameTimeInfo(out days, out hours);

        // 생존 시간 텍스트
        if (survivalTimeText != null)
        {
            if (days > 0)
                survivalTimeText.text = $"당신은 {days}일 {hours}시간 동안 항해했습니다.";
            else
                survivalTimeText.text = $"당신은 {hours}시간 동안 항해했습니다.";
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

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }
}