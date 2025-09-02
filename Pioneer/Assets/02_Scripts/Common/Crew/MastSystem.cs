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

    [Header("기본 UI")]
    public GameObject mastUI;
    public GameObject interactionPrompt; // E키 프롬프트
    public GameObject messagePanel; // 메시지 패널
    public TextMeshProUGUI messageText; // 메시지 텍스트
    public Button closeButton; // X 닫기 버튼

    [Header("재료 UI")]
    public Image material1Image; // 통나무 이미지
    public Image material2Image; // 천 이미지
    public TextMeshProUGUI material1CountText; // 통나무 개수 (n/30)
    public TextMeshProUGUI material2CountText; // 천 개수 (n/15)

    [Header("강화 정보 UI")]
    public TextMeshProUGUI currentStageText; // 현재 1단계
    public Image nextStageImage; // 2단계 이미지
    public TextMeshProUGUI enhanceEffectText; // 강화 효과 설명
    public Button enhanceButton; // 강화하기 버튼

    private bool playerInRange = false;
    private bool isUIOpen = false;
    private Coroutine messageCoroutine;
    private Coroutine warningCoroutine;

    void Start()
    {
        // 레벨에 따른 최대 체력 설정
        SetMastLevel(mastLevel);
        hp = maxHp;

        // UI 초기화
        if (mastUI) mastUI.SetActive(false);

        // 버튼 이벤트 연결
        if (enhanceButton) enhanceButton.onClick.AddListener(EnhanceMast);
        if (closeButton) closeButton.onClick.AddListener(CloseUI);
    }

    void Update()
    {
        CheckPlayerDistance();
        HandleInput();
        UpdateUI();
        CheckMastCondition();
    }

    // 레벨 설정
    void SetMastLevel(int level)
    {
        mastLevel = Mathf.Clamp(level, 1, 2);
        maxHp = mastLevel == 1 ? 500 : 1000;
        if (hp > maxHp) hp = maxHp;
    }

    // 최대 갑판 개수 반환
    public int GetMaxDeckCount()
    {
        return mastLevel == 1 ? 30 : 50;
    }

    // 플레이어 거리 체크
    void CheckPlayerDistance()
    {
        Collider[] playersInRange = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        bool wasInRange = playerInRange;
        playerInRange = playersInRange.Length > 0;

        // 상태 변화시에만 UI 업데이트
        if (wasInRange != playerInRange)
        {
            if (!playerInRange && isUIOpen)
            {
                CloseUI();
            }
            ShowInteractionPrompt(playerInRange);
        }
    }

    // 입력 처리
    void HandleInput()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isUIOpen)
                CloseUI();
            else
                OpenUI();
        }
    }

    // 상호작용 프롬프트 표시
    void ShowInteractionPrompt(bool show)
    {
        if (interactionPrompt)
            interactionPrompt.SetActive(show);
    }

    // UI 열기
    void OpenUI()
    {
        isUIOpen = true;
        if (mastUI) mastUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // UI 닫기
    void CloseUI()
    {
        isUIOpen = false;
        if (mastUI) mastUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // UI 업데이트
    void UpdateUI()
    {
        if (!isUIOpen) return;

        // 현재 단계 표시
        if (currentStageText) currentStageText.text = $"현재 {mastLevel}단계";

        // 강화 효과 설명
        if (enhanceEffectText)
        {
            if (mastLevel == 1)
                enhanceEffectText.text = "2단계로 강화하면\n최대 갑판 설치 개수가\n30개에서 50개로 증가합니다";
            else
                enhanceEffectText.text = "이미 최대 단계입니다";
        }

        // 재료 개수 업데이트
        int currentWood = MastManager.Instance.GetItemCount(MastManager.Instance.woodItemID);
        int currentCloth = MastManager.Instance.GetItemCount(MastManager.Instance.clothItemID);

        // 통나무 개수 표시 (부족하면 빨간색)
        if (material1CountText)
        {
            material1CountText.text = $"{currentWood}/30";
            material1CountText.color = currentWood >= 30 ? Color.white : Color.red;
        }

        // 천 개수 표시 (부족하면 빨간색)
        if (material2CountText)
        {
            material2CountText.text = $"{currentCloth}/15";
            material2CountText.color = currentCloth >= 15 ? Color.white : Color.red;
        }

        // 강화 버튼 활성화/비활성화
        if (enhanceButton)
        {
            bool canEnhance = mastLevel < 2 && currentWood >= 30 && currentCloth >= 15;
            enhanceButton.interactable = canEnhance;
        }
    }

    // 돛대 상태 체크
    void CheckMastCondition()
    {
        float hpPercentage = (float)hp / maxHp;

        // 50% 이하일 때 경고 메시지
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

    // 경고 메시지 코루틴
    IEnumerator ShowWarningMessage()
    {
        while (true)
        {
            ShowMessage("돛대가 불안정해 보인다.", 4f);
            yield return new WaitForSeconds(10f);
        }
    }

    // 메시지 표시
    public void ShowMessage(string message, float duration)
    {
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    // 메시지 코루틴
    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        if (messagePanel) messagePanel.SetActive(true);
        if (messageText) messageText.text = message;

        yield return new WaitForSeconds(duration);

        if (messagePanel) messagePanel.SetActive(false);
        messageCoroutine = null;
    }

    // 강화하기
    void EnhanceMast()
    {
        int currentWood = MastManager.Instance.GetItemCount(MastManager.Instance.woodItemID);
        int currentCloth = MastManager.Instance.GetItemCount(MastManager.Instance.clothItemID);

        if (mastLevel >= 2)
        {
            ShowMessage("이미 최대 단계입니다.", 3f);
            return;
        }

        if (currentWood < 30 || currentCloth < 15)
        {
            ShowMessage("재료가 부족합니다.", 3f);
            return;
        }

        // 자원 소모
        if (!MastManager.Instance.ConsumeItems(MastManager.Instance.woodItemID, 30) ||
            !MastManager.Instance.ConsumeItems(MastManager.Instance.clothItemID, 15))
        {
            ShowMessage("아이템 소모에 실패했습니다.", 3f);
            return;
        }

        // 레벨업
        SetMastLevel(mastLevel + 1);
        hp = maxHp; // 강화시 체력 회복

        ShowMessage("돛대가 강화되었습니다!", 3f);

        // 인벤토리 UI 새로고침
        if (InventoryUiMain.instance != null)
        {
            InventoryUiMain.instance.IconRefresh();
        }
    }

    // 데미지 받기 오버라이드
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

    // 파괴시 처리 오버라이드
    public override void WhenDestroy()
    {
        Debug.Log("돛대 파괴됨 - 게임오버");
        MastManager.Instance.GameOver();
    }
}