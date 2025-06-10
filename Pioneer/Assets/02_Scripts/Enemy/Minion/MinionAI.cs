using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
[좃뺑이칠 목록]
** 타겟 오브젝트 => 최종 목표가 엔진
    - 처음 => 엔진
    - 타겟 오브젝트가 바뀌었다가 없어지면 다시 엔진으로 설정

- 미니언 플레이어 공격시 미니언 체력 깎이도록 구현

- 감지 범위 및 공격 범위 추가 
    - 유닛, 타겟 오브젝트, 설치형 오브젝트 등 => 아마 레이어로 감지 할 것 같음, 여러 레이어 선택 가능하도록 구현
- 타겟 오브젝트 쪽으로 이동 구현
    - 이동 중 오브젝트 정보가 null이 될 시 이동 재시작
    - 이동 중 타겟 오브젝트가 감지 범위 안에 들어오면 공격 행동 시작
    - 이동 중 타겟 오브젝트 외의 오브젝트에게 공격을 받았을 경우 타겟 오브젝트를 해당 오브젝트로 변경함
        - 감지 범위를 (4x1x4 => 2x1x2)로 조정
        - 공격한 대상의 위치로 0.2초 동안 이동
            - (2x1x2)로 수정한 감지 범위 내에 나를 공격한 타겟 오브젝트가 있다면
                - 해당 오브젝트 위치 방향으로 바라보기 후 공격 행동 시작
        - 공격한 대상이 없다면(못 찾았다면) 이동 행동 시작
- 공격
    - 공격 행동 도중 타겟 오브젝트가 null이되면 엔진으로 목표 설정
    - 공격 범위
        - 미니언의 앞 방향으로 (2x1x1) 사이즈의 정사각형 범위를 attackvisualTime 시간동안 보여줌 (바닥에서? 콜라이더 범위에서?)
        - 공격 범위 내에 있다면 공격력(attackPower)만큼 피해를 입힘 (에너미와 바닥 제외)
        - 공격이 끝나면 경직 (공격 속도 만큼 ?이라는데 일단 ㅇㅋ..) 한 뒤 이동 시작
 */

public class MinionAI : EnemyBase
{
    [Header("탐지 및 공격 콜라이더")]
    [SerializeField] private Collider detectCollider;
    [SerializeField] private Collider attackCollider;

    // 둥지 관련 변수
    [Header("둥지 관련")]
    [SerializeField] private GameObject nestPrefab; // 둥지 프리팹

    public GameObject[] spawnNestList;

    private int maxNestCount = 2;
    private int currentSpawnNest = 0;
    private float spawnNestCoolTime = 15f;
    private float nestSpawnTime = 0f;
    private int spawnNestSlot = 0;

    // Behavior Tree Runner
    private BehaviorTreeRunner BTRunner = null;

    private void Awake()
    {
        base.Awake();
        BTRunner = new BehaviorTreeRunner(SettingBt());

        spawnNestList = new GameObject[maxNestCount];
    }

    private void Update()
    {
        BTRunner.Operate();
    }

    /// <summary>
    /// 기초 값 세팅
    /// </summary>
    protected override void SetAttribute()
    {
        hp = 20;
        attackPower = 1;
        speed = 2.0f;
        detectionRange = 4;
        attackRange = 2;
        attackVisualTime = 1.0f;  // 선제 시간
        restTime = 2.0f;
        targetObject = GameObject.FindGameObjectWithTag("Engine");
    }

    #region 둥지
    /// <summary>
    /// 둥지 배열 빈 인덱스 찾기
    /// </summary>
    /// <returns></returns>
    private int FindEmptyNestSlot()
    {
        for(int i = 0; i < spawnNestList.Length; i++)
        {
            if (spawnNestList[i] == null)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 배열에서 둥지 제거
    /// </summary>
    private void PopNestList()
    {
        for(int i = 0; i < spawnNestList.Length; i++)
        {
            if(spawnNestList[i] == null && currentSpawnNest > 0)
            {
                currentSpawnNest--;
            }
        }
    }

    /// <summary>
    /// 둥지 소환
    /// </summary>
    /// <returns></returns>
    INode.ENodeState SpawnNest()
    {
        if(Time.time - nestSpawnTime < spawnNestCoolTime)
        {
            return INode.ENodeState.Failure;
        }

        if(currentSpawnNest >= maxNestCount)
        {
            return INode.ENodeState.Failure;
        }

        spawnNestSlot = FindEmptyNestSlot();
        if (spawnNestSlot == -1)
        {
            return INode.ENodeState.Failure;
        }

        GameObject spawnNest = Instantiate(nestPrefab, transform.position, transform.rotation);

        currentSpawnNest++;
        spawnNestList[spawnNestSlot] = spawnNest;

        nestSpawnTime = Time.time;

        return INode.ENodeState.Success;
    }
    #endregion


    #region 감지

    private void SetDetectRange()
    {

    }

    private void DetectTarget()
    {
        if ()
        {
            
        }
    }

    #endregion

    /// <summary>
    /// 이동
    /// </summary>
    /// <returns></returns>
    INode.ENodeState Movement()
    {
        // 타겟 오브젝트 정보에 null이 들어갔다면 다시 탐색? -> 기획서에는 그렇게 적혀있으나 엔진을 목표로 둘것임.
        if(targetObject == null)
        {
            return INode.ENodeState.Failure;
        }



        return INode.ENodeState.Running;
    }

    /// <summary>
    /// 공격
    /// </summary>
    /// <returns></returns>
    INode.ENodeState Attack()
    {
        return INode.ENodeState.Failure;
    }

    INode SettingBt()
    {
        return new SelecterNode(new List<INode>
        {
            new SequenceNode(new List<INode>
            {
                new ActionNode(() => SpawnNest())
            }),

            new SequenceNode(new List<INode>
            {
                new ActionNode(() => Movement())
            }),

            new SequenceNode(new List<INode>
            {
                new ActionNode(() => Attack())
            })
        });
    }
}
