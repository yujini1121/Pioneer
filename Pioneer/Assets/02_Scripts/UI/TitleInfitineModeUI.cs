using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleInfiniteModeUI : MonoBehaviour
{
    [Header("버튼 연결")]
    [SerializeField] private Button normalStartButton;
    [SerializeField] private Button infiniteModeButton;

    [Header("게임 씬 이름")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Start()
    {
        if (normalStartButton != null)
        {
            normalStartButton.onClick.RemoveAllListeners();
            normalStartButton.onClick.AddListener(StartNormalGame);
        }

        if (infiniteModeButton != null)
        {
            infiniteModeButton.onClick.RemoveAllListeners();
            infiniteModeButton.onClick.AddListener(StartInfiniteGame);
            infiniteModeButton.gameObject.SetActive(GameModeState.IsInfiniteModeUnlocked);
        }
    }

    public void StartNormalGame()
    {
        GameModeState.StartNormalMode();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartInfiniteGame()
    {
        GameModeState.StartInfiniteMode();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}