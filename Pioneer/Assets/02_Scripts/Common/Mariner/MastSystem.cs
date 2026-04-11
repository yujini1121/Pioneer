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
        Debug.Log("[MastSystem] Awake 실행됨");
    }

    void Start()
    {
        Debug.Log("[MastSystem] Start 초기화");

        SetMastLevel(mastLevel);
        hp = maxHp;

        mastUI?.SetActive(false);
        upgradeUI?.SetActive(false);

        if (upgradeMenuButton) upgradeMenuButton.onClick.AddListener(OpenUpgradeMenu);
        if (enhanceButton) enhanceButton.onClick.AddListener(EnhanceMast);
        if (closeButton) closeButton.onClick.AddListener(CloseAllUI);
        if (backButton) backButton.onClick.AddListener(BackToMainUI);

        Debug.Log("[MastSystem] Start 완료");
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
            Debug.Log($"[MastSystem] playerInRange 변경: {playerInRange} → {newState}");

        playerInRange = newState;

        if (!playerInRange && isUIOpen)
        {
            Debug.Log("[MastSystem] 플레이어 범위 벗어남 → UI 닫음");
            CloseAllUI();
        }
    }

    void HandleInput()
    {
        if (!playerInRange) return;

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("[MastSystem] 우클릭 감지됨");

            if (!isUIOpen)
            {
                Debug.Log("[MastSystem] UI Open 조건 만족 → OpenUI 실행");
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
                Debug.Log("[MastSystem] UI 이미 열려있음 (무시)");
            }
        }
    }

    void OpenUI()
    {
        Debug.Log("[MastSystem] OpenUI()");

        isUIOpen = true;
        //isUpgradeMenuOpen = false;

        if (mastUI == null) Debug.LogError("[MastSystem] mastUI 가 Inspector에 연결되지 않음!");
        if (upgradeUI == null) Debug.LogError("[MastSystem] upgradeUI 가 Inspector에 연결되지 않음!");

        mastUI?.SetActive(true);
        //upgradeUI?.SetActive(false);
        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UPGRADE);
    }

    void OpenUpgradeMenu()
    {
        Debug.Log("[MastSystem] OpenUpgradeMenu()");
        
        InGameUI.instance.CloseUI(InGameUI.ID_MAST_UI);
        
        isUpgradeMenuOpen = true;
        upgradeUI?.SetActive(true);
        InGameUI.instance.OpenUI(new System.Collections.Generic.List<GameObject>() { },
                       InGameUI.ID_MAST_UPGRADE, () =>
                       {
                           isUpgradeMenuOpen = false;
                           upgradeUI?.SetActive(false);
                       });

        //mastUI?.SetActive(false);
    }

    void BackToMainUI()
    {
        Debug.Log("[MastSystem] BackToMainUI()");

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
        //isUpgradeMenuOpen = false;

        //mastUI?.SetActive(true);
        //upgradeUI?.SetActive(false);
    }

    public void CloseAllUI()
    {
        Debug.Log("[MastSystem] CloseAllUI()");

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

        // 강화 화면 UI
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
        Debug.Log("[MastSystem] EnhanceMast() 시도");

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
        Debug.Log($"[MastSystem] 돛대 데미지 {damage}, 현재 HP {hp}");

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

        Debug.Log("[MastSystem] WhenDestroy() → 게임오버 호출");
        GameManager.Instance?.TriggerGameOver();
    }
}
