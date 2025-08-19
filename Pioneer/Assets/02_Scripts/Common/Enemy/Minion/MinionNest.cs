using System.Collections;
using UnityEngine;

/* ================================
2508118
- 둥지는 미니언 2마리를 생성하기만 함
- 둥지는 10의 체력을 가짐 미니언 생성 쿨타임은 5초
- 미니언 2마리를 생성하고 나면 파괴

- 새로 생성된 미니언들이 둥지 프리팹을 안 가지고 태어남.
- 새로 생성됨 미니언들이 잘 작동하지않는거 같음..?
================================ */


public class MinionNest : EnemyBase
{
    [Header("생성 미니언 설정")]
    [SerializeField] private GameObject minionPrefab;
    private int maxMinionCount = 2;

    [Header("생성 시간 설정")]
    [SerializeField] float initDelay = 5.0f;

    void OnEnable()
    {
        StartCoroutine(SpawnMinionRoutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnMinionRoutine()
    {
        yield return new WaitForSeconds(initDelay);

        for(int i = 0; i < maxMinionCount; i++)
        {
            Instantiate(minionPrefab, transform.position, Quaternion.identity);

            if(i < maxMinionCount - 1)
            {
                yield return new WaitForSeconds(initDelay);
            }
        }

        Destroy(gameObject);
    }
}
