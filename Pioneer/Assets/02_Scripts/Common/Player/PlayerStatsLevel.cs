using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;

// PlayerLevelSystem : 전투, 제작, 채집 등 레벨과 경험치 관리
// TODO : 낚시를 제외한 전투, 제작은 경험치 획득 코드 추가 완료, 낚시 경험치 추가해야함

#region 성장 스테이터스 기획 요약
/* =============================================================
- switch문들 리스트 방식으로 바꿔야함 

    [[ 전투 레벨 ]] => 에너미 베이스에서 해야하나..
- 에너미를 한 대 직접 때릴때마다, 경험치 획득 *
    
    { 에너미 처치시 획득 가능한 경험치 량 }
    * 둥지 : 3
    * 미니언 : 5
    * 좀비 승무원 : 5                => 좀비 승무원은 에너미가 아니라 따로 추가 구현 들어가야할듯..
    * 타이탄 : 8
    * 크롤러 : 10
     
    { 전투 레벨 }
    * 0 : 효과 없음
    * 1 : 공격력 10% 상승 / 무기 아이템 내구도 감소량 -0.1
    * 2 : 공격력 15% 상승 / 무기 아이템 내구도 감소량 -0.3
    * 3 : 공격력 20% 상승 / 무기 아이템 내구도 감소량 -0.5
    * 4 : 공격력 25% 상승 / 무기 아이템 내구도 감소량 -0.8
    * 5 : 공격력 30% 상승 / 무기 아이템 내구도 감소량 -1.0
==================================================================   
==================================================================  
    [[ 손재주 ]] => 태윤씨한테 질문해야할듯..
- 설치형 오브젝트를 설치 완료 했을때 craftExp 획득
- 일반 아이템 제작 완료시 craftExp 획득 *
    - 제작된 후 실물이 존재해야함
    - 제작하는데 필요한 아이템이 1이상 소모
    
    { 제작 레시피에 따른 경험치 량 }
    * 일반 재료 아이템 : 5
    * 소비형 아이템 : 10
    * 설치형 오브젝트 : 15
    * 갑판 : 4
    
    { 손재주 레벨 } 
    * 0 : 효과 없음
    * 1 : 대성공 확률 5%
    * 2 : 대성공 확률 10%
    * 3 : 대성공 확률 15%
    * 4 : 대성공 확률 20%
    * 5 : 대성공 확률 30%
    
    // 여기 보세요!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // 대성공 시스템 제작해야함;
    = 대성공이란? = 
    - 아이템 제작시 1개 더 획득
    - 제작시 소모해야 할 재료 아이템 40% 페이백..?
==================================================================    
==================================================================  
    [[ 낚시 레벨 ]] => 낚시는..?
- 플레이어가 직접 낚시를 통해 일반 아이템을 획득할 경우 gratheringExp 획득

    { 낚시를 통한 아이템 획득에 따른 경험치 량 }
    * 낚시로 얻을 수 있는 아이템 : 5
    * 보물상자 : 10
    
    { 낚시 레벨 }
    * 0 : 효과 없음
    * 1 : 5% 확률로 자원 1개 추가 획득
    * 2 : 7% 확률로 자원 1개 추가 획득
    * 3 : 10% 확률로 자원 1개 추가 획득 / 30% 확률로 보물상자 1개 획득 (낚시로 보물상자를 얻었어도 받을 수 있음
    * 4 : 12% 확률로 자원 1개 추가 획득 / 40% 확률로 보물상자 1개 획득 (낚시로 보물상자를 얻었어도 받을 수 있음
    * 5 : 15% 확률로 자원 1개 추가 획득 / 50% 확률로 보물상자 1개 획득 (낚시로 보물상자를 얻었어도 받을 수 있음
============================================================= */
#endregion

public enum GrowStatType
{
    Combat,         // 전투
    Crafting,       // 제작
    Fishing,        // 낚시
}

[System.Serializable]
public class GrowState
{
    public GrowStatType Type;      // 스테이터스 종류
    public int level;               // 현재 레벨
    public float currentExp;        // 현재 보유중인 경험치 값
    public int[] maxExp;            // 레벨에 따른 경험치 최대값

    public GrowState(GrowStatType type, int[] maxExp)
    {
        this.Type = type;
        this.maxExp = maxExp;
        this.level = 0;
        this.currentExp = 0;
    }
}

// 레벨업으로 인한 확률 switch 부분 리스트로 변경하기
public class PlayerStatsLevel : MonoBehaviour
{
    public static PlayerStatsLevel Instance { get; private set; }

    public Dictionary<GrowStatType, GrowState> growStates = new Dictionary<GrowStatType, GrowState>();

    public PlayerCore player;

    public List<(float attack, float durability)> combatList 
        = new List<(float attack, float durability)> { (0f, 0f), (0.10f, -0.1f), (0.15f, -0.3f), (0.20f, -0.5f), (0.25f, -0.8f), (0.30f, -1) };
    public List<float> craftingList = new List<float> { 0f, 0.05f, 0.10f, 0.15f, 0.20f, 0.30f };
    public List<(float count, float chest)> fishingList 
        = new List<(float count, float chest)> { (0.0f, 0f), (0.05f, 0f), (0.1f, 0.3f), (0.12f, 0.4f), (0.15f, 0.5f) };

    public static event Action<GrowStatType> StatLevelUp;
     

    // =============== 디버깅용 인스펙터창에서 레벨과 경험치들 보이도록 ==================
    [SerializeField] private List<GrowState> growStateForInspector;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(Instance);

            player = GetComponent<PlayerCore>();

        InitGrowState();
    }

    // =============== 디버깅용 인스펙터창에서 레벨과 경험치들 보이도록 ==================
    private void Update()
    {
        if (Application.isEditor)
        {
            growStateForInspector = new List<GrowState>(growStates.Values);
        }

        if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.F8)) growStates[GrowStatType.Combat].level++;
            if (Input.GetKeyDown(KeyCode.F9)) growStates[GrowStatType.Crafting].level++;
            if (Input.GetKeyDown(KeyCode.F10)) growStates[GrowStatType.Fishing].level++;
		}
    }

    // 스테이터스 초기 상태 설정
    void InitGrowState()
    {
        growStates.Clear();
        growStates.Add(GrowStatType.Combat, new GrowState(GrowStatType.Combat, new int[] { 50, 100, 150, 200, 250 }));
        growStates.Add(GrowStatType.Crafting, new GrowState(GrowStatType.Crafting, new int[] { 50, 100, 150, 200, 250 }));
        growStates.Add(GrowStatType.Fishing, new GrowState(GrowStatType.Fishing, new int[] { 60, 90, 120, 150, 180 }));
    }

    /// <summary>
    /// 경험치 획득
    /// </summary>
    /// <param name="type">스테이터스 종류</param>
    /// <param name="amount">경험치 값</param>
    public void AddExp(GrowStatType type, int amount)
    {
        UnityEngine.Debug.Log($"AddExp() 시작");
        GrowState growState = growStates[type];

        if (growState.level >= growState.maxExp.Length)
            return;

        growState.currentExp += amount;

        while (growState.level < growState.maxExp.Length && growState.currentExp >= growState.maxExp[growState.level])
        {
            growState.currentExp -= growState.maxExp[growState.level];
            growState.level++;
            UnityEngine.Debug.Log($"{type} 레벨업 -> {growState.level}");

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.LevelUp);

            // switch 문으로 수정
            if (type == GrowStatType.Combat)
            {
                CombatLevelUp(type); // 레벨 업 순간
            }
            // ===========================================
            StatLevelUp?.Invoke(type); // ui 업데이트 이벤튼
        }
        UnityEngine.Debug.Log($"{type} 스탯 경험치 {amount} 획득");
    }

    /// <summary>
    /// [[ 전투 ]] 레벨업 시 효과 적용
    /// </summary>
    /// <param name="type"></param>
    /// 호출 시점 : 경험치를 얻는 시점 && 레벨 업 / not 공격력을 얻
    private void CombatLevelUp(GrowStatType type)
    {
        int combatLevel = growStates[GrowStatType.Combat].level;
        float increaseAttackDamage = 0f;

        /*if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.LevelUp);*/

        if (combatLevel >= 0 && combatLevel < combatList.Count)
        {
            increaseAttackDamage = combatList[combatLevel].attack;
        }

        player.duabilityReducePrevent += combatList[combatLevel].durability;

        int prevDamage = (int)player.handAttackCurrentValueRaw.weaponDamage;

        player.handAttackCurrentValueRaw.weaponDamage =
            Mathf.RoundToInt(prevDamage * (1 + increaseAttackDamage)); // 레벨 업에 따른 원본 변경

		//player.attackDamage = Mathf.RoundToInt(player.attackDamage * (1 + increaseAttackDamage));
	}

    /// <summary>
    /// [[ 손재주 (아이템 제작) ]] 확률 적용
    /// </summary>
    /// <returns></returns>
    public float CraftingChance()
    {
        int level = growStates[GrowStatType.Crafting].level;
        float greatSuccessChance = 0f;

        /*if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.LevelUp);*/

        if (level >= 0 &&  level < craftingList.Count)
        {
            greatSuccessChance = craftingList[level];
        }         

        return PlayerCore.Instance.IsMentalDebuff() ? (greatSuccessChance * 6) / 10 : greatSuccessChance;
    }

    /// <summary>
    /// [[ 낚시 ]] 파밍 재료 및 보물상자 추가 획득 확률 적용
    /// </summary>
    /// <returns></returns>
    public (float count, float chest) FishingChance()      // C#의 튜플이라는 방식의 구현
    {
        int level = growStates[GrowStatType.Fishing].level;

        /*if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.LevelUp);*/

        if (level >= 0 && level < fishingList.Count)
        {
            return fishingList[level]; // 정상 출력
        }

        return (0.0f, 0f); // 이건 아마 에러용
    }




}
