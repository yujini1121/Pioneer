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
    [Min(0.01f)] public float dayDuration = 150f;
    [Min(0.01f)] public float nightDuration = 50f;

    [Header("Enemy Spawn Table")]
    public EnemySpawnRow[] enemySpawnTable =
    {
        new EnemySpawnRow { total = 3, minion = 3, crawler = 0, titan = 0 },
        new EnemySpawnRow { total = 5, minion = 4, crawler = 1, titan = 0 },
        new EnemySpawnRow { total = 8, minion = 5, crawler = 2, titan = 1 },
        new EnemySpawnRow { total = 9, minion = 6, crawler = 2, titan = 1 },
        new EnemySpawnRow { total = 12, minion = 7, crawler = 3, titan = 2 },
    };

    [Header("Enemy Scale Table")]
    public EnemyScaleRow[] enemyScaleTable =
    {
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 0f },
        new EnemyScaleRow { attackPercent = 0f, hpPercent = 10f },
        new EnemyScaleRow { attackPercent = 10f, hpPercent = 15f },
        new EnemyScaleRow { attackPercent = 20f, hpPercent = 25f },
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
