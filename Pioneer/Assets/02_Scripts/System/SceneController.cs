using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour, IBegin
{
    public static SceneController Instance;

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] public string sceneToLoad;
    [SerializeField] private string allowedSceneName = "Title";
    [SerializeField] private float minimumLoadingScreenTime = 0.8f;
    [SerializeField]
    private string[] loadingTips =
    {
        "Tip. 해가 떠 있을 때 자원을 모으세요. 밤바다는 오래 기다려주지 않습니다.",
        "Tip. 낚시를 하다 보면 가끔 보물상자를 건질 수 있습니다.",
        "Tip. 죄책감이 쌓이면 예상치 못한 일이 벌어질 수 있습니다.",
        "Tip. 돛대를 업그레이드하면 더 먼 구역까지 확장할 수 있습니다.",
        "Tip. 손상된 시설은 오래 버틸 수 없습니다. 틈틈이 수리하세요.",
        "Tip. 불빛은 어둠을 완전히 막아주지 못합니다.",
        "Tip. 자원이 부족하다면 생존에 필요한 시설부터 유지하세요.",
        "Tip. 조용한 밤일수록 더 주의해야 합니다."
    };

    public bool isLoading = false;
    [SerializeField] private GameObject loadingUIRoot;
    [SerializeField] private TextMeshProUGUI loadingTitleText;
    [SerializeField] private TextMeshProUGUI loadingTipText;
    [SerializeField] private Image loadingProgressFill;
    private string currentSceneName;
    private string currentLoadingTip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLoadingUI();
            SetLoadingUIVisible(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(Fade(1, 0));
    }

    private void Update()
    {
        /*if (!isLoading &&
            SceneManager.GetActiveScene().name != sceneToLoad &&
            Input.GetKeyDown(KeyCode.Space))
        {
            AudioManager.instance.PlaySfx(AudioManager.SFX.OpenBox);
        }
        if (!isLoading &&
            SceneManager.GetActiveScene().name != sceneToLoad &&
            Input.GetKeyUp(KeyCode.Space))
        {
            AudioManager.instance.PlaySfx(AudioManager.SFX.GetFishing);
            LoadScene(sceneToLoad);
        }*/
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        isLoading = true;
        InitializeLoadingUI();
        SetLoadingUIVisible(true);
        SelectLoadingTip();
        UpdateLoadingUI(0f);

        // AudioManager.instance.PlayBgm(AudioManager.BGM.Morning);

        yield return Fade(0, 1);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false;

        float loadingScreenTime = 0f;
        while (!loadOperation.isDone)
        {
            loadingScreenTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            UpdateLoadingUI(progress);

            if (loadOperation.progress >= 0.9f && loadingScreenTime >= minimumLoadingScreenTime)
            {
                UpdateLoadingUI(1f);
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        SetLoadingUIVisible(false);
        yield return new WaitForSecondsRealtime(0.1f);
        yield return Fade(1, 0);

        isLoading = false;

        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float time = 0f;
        fadeCanvasGroup.alpha = from;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }

    private void InitializeLoadingUI()
    {
        if (fadeCanvasGroup == null)
            return;

        HideLegacyLoadingText();

        if (loadingUIRoot != null)
        {
            WireLoadingUIReferences();
            return;
        }

        Transform canvasTransform = fadeCanvasGroup.transform;
        TMP_FontAsset fontAsset = FindExistingFontAsset();

        loadingUIRoot = new GameObject("Loading UI", typeof(RectTransform));
        loadingUIRoot.transform.SetParent(canvasTransform, false);
        loadingUIRoot.transform.SetAsLastSibling();

        RectTransform rootRect = loadingUIRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        loadingTitleText = CreateText("Loading Title", loadingUIRoot.transform, fontAsset, "항해 준비 중", 42, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(loadingTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 92f), new Vector2(640f, 70f));

        GameObject progressBack = CreateImage("Loading Progress Back", loadingUIRoot.transform, new Color(0.12f, 0.16f, 0.18f, 0.95f));
        SetRect(progressBack.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(560f, 18f));

        GameObject progressFillObject = CreateImage("Loading Progress Fill", progressBack.transform, new Color(0.83f, 0.64f, 0.28f, 1f));
        loadingProgressFill = progressFillObject.GetComponent<Image>();
        loadingProgressFill.type = Image.Type.Filled;
        loadingProgressFill.fillMethod = Image.FillMethod.Horizontal;
        loadingProgressFill.fillOrigin = 0;
        loadingProgressFill.fillAmount = 0f;

        RectTransform fillRect = progressFillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        loadingTipText = CreateText("Loading Tip", loadingUIRoot.transform, fontAsset, string.Empty, 24, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(loadingTipText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(900f, 90f));
    }

    private void WireLoadingUIReferences()
    {
        if (loadingUIRoot == null)
            return;

        if (loadingTitleText == null)
        {
            Transform titleTransform = loadingUIRoot.transform.Find("Loading Title");
            if (titleTransform != null)
                loadingTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
        }

        if (loadingTipText == null)
        {
            Transform tipTransform = loadingUIRoot.transform.Find("Loading Tip");
            if (tipTransform != null)
                loadingTipText = tipTransform.GetComponent<TextMeshProUGUI>();
        }

        if (loadingProgressFill == null)
        {
            Transform fillTransform = loadingUIRoot.transform.Find("Loading Progress Back/Loading Progress Fill");
            if (fillTransform != null)
                loadingProgressFill = fillTransform.GetComponent<Image>();
        }
    }

    private void SetLoadingUIVisible(bool visible)
    {
        HideLegacyLoadingText();

        if (loadingUIRoot != null)
            loadingUIRoot.SetActive(visible);
    }

    private void UpdateLoadingUI(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (loadingProgressFill != null)
            loadingProgressFill.fillAmount = progress;

        if (loadingTitleText != null)
            loadingTitleText.text = "항해 준비 중...";

        if (loadingTipText != null)
            loadingTipText.text = currentLoadingTip;
    }

    private void SelectLoadingTip()
    {
        if (loadingTips == null || loadingTips.Length == 0)
        {
            currentLoadingTip = string.Empty;
            return;
        }

        currentLoadingTip = loadingTips[Random.Range(0, loadingTips.Length)];
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, TMP_FontAsset fontAsset, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = new Color(0.95f, 0.91f, 0.82f, 1f);
        textComponent.raycastTarget = false;

        if (fontAsset != null)
            textComponent.font = fontAsset;

        return textComponent;
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }

    private void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private TMP_FontAsset FindExistingFontAsset()
    {
        TextMeshProUGUI[] textComponents = fadeCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i].font != null)
                return textComponents[i].font;
        }

        return null;
    }

    private void HideLegacyLoadingText()
    {
        if (fadeCanvasGroup == null)
            return;

        TextMeshProUGUI[] textComponents = fadeCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i] == null)
                continue;

            if (loadingUIRoot != null && textComponents[i].transform.IsChildOf(loadingUIRoot.transform))
                continue;

            if (textComponents[i].text.Contains("Loading"))
                textComponents[i].gameObject.SetActive(false);
        }
    }
}
