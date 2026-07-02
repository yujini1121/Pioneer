using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceSettings", menuName = "ScriptableObjects/Game Balance Settings", order = 0)]
public class GameBalanceSettings : ScriptableObject
{
    public const string DefaultResourceName = "GameBalanceSettings";

    [System.Serializable]
    public struct EnemySpawnRow
    {
        public int total;
        public int minion;
        public int crawler;
        public int titan;
    }

    [System.Serializable]
    public struct EnemyScaleRow
    {
        [Range(0, 200)] public float attackPercent;
        [Range(0, 200)] public float hpPercent;
    }

    [Header("Day / Night")]
    [Min(0.01f)] public float dayDuration = 120f;
    [Min(0.01f)] public float nightDuration = 60f;

    [Header("Enemy Spawn Table")]
    public EnemySpawnRow[] enemySpawnTable =
    {
        new EnemySpawnRow { total = 3, minion = 3, crawler = 0, titan = 0 },
        new EnemySpawnRow { total = 6, minion = 4, crawler = 2, titan = 0 },
        new EnemySpawnRow { total = 10, minion = 5, crawler = 3, titan = 2 },
        new EnemySpawnRow { total = 13, minion = 6, crawler = 4, titan = 3 },
        new EnemySpawnRow { total = 17, minion = 8, crawler = 5, titan = 4 },
    };

    [Header("Enemy Scale Table")]
    public EnemyScaleRow[] enemyScaleTable =
    {
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 20f, hpPercent = 40f },
        new EnemyScaleRow { attackPercent = 50f, hpPercent = 60f },
    };

    private void OnValidate()
    {
        dayDuration = Mathf.Max(0.01f, dayDuration);
        nightDuration = Mathf.Max(0.01f, nightDuration);
        NormalizeTotals();
    }

    public void NormalizeTotals()
    {
        if (enemySpawnTable == null)
            return;

        for (int i = 0; i < enemySpawnTable.Length; i++)
        {
            EnemySpawnRow row = enemySpawnTable[i];
            row.total = row.minion + row.crawler + row.titan;
            enemySpawnTable[i] = row;
        }
    }
}
