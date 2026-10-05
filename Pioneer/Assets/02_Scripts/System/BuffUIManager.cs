using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 기획서에 있는 모든 상태 이상 타입 정의
public enum EffectType
{
    None,

    // --- 버프 & 특수버프 (위쪽 패널) ---
    Fullness_Full,      // 배부름
    Drunk,              // 만취 (특수버프)

    // --- 디버프 (아래쪽 패널) ---
    Confusion,          // 혼란
    Charm,              // 매혹
    Fullness_Hungry,    // 배고픔
    Fullness_Starving,  // 굶주림
    Panic,              // 패닉
    Disarm,             // 무장 해제
    Lethargy,           // 무기력
    Mental_Unstable
}

public class BuffUIManager : MonoBehaviour
{
    public static BuffUIManager Instance;
    public Transform buffParent;
    public Transform debuffParent;
    [SerializeField] private RectTransform tooltipLayer;
    public GameObject effectIconPrefab;
    public Sprite iconFull;
    public Sprite iconDrunk;
    public Sprite iconConfusion;
    public Sprite iconCharm;
    public Sprite iconHungry;
    public Sprite iconStarving;
    public Sprite iconPanic;
    public Sprite iconDisarm;
    public Sprite iconLethargy;
    public Sprite iconMentalUnstable;

    private readonly Dictionary<EffectType, GameObject> activeEffects = new Dictionary<EffectType, GameObject>();
    private EffectType desiredFullness = EffectType.None;
    private static readonly EffectType[] fullnessTypes =
        { EffectType.Fullness_Full, EffectType.Fullness_Hungry, EffectType.Fullness_Starving };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnEnable() { if (Instance == null) Instance = this; }

    private void LateUpdate()
    {
        if (Instance != this) return;
        var player = PlayerCore.Instance;
        if (player == null || !player.isActiveAndEnabled || player.IsDead
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive))
        {
            ClearAll();
            return;
        }
        player.RefreshStatusEffectUI();
    }

    public void BeginUI(EffectType type, bool isBuff)
    {
        if (IsFullness(type)) { SetFullnessUI(type); return; }
        Show(type, isBuff);
    }

    public void SetFullnessUI(EffectType type)
    {
        desiredFullness = IsFullness(type) ? type : EffectType.None;
        foreach (var other in fullnessTypes)
            if (other != desiredFullness) EndUI(other);
        ShowDesiredFullness();
    }

    private void ShowDesiredFullness()
    {
        if (desiredFullness == EffectType.None) return;
        foreach (var other in fullnessTypes)
            if (other != desiredFullness && activeEffects.ContainsKey(other)) return;
        Show(desiredFullness, desiredFullness == EffectType.Fullness_Full);
    }

    private void Show(EffectType type, bool isBuff)
    {
        if (!isActiveAndEnabled || Instance != this || effectIconPrefab == null
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        if (!Presentation(type, out var sprite, out var label, out var description)) return;
        var parent = isBuff ? buffParent : debuffParent;
        if (parent == null) return;
        if (activeEffects.TryGetValue(type, out var existing) && existing != null)
        {
            var view = existing.GetComponent<StatusEffectIconUI>();
            if (view.IsRemoving) view.Show(type, sprite, label, description, isBuff);
            return;
        }
        var icon = Instantiate(effectIconPrefab, parent, false);
        activeEffects[type] = icon;
        var newView = icon.GetComponent<StatusEffectIconUI>();
        newView.SetTooltipLayer(tooltipLayer);
        newView.Show(type, sprite, label, description, isBuff);
    }

    public void SetRemainingTime(EffectType type, float remaining)
    {
        if (activeEffects.TryGetValue(type, out var icon) && icon != null)
            icon.GetComponent<StatusEffectIconUI>().SetRemainingTime(remaining);
    }

    public void EndUI(EffectType type)
    {
        if (desiredFullness == type) desiredFullness = EffectType.None;
        if (!activeEffects.TryGetValue(type, out var icon)) return;
        if (icon == null) { activeEffects.Remove(type); ShowDesiredFullness(); return; }
        var view = icon.GetComponent<StatusEffectIconUI>();
        if (view.IsRemoving) return;
        view.Hide(() =>
        {
            if (!activeEffects.TryGetValue(type, out var current) || current != icon) return;
            activeEffects.Remove(type);
            icon.SetActive(false);
            Destroy(icon);
            ShowDesiredFullness();
        });
    }

    public void ClearAll()
    {
        desiredFullness = EffectType.None;
        foreach (var icon in activeEffects.Values)
        {
            if (icon == null) continue;
            icon.GetComponent<StatusEffectIconUI>().ResetVisuals();
            icon.SetActive(false);
            Destroy(icon);
        }
        activeEffects.Clear();
    }

    private bool Presentation(EffectType type, out Sprite sprite, out string label, out string description)
    {
        sprite = null; label = description = "";
        switch (type)
        {
            case EffectType.Fullness_Full:
                sprite = iconFull; label = "배부름"; description = "이동 속도가 증가합니다."; break;
            case EffectType.Fullness_Hungry:
                sprite = iconHungry; label = "배고픔"; description = "허기가 낮습니다."; break;
            case EffectType.Fullness_Starving:
                sprite = iconStarving; label = "굶주림"; description = "이동 속도가 감소하고 체력을 잃습니다."; break;
            case EffectType.Drunk:
                sprite = iconDrunk; label = "만취"; description = "정신력 변화가 일시적으로 멈춥니다."; break;
            case EffectType.Mental_Unstable:
                sprite = iconMentalUnstable; label = "정신 불안"; description = "공격력이 감소합니다."; break;
        }
        return sprite != null;
    }
    private static bool IsFullness(EffectType type) => type == EffectType.Fullness_Full
        || type == EffectType.Fullness_Hungry || type == EffectType.Fullness_Starving;
    private void OnDisable() { ClearAll(); if (Instance == this) Instance = null; }
    private void OnDestroy() { ClearAll(); if (Instance == this) Instance = null; }
}
