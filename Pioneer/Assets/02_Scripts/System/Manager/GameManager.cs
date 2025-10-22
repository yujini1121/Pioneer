using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#region 임시 Stats
public class EnemyStats : MonoBehaviour
{
    public float baseHP = 100f;
    public float baseATK = 10f;
    public float hp, atk;

    void Awake() { hp = baseHP; atk = baseATK; }

    public void ApplyScaling(float atkMul, float hpMul)
    {
        atk = baseATK * atkMul;
        hp = baseHP * hpMul;
    }
}
#endregion

public class GameManager : MonoBehaviour, IBegin
{
	#region 아 너무 길어요 
	public static GameManager Instance;

    [Header("시간 설정")]
    public float currentGameTime = 0f;

    [Header("낮밤 체크 및 일차수 확인")]
    public bool IsDaytime = true;
    public int currentDay = 1; // 1일차 시작

    [Header("낮밤 순환 설정")]
    public Volume postProcessVolume;
    public Gradient dayToNightGradient;
    public Gradient nightToDayGradient;
    public AnimationCurve exposureCurve;
    public float dayDuration = 270f;
    public float nightDuration = 90f;
    private float oneDayDuration;

    private ColorAdjustments colorAdjustments;
    private float cycleTime = 0f;

    [Header("스포너 지점")]
    public GameObject[] spawnPoints;

    [Header("승무원 스프라이트 지정")]
    public Sprite[] marinerSprites;

    [Header("에너미 프리팹")]
    public GameObject minion;   
    public GameObject crawler;  
    public GameObject titan;   

    [Header("게임오버 관리")]
    public int totalCrewMembers = 0;
    public int deadCrewMembers = 0;
    public GameOverUI gameOverUI;
    public Canvas[] allUICanvas;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // 생성된 적들을 한 곳에 모아 보기 위한 부모 Transform
    private Transform enemyRoot;

	[System.Serializable]
    public struct DayEnemyRow
    {
        [Tooltip("총 출현 수 = 미니언 + 크룰러 + 타이탄")]
        public int total;   // 총 출현수
        public int minion;  // 미니언 수
        public int crawler; // 크롤러 수
        public int titan;   // 타이탄 수
    }

    [System.Serializable]
    public struct EnemyScaleRow
    {
        [Range(0, 200)] public float attackPercent; // 공격력 증가 %
        [Range(0, 200)] public float hpPercent;     // 체력 증가 %
    }

    [Header("일차별 에너미 출현 표 (1~5일차)")]
    public DayEnemyRow[] enemySpawnTable = new DayEnemyRow[5];

    [Header("일차별 능력치 강화 표 (1~5일차)")]
    public EnemyScaleRow[] enemyScaleTable = new EnemyScaleRow[5];
	#endregion

	[Header("뗏목 끝지점 탐색")]
	[SerializeField] Transform mast;
	[SerializeField] int points = 12;





	void FindEdgePoint()
    {
        List<Vector3> candidatePoints = new List<Vector3>();
        
        for (int i = 0; i < points; i++)
		{

		}
	}


	#region 집 보내주세요
	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);

		postProcessVolume.profile.TryGet(out colorAdjustments);
	}

	private void Start()
	{
		oneDayDuration = dayDuration + nightDuration;

		Debug.Log($">> GameManager.Start()");

		if (InventoryUiMain.instance != null)
			InventoryUiMain.instance.Start();
		else
			Debug.Log($">> GameManager.Start() : InventoryUiMain 인스턴스가 없음");
	}

	private void Update()
	{
		if (Time.timeScale > 0)
		{
			currentGameTime += Time.deltaTime;
			cycleTime += Time.deltaTime;
		}

		UpdateDayNightCycle();
	}

	private void UpdateDayNightCycle()
	{
		// 현재 페이즈(낮/밤)에 맞는 설정을 한 번에 가져옴
		bool isDay = IsDaytime;
		float duration = isDay ? dayDuration : nightDuration;
		Gradient grad = isDay ? dayToNightGradient : nightToDayGradient;

		// 진행도 0~1f
		float t = Mathf.Clamp01(cycleTime / duration);

		// 컬러/노출 보정
		colorAdjustments.colorFilter.value = grad.Evaluate(t);
		colorAdjustments.postExposure.value = exposureCurve.Evaluate(t);

		// 아직 페이즈가 끝나지 않았으면 리턴
		if (cycleTime < duration) return;

		// 페이즈 종료 처리
		cycleTime = 0f;

		if (isDay)
		{
			// 낮 -> 밤 전환
			Debug.Log($"밤이 되었습니다. (Day {currentDay})");
			IsDaytime = false;
			OnNightStart();
		}
		else
		{
			// 밤 -> 낮 전환
			IsDaytime = true;
			OnNightEnd();
			currentDay++;
			Debug.Log($"아침이 되었습니다. (Day {currentDay})");
		}
	}


	private void OnNightStart()
	{
		var s = GetScaleRowForDay(currentDay);
		Debug.Log($"[ScaleTable] Day {currentDay} -> ATK +{s.attackPercent}%, HP +{s.hpPercent}%");
		SpawnEnemiesForCurrentDay();
	}

	private void OnNightEnd()
	{
		DespawnAllEnemies();
		ApplyCrewEmbarkRule();
	}

	public void GetGameTimeInfo(out int days, out int hours)
	{
		days = Mathf.FloorToInt(currentGameTime / oneDayDuration);
		float remainingTime = currentGameTime % oneDayDuration;
		hours = Mathf.FloorToInt((remainingTime / oneDayDuration) * 24f);
	}

	public void AddCrewMember() { totalCrewMembers++; }
	public void MarinerDiedCount() { deadCrewMembers++; }

	public void TriggerGameOver()
	{
		Time.timeScale = 0f;

		if (ThisIsPlayer.Player != null)
		{
			Renderer playerRenderer = ThisIsPlayer.Player.GetComponent<Renderer>();
			if (playerRenderer != null)
			{
				Color color = playerRenderer.material.color;
				color.a = 0f;
				playerRenderer.material.color = color;
			}
		}

		HideAllUI();

		if (gameOverUI != null)
			gameOverUI.ShowGameOverScreen(totalCrewMembers, deadCrewMembers);
	}

	void HideAllUI()
	{
		foreach (Canvas canvas in allUICanvas)
		{
			if (canvas != null && canvas != gameOverUI.GetComponent<Canvas>())
				canvas.gameObject.SetActive(false);
		}
	}

	private void SpawnEnemiesForCurrentDay()
	{
		Debug.Log("Spawn Enemies");

		if (spawnPoints == null || spawnPoints.Length == 0) return;

		DayEnemyRow row = GetSpawnRowForDay(currentDay);
		EnemyScaleRow scale = GetScaleRowForDay(currentDay);

		SpawnOf(minion, row.minion, scale);     // 미니언
		SpawnOf(crawler, row.crawler, scale);   // 크롤러
		SpawnOf(titan, row.titan, scale);       // 타이탄

		int spawnedCount = row.minion + row.crawler + row.titan;
		Debug.Log($"[Spawn] Day {currentDay}: Minion {row.minion}, Crawler {row.crawler}, Titan {row.titan} (총 {spawnedCount})");
	}

	// 일차별 공격력 적용된 에너미 생성
	private void SpawnOf(GameObject prefab, int count, EnemyScaleRow scale)
	{
		DayEnemyRow row = GetSpawnRowForDay(currentDay);
		Debug.Log($"[Table] Day{currentDay} -> M:{row.minion}, C:{row.crawler}, T:{row.titan}");

		if (prefab == null || count <= 0) return;

		// 부모 컨테이너 
		EnsureEnemyRoot();

		for (int i = 0; i < count; i++)
		{
			int sp = Random.Range(0, spawnPoints.Length);
			Transform p = spawnPoints[sp].transform;
			Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

			GameObject e = Instantiate(prefab, p.position + offset, Quaternion.identity);

			// 계층 정리 및 식별용 이름
			if (enemyRoot != null) e.transform.SetParent(enemyRoot);
			e.name = $"{prefab.name}_Day{currentDay}_#{i + 1}";

			spawnedEnemies.Add(e);

			if (e.TryGetComponent(out EnemyStats stats))
			{
				float atkMul = 1f + (scale.attackPercent * 0.01f);
				float hpMul = 1f + (scale.hpPercent * 0.01f);
				stats.ApplyScaling(atkMul, hpMul);

				Debug.Log($"[Scale] Day {currentDay} {e.name} " +
						  $"ATK {stats.baseATK}→{stats.atk} (x{atkMul:0.00}), " +
						  $"HP {stats.baseHP}→{stats.hp} (x{hpMul:0.00})");
			}
		}
	}

	private void DespawnAllEnemies()
	{
		foreach (GameObject e in spawnedEnemies)
			if (e != null) Destroy(e);

		spawnedEnemies.Clear();
		Debug.Log("[Despawn] 밤 종료로 모든 에너미 제거");
	}

	private DayEnemyRow GetSpawnRowForDay(int day)
	{
		if (enemySpawnTable != null && enemySpawnTable.Length > 0)
		{
			int idx = Mathf.Clamp(day - 1, 0, enemySpawnTable.Length - 1);
			return enemySpawnTable[idx];
		}
		return new DayEnemyRow { total = 0, minion = Random.Range(2, 8), crawler = 0, titan = 0 };
	}

	private EnemyScaleRow GetScaleRowForDay(int day)
	{
		if (enemyScaleTable != null && enemyScaleTable.Length > 0)
		{
			int idx = Mathf.Clamp(day - 1, 0, enemyScaleTable.Length - 1);
			return enemyScaleTable[idx];
		}
		return new EnemyScaleRow { attackPercent = 0, hpPercent = 0 };
	}

	private void ApplyCrewEmbarkRule()
	{
		int add = CalcCrewEmbarkCount(currentDay, totalCrewMembers);
		for (int i = 0; i < add; i++) AddCrewMember();

		if (add > 0)
			Debug.Log($"[Crew] Day {currentDay} 아침: 승선 {add}명 → 총 {totalCrewMembers}명");
	}

	// 1일차 0명, 2일차 1명, 3일차 2명, 4일차 3명,
	// 5일차: 현재 승무원 수 ≤3 → 4명, 현재 승무원 수 ≥4 → 5명
	private int CalcCrewEmbarkCount(int day, int crewNow)
	{
		switch (Mathf.Clamp(day, 1, 5))
		{
			case 1: return 0;
			case 2: return 1;
			case 3: return 2;
			case 4: return 3;
			case 5: return (crewNow <= 3) ? 4 : 5;
			default:
				// 6일차 이상은 마지막 값을 유지하거나, 필요 시 규칙 확장
				return (crewNow <= 3) ? 4 : 5;
		}
	}

	public float TimeUntilNight()
	{
		if (IsDaytime) return Mathf.Max(0f, dayDuration - cycleTime);
		else return 0f;
	}

	public void CollectResource(string type)
	{
		Debug.Log($"자원 획득: {type}");
	}
	#endregion
	#region 하이어라키창에서 보기 쉽게 정리 (부모 보장)
	private void EnsureEnemyRoot()
    {
        if (enemyRoot == null)
        {
            var go = GameObject.Find("__ENEMIES__");
            enemyRoot = (go != null) ? go.transform : new GameObject("__ENEMIES__").transform;
        }
    }
	#endregion
}
