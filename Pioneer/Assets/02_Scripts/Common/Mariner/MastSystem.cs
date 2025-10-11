using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MastSystem : CommonBase
{
    [Header("돗대 설정")]
    public int mastLevel = 1;
    public float interactionRange = 3f;
    public LayerMask playerLayer;

    [Header("첫 번째 UI - 기본 정보")]
    public GameObject mastUI; // 첫 번째 UI 
    public TextMeshProUGUI hpPercentageText; // 내구도 % 표시
    public Button upgradeMenuButton; // 업그레이드 메뉴로 가는 버튼
    public Button closeButton; // 닫기 버튼

    [Header("두 번째 UI - 강화 상세")]
    public GameObject upgradeUI; // 두 번째 UI (강화 상세)
    public Image material1Image; // 통나무 이미지
    public Image material2Image; // 천 이미지
    public TextMeshProUGUI material1CountText; // 통나무 개수 (n/30)
    public TextMeshProUGUI material2CountText; // 천 개수 (n/15)
    public TextMeshProUGUI currentStageText; // 다음 단계 표시
    public Image nextStageImage; // 2단계 이미지
    public TextMeshProUGUI enhanceEffectText; // 강화 효과 설명
    public Button enhanceButton; // 강화하기 버튼
    public Button backButton; // 뒤로가기 버튼

    [Header("메시지 시스템")]
    public GameObject messagePanel; // 메시지 패널
    public TextMeshProUGUI messageText; // 메시지 텍스트

    private bool playerInRange = false;
    private bool isUIOpen = false;
    private bool isUpgradeMenuOpen = false; // 업그레이드 메뉴 상태
    private Coroutine messageCoroutine;
    private Coroutine warningCoroutine;

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

    void CloseAllUI()
    {
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
        }
        else
        {
            if (currentStageText)
            {
                if (mastLevel == 1)
                    currentStageText.text = "다음 단계: 2단계";
                else
                    currentStageText.text = "최대 단계";
            }

            if (enhanceEffectText)
            {
                if (mastLevel == 1)
                    enhanceEffectText.text = "최대 갑판 설치 개수가\n30개에서 50개로\n증가합니다";
                else
                    enhanceEffectText.text = "이미 최대 단계입니다";
            }

            int currentWood = InventoryManager.Instance.Get(MastManager.Instance.woodItemID);
            int currentCloth = InventoryManager.Instance.Get(MastManager.Instance.clothItemID);

            if (material1CountText)
            {
                material1CountText.text = $"{currentWood}/30";
                material1CountText.color = currentWood >= 30 ? Color.white : Color.red;
            }

            if (material2CountText)
            {
                material2CountText.text = $"{currentCloth}/15";
                material2CountText.color = currentCloth >= 15 ? Color.white : Color.red;
            }

            if (enhanceButton)
            {
                bool canEnhance = mastLevel < 2 && currentWood >= 30 && currentCloth >= 15;
                enhanceButton.interactable = canEnhance;
            }
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

        const int requiredWood = 30;
        const int requiredCloth = 15;

        // MastManager에 아이템 ID가 정의되어 있음 30001 : 나무와 30003 : 천으로
        int woodId = MastManager.Instance.woodItemID;
        int clothId = MastManager.Instance.clothItemID;

        // 인벤토리에 재료가 충분한지 확인
        if (InventoryManager.Instance.Get(woodId) < requiredWood ||
            InventoryManager.Instance.Get(clothId) < requiredCloth)
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