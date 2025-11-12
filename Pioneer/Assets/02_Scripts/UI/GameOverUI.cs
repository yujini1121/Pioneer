using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject gameOverAndClear;
    public GameObject[] otherUIPanels; // 숨길 다른 UI 패널들

    [Tooltip("마우스를 올렸을 때 변경될 색상")]
    public Color hoverColor = Color.white;

    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI crewStatsText;
    public Button restartButton;
    public Button titleButton;
    public TextMeshProUGUI restartButtonText;
    public TextMeshProUGUI titleButtonButtonText;


    private void Start()
    {
        gameOverAndClear.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (titleButton != null)
            titleButton.onClick.AddListener(GoToTitle);
    }

    public void ShowGameOverScreen(int totalCrewMembers, int deadCrewMembers)
    {
        if (gameOverAndClear != null)
            gameOverAndClear.SetActive(true);
        Time.timeScale = 0f; // 게임 일시정지
        UpdateGameOverTexts(totalCrewMembers, deadCrewMembers);
    }

    public void ShowGameClearScreen(int totalCrewMembers, int deadCrewMembers)
    {
        if (gameOverAndClear != null)
            gameOverAndClear.SetActive(true);
        Time.timeScale = 0f; // 게임 일시정지
        UpdateGameClearTexts(totalCrewMembers, deadCrewMembers);
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

    private void UpdateGameClearTexts(int totalCrewMembers, int deadCrewMembers)
    {
        // GameManager에서 일수와 시간 가져오기
        int days, hours;
        GameManager.Instance.GetGameTimeInfo(out days, out hours);

        // 생존 시간 텍스트
        if (survivalTimeText != null)
        {
            if (days > 0)
                survivalTimeText.text = $"축하합니다! 당신은 항해에 성공하셨습니다!";
        }

        // 승무원 통계 텍스트
        if (crewStatsText != null)
        {
            crewStatsText.text = $"당신은 항해하는 동안 승무원 총 {totalCrewMembers}명과 함께하고, {deadCrewMembers}명과 작별하였습니다\n그럼에도 당신은 굳건히 항해를 마쳤습니다.";
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

    public void RetryChangeColor()
    {
        restartButtonText.color = hoverColor;
    }

    public void GoToTitleChangeColor()
    {
        titleButtonButtonText.color = hoverColor;
    }

    public void GeneralColor()
    {
        restartButtonText.color = Color.white;
        titleButtonButtonText.color = Color.white;
    }
}