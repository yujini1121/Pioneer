using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MastSystem : CommonBase
{
    [Header("돗대 설정")]
    public int mastLevel = 1;
    public float interactionRange = 3f;
    public LayerMask playerLayer;

    [Header("첫 번째 UI - 기본 정보")]
    public GameObject mastUI;                   // 첫 번째 UI 
    public TextMeshProUGUI hpPercentageText;    // 내구도 % 표시
    public Button upgradeMenuButton;            // 업그레이드 메뉴로 가는 버튼
    public Button closeButton;                  // 닫기 버튼

    [Header("두 번째 UI - 강화 상세 (경량화 + 카운팅 표시)")]
    public GameObject upgradeUI;                // 두 번째 UI (강화 상세)
    public Button enhanceButton;                // 강화하기 버튼
    public Button backButton;                   // 뒤로가기 버튼
    public TextMeshProUGUI material1CountText;  // 통나무 개수 (n/30)
    public TextMeshProUGUI material2CountText;  // 천 개수 (n/15)

    [Header("강화 재료 요구치")]                 // 기획자분들이 수정할 수 있으므로 빼둠
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

        if (mastUI) mastUI.SetActive(false);
        if (upgradeUI) upgradeUI.SetActive(false);

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
        Collider[] playersInRange = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        bool wasInRange = playerInRange;
        playerInRange = playersInRange.Length > 0;

        // 범위 이탈시 모든 UI 닫기
        if (wasInRange && !playerInRange)
        {
            CloseAllUI();
        }
    }

    void HandleInput()
    {
        if (playerInRange && Input.GetMouseButtonDown(1)) // 우클릭
        {
            if (!isUIOpen)
                OpenUI();
        }
    }

    void OpenUI()
    {
        isUIOpen = true;
        isUpgradeMenuOpen = false;
        if (mastUI) mastUI.SetActive(true);
        if (upgradeUI) upgradeUI.SetActive(false);
    }

    void OpenUpgradeMenu()
    {
        Debug.Log("OpenUpgradeMenu 호출됨");
        isUpgradeMenuOpen = true;
        if (mastUI)
        {
            mastUI.SetActive(false);
            Debug.Log("mastUI 비활성화");
        }
        if (upgradeUI)
        {
            upgradeUI.SetActive(true);
            Debug.Log("upgradeUI 활성화");
        }
        else
        {
            Debug.LogError("upgradeUI가 null.");
        }
    }

    void BackToMainUI()
    {
        isUpgradeMenuOpen = false;
        if (mastUI) mastUI.SetActive(true);
        if (upgradeUI) upgradeUI.SetActive(false);
    }

    public void CloseAllUI()
    {
        Debug.Log("MastUI : CloseAllUI");
        isUIOpen = false;
        isUpgradeMenuOpen = false;
        if (mastUI) mastUI.SetActive(false);
        if (upgradeUI) upgradeUI.SetActive(false);
    }

    void UpdateUI()
    {
        if (!isUIOpen) return;

        if (!isUpgradeMenuOpen)
        {
            if (hpPercentageText)
            {
                float hpPercentage = (float)hp / maxHp * 100f;
                hpPercentageText.text = $"내구도: {hpPercentage:F0}%";
            }
            return;
        }

        // 2번 상세 탭 : 재료 수량 표시
        int woodId = MastManager.Instance != null ? MastManager.Instance.woodItemID : 0;
        int clothId = MastManager.Instance != null ? MastManager.Instance.clothItemID : 0;

        int currentWood = (InventoryManager.Instance != null) ? InventoryManager.Instance.Get(woodId) : 0;
        int currentCloth = (InventoryManager.Instance != null) ? InventoryManager.Instance.Get(clothId) : 0;

        if (material1CountText)
        {
            material1CountText.text = $"{currentWood}/{requiredWood}";
            material1CountText.color = currentWood >= requiredWood ? Color.white : Color.red;
        }

        if (material2CountText)
        {
            material2CountText.text = $"{currentCloth}/{requiredCloth}";
            material2CountText.color = currentCloth >= requiredCloth ? Color.white : Color.red;
        }

        if (enhanceButton)
        {
            bool canEnhance = mastLevel < 2 && currentWood >= 30 && currentCloth >= 15;
            enhanceButton.interactable = canEnhance;
        }
    }


    void CheckMastCondition()
    {
        float hpPercentage = (float)hp / maxHp;

        if (hpPercentage <= 0.5f && hpPercentage > 0f)
        {
            if (warningCoroutine == null)
            {
                warningCoroutine = StartCoroutine(ShowWarningMessage());
            }
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
        if (messagePanel) messagePanel.SetActive(true);
        if (messageText) messageText.text = message;

        yield return new WaitForSeconds(duration);

        if (messagePanel) messagePanel.SetActive(false);
        messageCoroutine = null;
    }

    void EnhanceMast()
    {
        if (mastLevel >= 2)
        {
            ShowMessage("이미 최대 단계입니다.", 3f);
            return;
        }

        // MastManager에 아이템 ID가 정의되어 있음 30001 : 나무와 30003 : 천으로
        int woodId = MastManager.Instance != null ? MastManager.Instance.woodItemID : 0;
        int clothId = MastManager.Instance != null ? MastManager.Instance.clothItemID : 0;

        int currentWood = (InventoryManager.Instance != null) ? InventoryManager.Instance.Get(woodId) : 0;
        int currentCloth = (InventoryManager.Instance != null) ? InventoryManager.Instance.Get(clothId) : 0;

        // 인벤토리에 재료가 충분한지 확인
        if (currentWood < requiredWood || currentCloth < requiredCloth)
        {
            ShowMessage("재료가 부족합니다.", 3f);
            return;
        }

        // Remove 메서드를 한 번만 호출하여 모든 재료를 소모합니다.
        InventoryManager.Instance.Remove(
            new SItemStack(woodId, requiredWood),
            new SItemStack(clothId, requiredCloth)
        );

        SetMastLevel(mastLevel + 1);
        hp = maxHp;
        ShowMessage("돛대가 강화되었습니다", 3f);

        if (InventoryUiMain.instance != null)
        {
            InventoryUiMain.instance.IconRefresh();
        }

        // 버튼/카운팅 즉시 갱신
        UpdateUI();
    }

    public override void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;

        hp -= damage;
        Debug.Log($"돛대가 데미지 {damage} 받음. 현재 HP: {hp}");
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}