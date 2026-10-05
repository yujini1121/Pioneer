using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MastSystem : CommonBase
{
    [Header("돛대 설정")]
    public int mastLevel = 1;
    public float interactionRange = 1.5f;
    public LayerMask playerLayer;

    [Header("첫 번째 UI - 기본 정보")]
    public GameObject mastUI;
    public TextMeshProUGUI hpPercentageText;
    public Button upgradeMenuButton;
    public Button closeButton;
    public Slider hpSlider;

    [Header("두 번째 UI - 강화 상세")]
    public GameObject upgradeUI;
    public Button enhanceButton;
    public Button backButton;
    public TextMeshProUGUI material1CountText;
    public TextMeshProUGUI material2CountText;

    [Header("강화 재료 요구치")]
    [SerializeField] private int requiredWood = 30;
    [SerializeField] private int requiredCloth = 15;

    [Header("메시지 시스템")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;

    private bool playerInRange = false;
    private bool isUIOpen = false;
    private bool isUpgradeMenuOpen = false;
    private Coroutine messageCoroutine;
    private Coroutine warningCoroutine;

    public static MastSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        SetMastLevel(mastLevel);
        hp = maxHp;

        mastUI?.SetActive(false);
        upgradeUI?.SetActive(false);

        if (upgradeMenuButton) upgradeMenuButton.onClick.AddListener(OpenUpgradeMenu);
        if (enhanceButton) enhanceButton.onClick.AddListener(EnhanceMast);
        if (closeButton) closeButton.onClick.AddListener(CloseAllUI);
        if (backButton) backButton.onClick.AddListener(BackToMainUI);

    }

    void Update()
    {
        CheckPlayerDistance();
        HandleInput();
        UpdateUI();
        CheckMastCondition();
    }

    void SetMastLevel(int level)
    {
        mastLevel = Mathf.Clamp(level, 1, 2);
        maxHp = mastLevel == 1 ? 500 : 1000;
        if (hp > maxHp) hp = maxHp;
    }

    public int GetMaxDeckCount()
    {
        return mastLevel == 1 ? 30 : 50;
    }

    void CheckPlayerDistance()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        bool newState = hits.Length > 0;

        if (newState != playerInRange)
            ;

        playerInRange = newState;

        if (!playerInRange && isUIOpen)
        {
            CloseAllUI();
        }
    }

    void HandleInput()
    {
        if (!playerInRange) return;

        if (Input.GetMouseButtonDown(1))
        {

            if (!isUIOpen)
            {
                OpenUI();

                InGameUI.instance.OpenUI(new System.Collections.Generic.List<GameObject>() { },
                    InGameUI.ID_MAST_UI, () =>
                    {
                        isUIOpen = false;
                        mastUI?.SetActive(false);
                    }
                    );
            }
            else
            {
            }
        }
    }

    void OpenUI()
    {

        isUIOpen = true;

        if (mastUI == null) Debug.LogError("돛대 화면이 연결되지 않았습니다.");
        if (upgradeUI == null) Debug.LogError("돛대 강화 화면이 연결되지 않았습니다.");

        mastUI?.SetActive(true);
        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UPGRADE);
    }

    void OpenUpgradeMenu()
    {

        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UI);

        isUpgradeMenuOpen = true;
        upgradeUI?.SetActive(true);
        InGameUI.instance.OpenUI(new System.Collections.Generic.List<GameObject>() { },
                       InGameUI.ID_MAST_UPGRADE, () =>
                       {
                           isUpgradeMenuOpen = false;
                           upgradeUI?.SetActive(false);
                       });

    }

    void BackToMainUI()
    {

        isUIOpen = true;
        mastUI?.SetActive(true);
        InGameUI.instance.OpenUI(new System.Collections.Generic.List<GameObject>() { },
                    InGameUI.ID_MAST_UI, () =>
                    {
                        isUIOpen = false;
                        mastUI?.SetActive(false);
                    }
                    );

        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UPGRADE);

    }

    public void CloseAllUI()
    {

        isUIOpen = false;
        isUpgradeMenuOpen = false;


        mastUI?.SetActive(false);
        upgradeUI?.SetActive(false);


        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UI);
        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UPGRADE);
    }

    void UpdateUI()
    {
        if (!isUIOpen) return;

        if (!isUpgradeMenuOpen)
        {
            if (hpPercentageText != null)
            {
                float percent = (float)hp / maxHp * 100f;
                hpPercentageText.text = $"내구도: {percent:F0}%";
                hpSlider.value = (float)hp / maxHp;
            }
            return;
        }

        int woodId = MastManager.Instance?.woodItemID ?? 0;
        int clothId = MastManager.Instance?.clothItemID ?? 0;

        int woodCount = InventoryManager.Instance?.Get(woodId) ?? 0;
        int clothCount = InventoryManager.Instance?.Get(clothId) ?? 0;

        material1CountText.text = $"{woodCount}/{requiredWood}";
        material1CountText.color = woodCount >= requiredWood ? Color.white : Color.red;

        material2CountText.text = $"{clothCount}/{requiredCloth}";
        material2CountText.color = clothCount >= requiredCloth ? Color.white : Color.red;

        enhanceButton.interactable = (mastLevel < 2 && woodCount >= requiredWood && clothCount >= requiredCloth);
    }

    void CheckMastCondition()
    {
        float percent = (float)hp / maxHp;
        if (percent <= 0.5f && percent > 0f)
        {
            if (warningCoroutine == null)
                warningCoroutine = StartCoroutine(ShowWarningMessage());
        }
        else if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
    }

    IEnumerator ShowWarningMessage()
    {
        while (true)
        {
            ShowMessage("돛대가 불안정해 보인다.", 4f);
            yield return new WaitForSeconds(10f);
        }
    }

    public void ShowMessage(string message, float duration)
    {
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messagePanel?.SetActive(true);
        if (messageText) messageText.text = message;

        yield return new WaitForSeconds(duration);

        messagePanel?.SetActive(false);
        messageCoroutine = null;
    }

    void EnhanceMast()
    {

        if (mastLevel >= 2)
        {
            ShowMessage("이미 최대 단계입니다.", 3f);
            return;
        }

        int woodId = MastManager.Instance?.woodItemID ?? 0;
        int clothId = MastManager.Instance?.clothItemID ?? 0;

        int woodCount = InventoryManager.Instance?.Get(woodId) ?? 0;
        int clothCount = InventoryManager.Instance?.Get(clothId) ?? 0;

        if (woodCount < requiredWood || clothCount < requiredCloth)
        {
            ShowMessage("재료가 부족합니다.", 3f);
            return;
        }

        InventoryManager.Instance.Remove(
            new SItemStack(woodId, requiredWood),
            new SItemStack(clothId, requiredCloth)
        );

        SetMastLevel(mastLevel + 1);
        hp = maxHp;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.FortifyObject);

        ShowMessage("돛대가 강화되었습니다.", 3f);
        InventoryUiMain.instance?.IconRefresh();
        UpdateUI();
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;

        hp -= damage;
        if (damage > 0 && hp > 0) StructureHitFeedback.Play(gameObject, 0.65f);

        this.attacker = attacker;

        if (hp <= 0)
        {
            hp = 0;
            IsDead = true;
            WhenDestroy();
        }
    }

    public override void WhenDestroy()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.GameOver);

        GameManager.Instance?.TriggerGameOver();
    }
}
