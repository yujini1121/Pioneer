using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerStatUI : MonoBehaviour
{
    public static PlayerStatUI Instance;

    [Header("Basic Stats UI")]
    public TextMeshProUGUI playerHp;
    public TextMeshProUGUI playerAttackDamage;
    public TextMeshProUGUI playerAttackSpeed;
    public TextMeshProUGUI playerAttackRange;

    [Header("Grow Stats Combat UI")]
    public UnityEngine.UI.Image combatIcon;
    public TextMeshProUGUI combatLevel;
    public TextMeshProUGUI additionCombat;
    public TextMeshProUGUI additionCombat_WeaponDurability;

    [Header("Grow Stats Crafting UI")]
    public UnityEngine.UI.Image craftingIcon;
    public TextMeshProUGUI craftingLevel;
    public TextMeshProUGUI additionCrafting;

    [Header("Grow Stats Fishing UI")]
    public UnityEngine.UI.Image fishingIcon;
    public TextMeshProUGUI fishingLevel;
    public TextMeshProUGUI additionFishing_TreasureChest;
    public TextMeshProUGUI additionFishing_AddIngredients;

    [Header("Bar UI")]
    public Slider hpBar;
    public Slider fullnessBar;
    public Slider mentalBar;
    public Slider guiltyBar;

#region
    [Header("Bar Icons")]
    [SerializeField] private Transform hpIcon;
    [SerializeField] private Transform fullnessIcon;
    [SerializeField] private Transform mentalIcon;
    [SerializeField] private Transform guiltyIcon;

    [Header("Bar Feedback")]
    [SerializeField] private float deltaFeedbackHoldDuration = 0.8f;
    [SerializeField] private float barFollowDuration = 0.25f;
    [SerializeField] private float deltaFeedbackFadeDuration = 0.35f;

    private readonly StatBarTweenState hpState = new StatBarTweenState();
    private readonly StatBarTweenState fullnessState = new StatBarTweenState();
    private readonly StatBarTweenState mentalState = new StatBarTweenState();
    private readonly StatBarTweenState guiltyState = new StatBarTweenState();
    private static Material deltaFeedbackMaterial;

    private class StatBarTweenState
    {
        public int LastValue = int.MinValue;
        public float FeedbackAnchorValue;
        public bool HasFeedback;
        public Transform Icon;
        public Image DeltaImage;
        public CanvasGroup DeltaCanvasGroup;
        public Sequence Sequence;
        public float DeltaVisualFrom;
        public float DeltaVisualTo;
        public Vector3 IconBaseScale;
        public Vector2 IconBaseAnchoredPosition;
        public bool HasIconBaseTransform;
    }
#endregion

    private void Awake()
	{
        Instance = this;
	}

	void Start()
    {
        InitUi();
    }

    void Update()
    {
        UpdateUI();
    }

#warning 버그 터지면 수정해야할 부분
    private void OnEnable()
    {
        PlayerStatsLevel.StatLevelUp += UpdatePlayerGrowStatUI;
        PlayerCore.PlayerHpChanged += UpdateHpUI;
        PlayerCore.PlayerFullnessChanged += UpdateFullnessUI;
        PlayerCore.PlayerMentalChanged += UpdateMentalUI;
    }

    private void OnDisable()
    {
        PlayerStatsLevel.StatLevelUp -= UpdatePlayerGrowStatUI;
        PlayerCore.PlayerHpChanged -= UpdateHpUI;
        PlayerCore.PlayerFullnessChanged -= UpdateFullnessUI;
        PlayerCore.PlayerMentalChanged -= UpdateMentalUI;

        CleanupBarState(hpState);
        CleanupBarState(fullnessState);
        CleanupBarState(mentalState);
        CleanupBarState(guiltyState);
    }

    void UpdateUI()
    {
        if (PlayerCore.Instance == null || GuiltySystem.instance == null)
            return;

        UpdateHpUI(PlayerCore.Instance.hp);
        UpdateFullnessUI(PlayerCore.Instance.currentFullness);
        UpdateMentalUI(PlayerCore.Instance.CurrentMental);
        UpdateBasicStatUI();
        UpdateGuiltyUI(GuiltySystem.instance.currentAttackWeight);
        UpdatePlayerGrowStatUI(GrowStatType.Combat);
        UpdatePlayerGrowStatUI(GrowStatType.Crafting);
        UpdatePlayerGrowStatUI(GrowStatType.Fishing);
    }
    

    void InitUi()
    {
        if (PlayerCore.Instance == null || GuiltySystem.instance == null)
            return;

        hpBar.maxValue = PlayerCore.Instance.maxHp;
        fullnessBar.maxValue = PlayerCore.Instance.maxFullness;
        mentalBar.maxValue = PlayerCore.Instance.maxMental;
        guiltyBar.maxValue = GuiltySystem.instance.maxAttackWeight;
        EnsureBarState(hpBar, hpState, hpIcon);
        EnsureBarState(fullnessBar, fullnessState, fullnessIcon);
        EnsureBarState(mentalBar, mentalState, mentalIcon);
        EnsureBarState(guiltyBar, guiltyState, guiltyIcon);
        UpdateHpUI(PlayerCore.Instance.hp);
        UpdateFullnessUI(PlayerCore.Instance.currentFullness);
        UpdateMentalUI(PlayerCore.Instance.CurrentMental);
        UpdateBasicStatUI();
        UpdatePlayerGrowStatUI(GrowStatType.Combat);
        UpdatePlayerGrowStatUI(GrowStatType.Crafting);
        UpdatePlayerGrowStatUI(GrowStatType.Fishing);
    }

    void UpdateFullnessUI(int currentFullness)
    {
        AnimateSlider(fullnessBar, currentFullness, fullnessState, fullnessIcon);
    }
    
    void UpdateMentalUI(int currentMental)
    {
        AnimateSlider(mentalBar, currentMental, mentalState, mentalIcon);
    }

    void UpdateHpUI(int currentHp)
    {
        playerHp.text = $"{currentHp}";
        AnimateSlider(hpBar, currentHp, hpState, hpIcon);
    }

    void UpdateGuiltyUI(int currentGuilty)
    {
        AnimateSlider(guiltyBar, currentGuilty, guiltyState, guiltyIcon);
    }

#region
    private void AnimateSlider(Slider slider, int value, StatBarTweenState state, Transform preferredIcon)
    {
        if (slider == null)
            return;

        EnsureBarState(slider, state, preferredIcon);

        if (state.LastValue == int.MinValue)
        {
            slider.value = value;
            state.LastValue = value;
            HideDeltaFeedback(state);
            return;
        }

        if (state.LastValue == value)
            return;

        int previousValue = state.LastValue;
        state.LastValue = value;
        bool isDecrease = value < previousValue;
        bool isIncrease = value > previousValue;
        float visualStartValue = slider.value;
        float feedbackStartValue = previousValue;

        slider.DOKill();
        state.Sequence?.Kill();

        if (state.HasFeedback)
        {
            feedbackStartValue = isDecrease
                ? Mathf.Max(state.FeedbackAnchorValue, visualStartValue, previousValue)
                : Mathf.Min(state.FeedbackAnchorValue, visualStartValue, previousValue);
        }

        state.FeedbackAnchorValue = feedbackStartValue;
        state.HasFeedback = true;
        slider.value = visualStartValue;

        ShowDeltaFeedback(slider, state, feedbackStartValue, value);
        AnimateStatIcon(state.Icon, isDecrease, isIncrease);

        state.Sequence = DOTween.Sequence();
        state.Sequence.AppendInterval(deltaFeedbackHoldDuration);
        state.Sequence.Append(slider.DOValue(value, barFollowDuration).SetEase(Ease.OutCubic));
        Tween deltaCollapseTween = CreateDeltaCollapseTween(state, barFollowDuration);
        if (deltaCollapseTween != null)
            state.Sequence.Join(deltaCollapseTween);
        if (state.DeltaCanvasGroup != null)
            state.Sequence.Join(state.DeltaCanvasGroup.DOFade(0.0f, deltaFeedbackFadeDuration).SetEase(Ease.InCubic));
        state.Sequence.OnComplete(() =>
        {
            slider.value = value;
            state.HasFeedback = false;
        });
    }

    private void EnsureBarState(Slider slider, StatBarTweenState state, Transform preferredIcon)
    {
        if (slider == null || state == null)
            return;

        if (state.Icon == null)
            state.Icon = preferredIcon != null ? preferredIcon : FindNearbyIcon(slider);

        CacheIconBaseTransform(state);

        if (state.DeltaImage != null)
            return;

        RectTransform fillRect = slider.fillRect;
        if (fillRect == null)
            return;

        Image fillImage = fillRect.GetComponent<Image>();
        RectTransform parent = fillRect.parent as RectTransform;
        if (fillImage == null || parent == null)
            return;

        GameObject deltaObject = new GameObject($"{slider.name}_DeltaFeedback", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform deltaRect = deltaObject.GetComponent<RectTransform>();
        deltaRect.SetParent(parent, false);
        deltaRect.anchorMin = new Vector2(0.0f, fillRect.anchorMin.y);
        deltaRect.anchorMax = new Vector2(0.0f, fillRect.anchorMax.y);
        deltaRect.offsetMin = new Vector2(0.0f, fillRect.offsetMin.y);
        deltaRect.offsetMax = new Vector2(0.0f, fillRect.offsetMax.y);
        deltaRect.SetAsLastSibling();

        Image deltaImage = deltaObject.GetComponent<Image>();
        deltaImage.sprite = fillImage.sprite;
        deltaImage.type = fillImage.type;
        deltaImage.color = new Color(1.0f, 1.0f, 1.0f, 0.9f);
        deltaImage.material = GetDeltaFeedbackMaterial();
        deltaImage.raycastTarget = false;

        CanvasGroup deltaCanvasGroup = deltaObject.GetComponent<CanvasGroup>();
        deltaCanvasGroup.alpha = 0.0f;
        deltaCanvasGroup.interactable = false;
        deltaCanvasGroup.blocksRaycasts = false;

        state.DeltaImage = deltaImage;
        state.DeltaCanvasGroup = deltaCanvasGroup;
    }

    private Material GetDeltaFeedbackMaterial()
    {
        if (deltaFeedbackMaterial != null)
            return deltaFeedbackMaterial;

        Shader shader = Resources.Load<Shader>("UIWhiteAlphaMask");
        if (shader == null)
            shader = Shader.Find("Pioneer/UI/WhiteAlphaMask");

        if (shader == null)
            return null;

        deltaFeedbackMaterial = new Material(shader)
        {
            name = "Runtime UI White Alpha Mask"
        };

        return deltaFeedbackMaterial;
    }

    private Transform FindNearbyIcon(Slider slider)
    {
        Transform current = slider.transform;
        Transform bestIcon = null;
        float bestDistance = float.MaxValue;

        for (int depth = 0; depth < 3 && current != null; depth++)
        {
            Image[] images = current.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null || image.transform == slider.fillRect)
                    continue;

                string lowerName = image.gameObject.name.ToLower();
                if (lowerName.Contains("icon"))
                {
                    float distance = Vector3.SqrMagnitude(image.transform.position - slider.transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIcon = image.transform;
                    }
                }
            }

            current = current.parent;
        }

        return bestIcon;
    }

    private void ShowDeltaFeedback(Slider slider, StatBarTweenState state, float fromValue, float toValue)
    {
        if (state.DeltaImage == null || state.DeltaCanvasGroup == null)
            return;

        RectTransform rectTransform = state.DeltaImage.rectTransform;
        float from = NormalizeSliderValue(slider, fromValue);
        float to = NormalizeSliderValue(slider, toValue);

        if (slider.direction == Slider.Direction.RightToLeft)
        {
            from = 1.0f - from;
            to = 1.0f - to;
        }

        state.DeltaVisualFrom = from;
        state.DeltaVisualTo = to;

        float min = Mathf.Min(from, to);
        float max = Mathf.Max(from, to);

        rectTransform.anchorMin = new Vector2(min, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(max, rectTransform.anchorMax.y);
        rectTransform.offsetMin = new Vector2(0.0f, rectTransform.offsetMin.y);
        rectTransform.offsetMax = new Vector2(0.0f, rectTransform.offsetMax.y);

        state.DeltaCanvasGroup.DOKill();
        state.DeltaCanvasGroup.alpha = Mathf.Approximately(min, max) ? 0.0f : 1.0f;
    }

    private Tween CreateDeltaCollapseTween(StatBarTweenState state, float duration)
    {
        if (state == null || state.DeltaImage == null)
            return null;

        RectTransform rectTransform = state.DeltaImage.rectTransform;
        bool movesLeft = state.DeltaVisualTo < state.DeltaVisualFrom;

        if (movesLeft)
        {
            float start = rectTransform.anchorMax.x;
            float end = rectTransform.anchorMin.x;
            return DOTween.To(
                () => start,
                value =>
                {
                    start = value;
                    rectTransform.anchorMax = new Vector2(value, rectTransform.anchorMax.y);
                },
                end,
                duration).SetEase(Ease.OutCubic);
        }
        else
        {
            float start = rectTransform.anchorMin.x;
            float end = rectTransform.anchorMax.x;
            return DOTween.To(
                () => start,
                value =>
                {
                    start = value;
                    rectTransform.anchorMin = new Vector2(value, rectTransform.anchorMin.y);
                },
                end,
                duration).SetEase(Ease.OutCubic);
        }
    }

    private float NormalizeSliderValue(Slider slider, float value)
    {
        if (Mathf.Approximately(slider.maxValue, slider.minValue))
            return 0.0f;

        return Mathf.Clamp01((value - slider.minValue) / (slider.maxValue - slider.minValue));
    }

    private void HideDeltaFeedback(StatBarTweenState state)
    {
        if (state == null || state.DeltaCanvasGroup == null)
            return;

        state.DeltaCanvasGroup.alpha = 0.0f;
        state.HasFeedback = false;
    }

    private void AnimateStatIcon(Transform icon, bool isDecrease, bool isIncrease)
    {
        if (icon == null)
            return;

        StatBarTweenState state = GetStateByIcon(icon);
        Vector3 baseScale = state != null && state.HasIconBaseTransform ? state.IconBaseScale : icon.localScale;

        icon.DOKill(false);
        icon.localScale = baseScale;
        icon.DOPunchScale(Vector3.one * (isDecrease ? 0.12f : 0.08f), 0.18f, 8, 0.7f)
            .OnKill(() => RestoreIconScale(state))
            .OnComplete(() => RestoreIconScale(state));

        RectTransform rectTransform = icon as RectTransform;
        if (rectTransform == null)
            return;

        Vector2 baseAnchoredPosition = state != null && state.HasIconBaseTransform
            ? state.IconBaseAnchoredPosition
            : rectTransform.anchoredPosition;
        Vector2 punch = isDecrease ? new Vector2(-8.0f, 0.0f) : new Vector2(6.0f, 0.0f);
        if (isIncrease == false && isDecrease == false)
            return;

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.DOPunchAnchorPos(punch, 0.18f, 8, 0.7f)
            .OnKill(() => RestoreIconAnchoredPosition(state))
            .OnComplete(() => RestoreIconAnchoredPosition(state));
    }

    private void CacheIconBaseTransform(StatBarTweenState state)
    {
        if (state == null || state.Icon == null || state.HasIconBaseTransform)
            return;

        state.IconBaseScale = state.Icon.localScale;
        RectTransform rectTransform = state.Icon as RectTransform;
        if (rectTransform != null)
            state.IconBaseAnchoredPosition = rectTransform.anchoredPosition;

        state.HasIconBaseTransform = true;
    }

    private void RestoreIconScale(StatBarTweenState state)
    {
        if (state == null || state.Icon == null || !state.HasIconBaseTransform)
            return;

        state.Icon.localScale = state.IconBaseScale;
    }

    private void RestoreIconAnchoredPosition(StatBarTweenState state)
    {
        if (state == null || state.Icon == null || !state.HasIconBaseTransform)
            return;

        RectTransform rectTransform = state.Icon as RectTransform;
        if (rectTransform != null)
            rectTransform.anchoredPosition = state.IconBaseAnchoredPosition;
    }

    private StatBarTweenState GetStateByIcon(Transform icon)
    {
        if (icon == null)
            return null;

        if (hpState.Icon == icon) return hpState;
        if (fullnessState.Icon == icon) return fullnessState;
        if (mentalState.Icon == icon) return mentalState;
        if (guiltyState.Icon == icon) return guiltyState;
        return null;
    }

    private void CleanupBarState(StatBarTweenState state)
    {
        if (state == null)
            return;

        state.Sequence?.Kill();
        state.Sequence = null;
        state.HasFeedback = false;

        if (state.DeltaCanvasGroup != null)
        {
            state.DeltaCanvasGroup.DOKill();
            state.DeltaCanvasGroup.alpha = 0.0f;
        }

        RestoreIconScale(state);
        RestoreIconAnchoredPosition(state);
    }
#endregion

    public void UpdateBasicStatUI()
    {
        SItemStack selectedItem = InventoryManager.Instance.SelectedSlotInventory;
        SItemWeaponTypeSO weaponOrNull = null;

		if (SItemStack.IsEmpty(selectedItem) == false)
        {
			int itemId = selectedItem.id;
			weaponOrNull =
				ItemTypeManager.Instance.itemTypeSearch[itemId] as SItemWeaponTypeSO;
		}


        if (weaponOrNull != null)
        {
			playerAttackDamage.text = $"{PlayerCore.Instance.CalculatedHandAttack.weaponDamage + weaponOrNull.weaponDamage}";
			playerAttackSpeed.text = $"{weaponOrNull.weaponDelay / 1f}";
			playerAttackRange.text = $"{weaponOrNull.weaponRange}";
		}
        else
        {
            // 무기가 아니기에 맨손 기준 적용

			playerAttackDamage.text = $"{PlayerCore.Instance.CalculatedHandAttack.weaponDamage}";
			playerAttackSpeed.text = $"{PlayerCore.Instance.attackDelayTime}";
			playerAttackRange.text = $"{PlayerCore.Instance.attackRange}";
		}

    }

    void UpdatePlayerGrowStatUI(GrowStatType type)
    {
        PlayerStatsLevel statLevel = PlayerStatsLevel.Instance;
        GrowState state = statLevel.growStates[type];
        int currentLv = state.level;

        switch (type)
        {
            case GrowStatType.Combat:
                combatLevel.text = $"Lv. {currentLv}";     // 전투 레벨                
                additionCombat.text = $"{statLevel.combatList[currentLv].attack:F1}";        // 공격력 + 추가 공격력 퍼센트                
                additionCombat_WeaponDurability.text = $"{statLevel.combatList[currentLv].durability:F1}";   // 무기 내구도 감소치 + 추가 무기 내구도 감소치
                break;
            case GrowStatType.Crafting:
                craftingLevel.text = $"Lv. {currentLv}";     // 제작 레벨
                // 대성공 확률 + 추가 대성공 확률
                additionCrafting.text = $"{statLevel.craftingList[currentLv]:F1}";
                break;
            case GrowStatType.Fishing:
                fishingLevel.text = "Lv. " + state.level.ToString();    // 낚시 레벨
                // 재료 추가 획득 확률 + 추가 획득 확률
                additionFishing_AddIngredients.text = $"{statLevel.fishingList[currentLv].count:F1}";
                // 보물상자 획득 확률 + 추가 획득 확률
                additionFishing_TreasureChest.text = $"{statLevel.fishingList[currentLv].chest:F1}";                
                break;

        }

        UpdateBasicStatUI();
    }
}


