using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerStatUI : MonoBehaviour
{
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


    void Start()
    {
        InitUi();
    }

    void Update()
    {
        
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
    }

    void InitUi()
    {
        hpBar.maxValue = PlayerCore.Instance.maxHp;
        fullnessBar.maxValue = PlayerCore.Instance.maxFullness;
        mentalBar.maxValue = PlayerCore.Instance.maxMental;

        UpdateHpUI(PlayerCore.Instance.hp);
        UpdateFullnessUI(PlayerCore.Instance.currentFullness);
        UpdateMentalUI(PlayerCore.Instance.currentMental);
        UpdateBasicStatUI();
        UpdatePlayerGrowStatUI(GrowStatType.Combat);
        UpdatePlayerGrowStatUI(GrowStatType.Crafting);
        UpdatePlayerGrowStatUI(GrowStatType.Fishing);
    }

    void UpdateFullnessUI(int currentFullness)
    {
        fullnessBar.value = currentFullness;
    }
    
    void UpdateMentalUI(int currentMental)
    {
        mentalBar.value = currentMental;
    }

    void UpdateHpUI(int currentHp)
    {
        playerHp.text = $"{currentHp}";
        hpBar.value = currentHp;
    }

    void UpdateBasicStatUI()
    {
        playerAttackDamage.text = $"{PlayerCore.Instance.attackDamage}";
        playerAttackSpeed.text = $"{PlayerCore.Instance.attackDelayTime}";
        playerAttackRange.text = $"{PlayerCore.Instance.attackRange}";
    }

    void UpdatePlayerGrowStatUI(GrowStatType type)
    {
        PlayerStatsLevel statLevel = PlayerStatsLevel.Instance;
        GrowState state = statLevel.growStates[type];
        int currentLv = state.level;

        switch (type)
        {
            case GrowStatType.Combat:
                combatLevel.text = $"{currentLv}";     // 전투 레벨                
                additionCombat.text = $"{statLevel.combatList[currentLv].attack * 100:F0} %";        // 공격력 + 추가 공격력 퍼센트                
                additionCombat_WeaponDurability.text = $"{statLevel.combatList[currentLv].durability} %";   // 무기 내구도 감소치 + 추가 무기 내구도 감소치
                break;
            case GrowStatType.Crafting:
                craftingLevel.text = $"{currentLv}";     // 제작 레벨
                // 대성공 확률 + 추가 대성공 확률
                additionCrafting.text = $"{statLevel.craftingList[currentLv] * 100:F0} %";
                break;
            case GrowStatType.Fishing:
                fishingLevel.text = state.level.ToString();    // 낚시 레벨
                // 재료 추가 획득 확률 + 추가 획득 확률
                additionFishing_AddIngredients.text = $"{statLevel.fishingList[currentLv].count * 100:F0} %";
                // 보물상자 획득 확률 + 추가 획득 확률
                additionFishing_TreasureChest.text = $"{statLevel.fishingList[currentLv].chest * 100:F0} %";                
                break;

        }
    }
}
