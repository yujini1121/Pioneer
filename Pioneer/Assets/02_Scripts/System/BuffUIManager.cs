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
    Lethargy            // 무기력
}

public class BuffUIManager : MonoBehaviour
{
    public static BuffUIManager Instance;

    [Header("UI Transforms")]
    [Tooltip("버프와 특수버프가 생성될 위쪽 패널")]
    public Transform buffParent;

    [Tooltip("디버프가 생성될 아래쪽 패널")]
    public Transform debuffParent;

    [Header("UI Prefab")]
    public GameObject effectIconPrefab;

    [Header("Sprites - Buff & Special")]
    public Sprite iconFull;       // 배부름
    public Sprite iconDrunk;      // 만취

    [Header("Sprites - Debuffs")]
    public Sprite iconConfusion;  // 혼란
    public Sprite iconCharm;      // 매혹
    public Sprite iconHungry;     // 배고픔
    public Sprite iconStarving;   // 굶주림
    public Sprite iconPanic;      // 패닉
    public Sprite iconDisarm;     // 무장 해제
    public Sprite iconLethargy;   // 무기력

    private Dictionary<EffectType, GameObject> activeEffects = new Dictionary<EffectType, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    // =================================================================
    // 외부에서 UI 띄울 때 호출하는 함수
    // =================================================================
    public void BeginUI(EffectType type, bool isBuff)
    {
        // 이미 해당 효과가 켜져있다면 중복 생성하지 않음
        if (activeEffects.ContainsKey(type)) return;

        // 버프(특수버프 포함)면 위쪽(buffParent), 디버프면 아래쪽(debuffParent)에 생성
        Transform parent = isBuff ? buffParent : debuffParent;
        GameObject iconObj = Instantiate(effectIconPrefab, parent);

        // 생성된 프리팹의 이미지 컴포넌트에 알맞은 스프라이트 적용
        Image img = iconObj.GetComponent<Image>();
        switch (type)
        {
            // 버프 & 특수버프
            case EffectType.Fullness_Full: img.sprite = iconFull; break;
            case EffectType.Drunk: img.sprite = iconDrunk; break;

            // 디버프
            case EffectType.Confusion: img.sprite = iconConfusion; break;
            case EffectType.Charm: img.sprite = iconCharm; break;
            case EffectType.Fullness_Hungry: img.sprite = iconHungry; break;
            case EffectType.Fullness_Starving: img.sprite = iconStarving; break;
            case EffectType.Panic: img.sprite = iconPanic; break;
            case EffectType.Disarm: img.sprite = iconDisarm; break;
            case EffectType.Lethargy: img.sprite = iconLethargy; break;
        }

        // 딕셔너리에 등록
        activeEffects.Add(type, iconObj);
    }

    // =================================================================
    // 외부에서 UI 끌 때 호출하는 함수
    // =================================================================
    public void EndUI(EffectType type)
    {
        // 해당 효과가 딕셔너리에 있다면 파괴하고 리스트에서 제거
        if (activeEffects.TryGetValue(type, out GameObject iconObj))
        {
            Destroy(iconObj);
            activeEffects.Remove(type);
        }
    }
}