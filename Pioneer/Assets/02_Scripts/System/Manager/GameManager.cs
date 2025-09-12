using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour, IBegin
{
    public static GameManager Instance;

    [Header("시간 설정")]
    public float currentGameTime = 0f;
    public bool IsDaytime = true;

    [Header("낮밤 순환 설정")]
    public Volume postProcessVolume;
    public Gradient dayToNightGradient;
    public Gradient nightToDayGradient;
    public AnimationCurve exposureCurve;
    public float dayDuration = 270f;
    public float nightDuration = 90f;
    public float oneDayDuration = 360f;

    private ColorAdjustments colorAdjustments;
    private float cycleTime = 0f;
    private float fullCycleDuration;
    private bool transitioningToNight = true;

    [Header("스포너 지점")]
    public GameObject[] spawnPoints;

    [Header("승무원 스프라이트 지정")]
    public Sprite[] marinerSprites;

    [Header("미니언 에너미")]
    public GameObject minion;

    [Header("게임오버 관리")]
    public int totalCrewMembers = 0;
    public int deadCrewMembers = 0;
    public GameOverUI gameOverUI;
    public Canvas[] allUICanvas;

    private List<GameObject> spawnedMinions = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        postProcessVolume.profile.TryGet(out colorAdjustments);    
    }

    private void Start()
    {
        Debug.Log($">> GameManager.Start()");

        if (InventoryUiMain.instance != null)
        {
            InventoryUiMain.instance.Start();
        }
        else
        {
            Debug.Log($">> GameManager.Start() : InventoryUiMain 인스턴스가 없음");
        }
    }

    private void Update()
    {
        if (Time.timeScale > 0)
        {
            currentGameTime += Time.deltaTime;
        }

        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        cycleTime += Time.deltaTime;
        float t;

        if (IsDaytime)
        {
            t = Mathf.Clamp01(cycleTime / dayDuration);
            colorAdjustments.colorFilter.value = Color.Lerp(dayToNightGradient.Evaluate(0f), dayToNightGradient.Evaluate(1f), t);
        }
        else
        {
            t = Mathf.Clamp01(cycleTime / nightDuration);
            colorAdjustments.colorFilter.value = Color.Lerp(nightToDayGradient.Evaluate(0f), nightToDayGradient.Evaluate(1f), t);
        }

        colorAdjustments.postExposure.value = exposureCurve.Evaluate(t);

        if (IsDaytime && cycleTime >= dayDuration)
        {
            IsDaytime = false;
            cycleTime = 0f;
            Debug.Log("밤이 되었습니다.");
            SpawnMinions(); // 밤에 미니언 생성
        }
        else if (!IsDaytime && cycleTime >= nightDuration)
        {
            IsDaytime = true;
            cycleTime = 0f;
            Debug.Log("아침이 되었습니다.");
            DespawnMinions(); // 아침에 미니언 제거
        }
    }

    public void GetGameTimeInfo(out int days, out int hours)
    {
        // 전체 경과 시간을 일 단위로 계산
        days = Mathf.FloorToInt(currentGameTime / oneDayDuration);

        // 남은 시간을 시간으로 계산 (하루 = 24시간 기준)
        float remainingTime = currentGameTime % oneDayDuration;
        hours = Mathf.FloorToInt((remainingTime / oneDayDuration) * 24f);
    }
    public void AddCrewMember() // 나중에 승무원 영입 시스템 쪽에서 추가해야할듯
    {
        totalCrewMembers++;
    }

    public void MarinerDiedCount() // 승무원 사망 카운트
    {
        deadCrewMembers++;
    }

    // 게임오버 실행
    public void TriggerGameOver()
    {
        Time.timeScale = 0f;

        // 플레이어 투명화
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
        {
            gameOverUI.ShowGameOverScreen(totalCrewMembers, deadCrewMembers);
        }
    }

    void HideAllUI()
    {
        foreach (Canvas canvas in allUICanvas)
        {
            if (canvas != null && canvas != gameOverUI.GetComponent<Canvas>())
            {
                canvas.gameObject.SetActive(false);
            }
        }
    }
    private void SpawnMinions()
    {
        if (spawnPoints.Length == 0 || minion == null) return;

        int spawnIndex = Random.Range(0, spawnPoints.Length);   // 1~12까지 스폰포인트 탐색
        Transform spawnTransform = spawnPoints[spawnIndex].transform;

        int count = Random.Range(2, 8); // 2~7마리
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            GameObject m = Instantiate(minion, spawnTransform.position + offset, Quaternion.identity);
            spawnedMinions.Add(m);
        }

        Debug.Log($"{count}마리 미니언이 스폰되었습니다.");
    }

    private void DespawnMinions()
    {
        foreach (GameObject m in spawnedMinions)
        {
            if (m != null) Destroy(m);
        }

        spawnedMinions.Clear();
        Debug.Log("아침이 되어 미니언이 제거되었습니다.");
    }

    public float TimeUntilNight()
    {
        if (IsDaytime)
            return Mathf.Max(0f, dayDuration - cycleTime);
        else
            return 0f;
    }

    public void CollectResource(string type)
    {
        Debug.Log($"자원 획득: {type}");
    }


}
