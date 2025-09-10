using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

// PlayerLevelSystem : 전투, 제작, 채집 등 레벨과 경험치 관리

#region 성장 스테이터스 기획 요약
/* =============================================================
    [[ 전투 레벨 ]] => 에너미 베이스에서 해야하나..
- 에너미가 플레이어 근접 공격으로 인해 사망할때 CombatExp 획득 (막타)
    - 에너미 스크립트에서 attacker == Player && hp <= 0 일때 경험치 주는 함수 호출
- 에너미 hp를 플레이어가 근접 공격으로 40% 이상 깎았을때 CombatExp 획득 -> 그러면... 너무.. 어렵지 않나..? 계산이 너무 많이 들어가야하는데..?
    
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
- 일반 아이템 제작 완료시 craftExp 획득
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

public enum GrowStateType
{
    Combat,         // 전투
    Crafting,       // 제작
    Fishing,        // 낚시
}

[System.Serializable]
public class GrowState
{
    public GrowStateType Type;      // 스테이터스 종류
    public int level;               // 현재 레벨
    public float currentExp;        // 현재 보유중인 경험치 값
    public int[] maxExp;            // 레벨에 따른 경험치 최대값

    public GrowState(GrowStateType type, int[] maxExp)
    {
        this.Type = type;
        this.maxExp = maxExp;
        this.level = 0;
        this.currentExp = 0;
    }
}

public class PlayerStatsLevel : MonoBehaviour
{
    public static PlayerStatsLevel instance { get; private set; }

    public Dictionary<GrowStateType, GrowState> growStates = new Dictionary<GrowStateType, GrowState>();

    public PlayerCore player;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(instance);

            player = GetComponent<PlayerCore>();

        InitGrowState();
    }

    // 스테이터스 초기 상태 설정
    void InitGrowState()
    {
        growStates.Add(GrowStateType.Combat, new GrowState(GrowStateType.Combat, new int[] { 50, 100, 150, 200, 250 }));
        growStates.Add(GrowStateType.Crafting, new GrowState(GrowStateType.Crafting, new int[] { 50, 100, 150, 200, 250 }));
        growStates.Add(GrowStateType.Fishing, new GrowState(GrowStateType.Fishing, new int[] { 50, 100, 150, 200, 250 }));
    }

    /// <summary>
    /// 경험치 획득
    /// </summary>
    /// <param name="type">스테이터스 종류</param>
    /// <param name="amount">경험치 값</param>
    public void AddExp(GrowStateType type, int amount)
    {
        GrowState growState = growStates[type];

        if (growState.level >= growState.maxExp.Length)
            return;

        growState.currentExp += amount;

        while (growState.level < growState.maxExp.Length && growState.currentExp >= growState.maxExp[growState.level])
        {
            growState.currentExp -= growState.maxExp[growState.level];
            growState.level++;
            Debug.Log($"{type} 레벨업 -> {growState.level}");

            CombatChance(type);
        }
    }

    // 아래 switch 문들에서 defalut로 받는 값은 레벨 0일때 값
    // [[ 전투 ]] 레벨업 시 효과 적용
    private void CombatChance(GrowStateType type)
    {
        float increaseAttackDamage = 0f;
        if (type == GrowStateType.Combat)
        {
            // 레벨에 따라 공격력 상승, 무기 아이템 내구도 감소량 감소
            switch(growStates[GrowStateType.Combat].level)
            {
                case 1:
                    increaseAttackDamage = 0.1f;
                    break;
                case 2:
                    increaseAttackDamage = 0.15f;
                    break;
                case 3:
                    increaseAttackDamage = 0.20f;
                    break;
                case 4:
                    increaseAttackDamage = 0.25f;
                    break;
                case 5:
                    increaseAttackDamage = 0.30f;
                    break;
                default:
                    increaseAttackDamage = 0f;
                    break;
            }

            player.attackDamage = Mathf.RoundToInt(player.attackDamage * (1 + increaseAttackDamage));
        }
    }

    // 여기 보세요!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // 제작 확률이 정신력에서도 영향을 받고 있어서 현재 방법에서 수정이 필요할 듯 함
    // [[ 손재주 ]] 대성공 (제작) 확률 반환
    /// <summary>
    /// [[ 손재주 (아이템 제작) ]] 확률 적용
    /// </summary>
    /// <returns></returns>
    public float CraftingChance()
    {
        float greatSuccessChance = 0f;
        switch (growStates[GrowStateType.Crafting].level)
        {
            case 1:
                greatSuccessChance = 0.05f;
                break;
            case 2:
                greatSuccessChance = 0.1f;
                break;
            case 3:
                greatSuccessChance = 0.15f;
                break;
            case 4:
                greatSuccessChance = 0.2f;
                break;
            case 5:
                greatSuccessChance = 0.3f;
                break;
            default:
                greatSuccessChance = 0f;
                break;
        }
        return greatSuccessChance;
    }

    // [[ 낚시 ]] 추가 획득 및 보물 상자 추가 획득 확률
    /// <summary>
    /// [[ 낚시 ]] 파밍 재료 및 보물상자 추가 획득 확률 적용
    /// </summary>
    /// <returns></returns>
    public (float count, float chest) FishingChance()      // C#의 튜플이라는 방식의 구현
    {
        (float count, float chest) fishingChance = (0f, 0f);
        switch(growStates[GrowStateType.Fishing].level)
        {
            case 1:
                fishingChance = (0.05f, 0f);
                break;
            case 2:
                fishingChance = (0.07f, 0f);
                break;
            case 3:
                fishingChance = (0.1f, 0.3f);
                break;
            case 4:
                fishingChance = (0.12f, 0.4f);
                break;
            case 5:
                fishingChance = (0.15f, 0.5f);
                break;
            default:
                fishingChance = (0.0f, 0f);
                break;
        }
        return fishingChance;
    }

    // 정신력에 따른 대성공 제작 확률 감소는 어떻게 해야할까..?
    
}
