using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour, IBegin
{
    public static SceneController Instance;

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string allowedSceneName = "Title";

    private bool isLoading = false;
    private string currentSceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Init()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(Fade(1, 0));
    }

    private void Update()
    {
        if (!isLoading &&
            SceneManager.GetActiveScene().name != sceneToLoad &&
            Input.GetKeyDown(KeyCode.Space))
        {
            LoadScene(sceneToLoad);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        isLoading = true;

        yield return Fade(0, 1);
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitForSeconds(0.1f);
        yield return Fade(1, 0);

        isLoading = false;

        Destroy(gameObject); // 씬 이동 완료 후 자기 자신 제거
    }



    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        fadeCanvasGroup.alpha = from;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
