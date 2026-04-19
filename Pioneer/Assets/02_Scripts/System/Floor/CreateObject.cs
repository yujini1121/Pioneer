using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI; // UI 레이캐스트용

#warning TODO : CreateObject 정리 필요
// 흐름: 마우스 위치 확인 -> 설치 프리뷰 표시 -> 이동 -> 설치 완료
// 목표: 설치 모드 진입 후 프리뷰 표시, 배치 가능 여부 확인, 이동 후 설치까지 한 번에 처리

public class CreateObject : MonoBehaviour, IBegin
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable, Ballista, Trap, Lantern, Chest }

    [System.Serializable]
    public class CreationList
    {
        public GameObject platform;
        public GameObject wall;
        public GameObject door;
        public GameObject barricade;
        public GameObject craftingTable;
        public GameObject ballista;
        public GameObject trap;
        public GameObject lantern;
        public GameObject chest;
    }

    public static CreateObject instance;

    public bool IsBuilding => onHand != null;

    [Header("기본 설정")]
    [SerializeField] private Transform worldSpaceParent;
    private Transform playerTrans;
    private Camera mainCamera;

    [Header("설치 오브젝트 설정")]
    public CreationType creationType;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask creationLayer;
    [SerializeField] private Color rejectColor = Color.red;
    [SerializeField] private Color permitColor = Color.green;
    [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.55f;
    [SerializeField] private CreationList creationList;
    private GameObject onHand;
    private GameObject tempObj;
    private Renderer creationRender;  // 프리뷰 색상 표시용 렌더러
    private readonly Dictionary<CreationType, GameObject> creationDict = new Dictionary<CreationType, GameObject>();
    private int rotateN = 0;

    [Header("네브메시 설정")]
    [SerializeField] public NavMeshSurface navMeshSurface;
    [SerializeField] private float stopDistance = 1.5f;
    private NavMeshAgent playerAgent;

    [Header("UI 레이캐스트 설정")]
    [SerializeField] private GraphicRaycaster uiRaycaster;  // 없어도 동작하지만 있으면 UI 위 클릭 차단 가능
    [SerializeField] private GameObject uiOutside;

    [Header("이동 잠금 설정")]
    [SerializeField] private bool lockMovementWhileOrienting = true;
    [SerializeField] private bool alsoZeroPlayerSpeed = true;
    private bool isOrienting = false;
    private bool movementLocked = false;
    private float originalPlayerSpeed = -1f;

    [Header("도착 판정 설정")]
    [SerializeField] private float arrivedSpeedEps;   // 이 속도 이하이면 거의 도착으로 간주
    [SerializeField] private float arrivedHoldTime;   // 일정 시간 이상 유지되면 설치 시작
    private float arrivedTimer = 0f;

    [Header("설치 진행 UI")]
    [SerializeField] private float installTimeSec = 2f; // Installable SO에서 덮어쓸 수 있음
    [SerializeField] private Image ringBackground;
    [SerializeField] private Image ringFill;

    [SerializeField] private const float defaultCellSize = 2f;

    private Coroutine installRoutine;
    private bool isCountingDown = false;
    private int rotateAngleIndex = 0;

    private GameObject _evalDummy;

    private SItemStack[] cost;

    // 현재 설치형 오브젝트의 Footprint / Anchor 계산 기준
    private SInstallableObjectDataSO _activeInstallableSO;

    private void HideInstallProgressUi()
    {
        if (ringBackground != null)
        {
            ringBackground.gameObject.SetActive(false);
        }

        if (ringFill != null)
        {
            ringFill.fillAmount = 0f;
        }
    }

    private bool HasValidInstallableSelection()
    {
        if (InventoryManager.Instance == null)
            return false;

        SItemStack selected = InventoryManager.Instance.SelectedSlotInventory;
        if (selected == null || selected.itemBaseType == null)
            return false;

        return selected.itemBaseType.categories == EDataType.BuildObject
            && selected.itemBaseType is SInstallableObjectDataSO;
    }
    private void Awake()
    {
        Debug.Log($">> CreateObject : {gameObject.name}");
        instance = this;

        mainCamera = Camera.main;
        playerTrans = transform;
        playerAgent = GetComponent<NavMeshAgent>();

        // 생성 프리팹 딕셔너리 구성
        creationDict.Add(CreationType.Platform, creationList.platform);
        creationDict.Add(CreationType.Wall, creationList.wall);
        creationDict.Add(CreationType.Door, creationList.door);
        creationDict.Add(CreationType.Barricade, creationList.barricade);
        creationDict.Add(CreationType.CraftingTable, creationList.craftingTable);
        creationDict.Add(CreationType.Ballista, creationList.ballista);
        creationDict.Add(CreationType.Trap, creationList.trap);
        creationDict.Add(CreationType.Lantern, creationList.lantern);
        creationDict.Add(CreationType.Chest, creationList.chest);

        CreateObjectInit();
    }

    private void Start()
    {
        ExitInstallMode(); // 시작 시 설치 모드 비활성화

        if (uiOutside == null)
        {
            Debug.LogWarning("CreateObject: uiOutside is not assigned.");
        }
    }

    private void Update()
    {
        if (uiOutside != null)
        {
            uiOutside.SetActive(onHand == null);
        }

        bool hasValidInstallableSelection = HasValidInstallableSelection();

        if (!hasValidInstallableSelection && !isCountingDown)
        {
            HideInstallProgressUi();

            if (onHand != null || tempObj != null || _activeInstallableSO != null)
            {
                ExitInstallMode();
            }

            return;
        }

        if (!isCountingDown)
        {
            HideInstallProgressUi();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ExitInstallMode();
            return;
        }

        if (onHand == null) return;

        bool hasPendingPlacement = tempObj != null || isCountingDown;
        SetPreviewVisible(!hasPendingPlacement);

        if (hasPendingPlacement)
        {
            Trim();
            UpdateBuildMoveAnimation();
            return;
        }

        CheckCreatable();
        HandleOrientationInput();
        Trim();
        UpdateBuildMoveAnimation();

        if (tempObj != null
            && !isOrienting
            && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
        {
            CancelInstall();
        }
    }

    public void CreateObjectInit()
    {
        rotateN = 0;

        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>(); // 프리뷰 렌더러 캐싱
        var col = onHand.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float cellSize = GetActiveCellSize();

        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int z = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector3(x * cellSize, 0f, z * cellSize);
    }

    // 현재 설치형 오브젝트의 셀 크기
    private float GetActiveCellSize()
    {
        // SO 값 우선
        if (_activeInstallableSO != null && _activeInstallableSO.gridCellSize > 0f)
            return _activeInstallableSO.gridCellSize;

        // SO가 없거나 0이면 기본값 사용
        return defaultCellSize;
    }

    // 현재 Anchor 오프셋 계산
    private Vector2 GetActiveAnchorOffsetCells()
    {
        if (_activeInstallableSO == null) return Vector2.zero;
        return _activeInstallableSO.GetAnchorOffsetCellsByRotateN(rotateN);
    }

    // 현재 Footprint 계산
    private Vector2Int GetActiveFootprint()
    {
        if (_activeInstallableSO == null) return Vector2Int.one;
        return _activeInstallableSO.GetFootprintByRotateN(rotateN);
    }

    // Anchor를 반영한 그리드 스냅
    private Vector3 SnapToGridWithAnchor(Vector3 localPos)
    {
        float cellSize = GetActiveCellSize();
        Vector2 anchorCells = GetActiveAnchorOffsetCells();

        float offsetX = anchorCells.x * cellSize;
        float offsetZ = anchorCells.y * cellSize;

        float x = Mathf.Round((localPos.x - offsetX) / cellSize) * cellSize + offsetX;
        float z = Mathf.Round((localPos.z - offsetZ) / cellSize) * cellSize + offsetZ;

        return new Vector3(x, 0f, z);
    }

    private bool TryGetMouseGroundPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float enter)) return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private void ApplyPreviewTransform(Vector3 localPos)
    {
        onHand.transform.localPosition = localPos;
        onHand.transform.position += Vector3.up * 0.01f;
    }

    private void SetPreviewVisible(bool visible)
    {
        if (onHand != null && onHand.activeSelf != visible)
            onHand.SetActive(visible);
    }

    private void SetPreviewColor(Color c)
    {
        if (creationRender != null && creationRender.material != null)
        {
            c.a = previewAlpha;
            creationRender.material.color = c;
        }
    }

    private void TryPlaceIfPermitted(Vector3 worldPos, Vector3 localPos)
    {
        if (!CheckNear(worldPos))
        {
            SetPreviewColor(rejectColor);
            return;
        }

        SetPreviewColor(permitColor);
        if (Input.GetMouseButtonDown(0))
            MoveToCreate(worldPos, localPos);
    }

    private void CheckCreatable()
    {
        #region UI 위 클릭이면 설치 프리뷰 갱신 중지
        if (IsBlockedByUI())
        {
            SetPreviewVisible(false);
            return;
        }
        else
        {
            SetPreviewVisible(true);
        }
        #endregion

        if (!TryGetMouseGroundPoint(out var mouseWorldPos)) return;

        // Anchor를 반영한 스냅 좌표 계산
        Vector3 localMouse = worldSpaceParent.InverseTransformPoint(mouseWorldPos);
        Vector3 localPos = SnapToGridWithAnchor(localMouse);

        ApplyPreviewTransform(localPos);

        Vector3 worldPos = onHand.transform.position;
        TryPlaceIfPermitted(worldPos, localPos);
    }

    private void HandleOrientationInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) // 위로
        {
            rotateAngleIndex++;
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        }
        else if (scroll < 0f) // 아래로
        {
            rotateAngleIndex--;
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        }
        if (rotateAngleIndex > 3) rotateAngleIndex = 0;
        else if (rotateAngleIndex < 0) rotateAngleIndex = 3;
        rotateN = rotateAngleIndex;
        onHand.transform.localRotation = Quaternion.Euler(0f, 90f * rotateAngleIndex, 0f);

        //int newIdx = -1;//RotateInstallTypeObject
        //if (Input.GetKeyDown(KeyCode.W)) { 
        //    newIdx = 0; 
        //    if (AudioManager.instance != null)
        //        AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        //}
        //else if (Input.GetKeyDown(KeyCode.D)) { 
        //    newIdx = 1; 
        //    if (AudioManager.instance != null)
        //        AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        //}
        //else if (Input.GetKeyDown(KeyCode.S)) { 
        //    newIdx = 2; 
        //    if (AudioManager.instance != null)
        //        AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        //}
        //else if (Input.GetKeyDown(KeyCode.A)) { 
        //    newIdx = 3; 
        //    if (AudioManager.instance != null)
        //        AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        //}
        //if (newIdx >= 0 && onHand != null)
        //{
        //    rotateN = newIdx;
        //    onHand.transform.localRotation = Quaternion.Euler(0f, 90f * rotateN, 0f);
        //}

        if (!lockMovementWhileOrienting || playerAgent == null) return;

        //bool holdingAny = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        //if (holdingAny && !isOrienting)
        //{
        //    isOrienting = true;
        //    LockPlayerMovement();
        //}
        //else if (!holdingAny && isOrienting)
        //{
        //    isOrienting = false;
        //    UnlockPlayerMovement();
        //}
    }

    private bool IsBlockedByUI()
    {
        if (EventSystem.current == null || uiRaycaster == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        return false;
    }

    private IEnumerable<Vector3> EnumerateFootprintCellCenters(Vector3 pivotCenterWorld)
    {
        Vector2Int fp = GetActiveFootprint();
        float cellSize = GetActiveCellSize();

        // 셀 중심 기준 계산
        for (int ix = 0; ix < fp.x; ix++)
        {
            float ox = (ix - (fp.x / 2f) + 0.5f) * cellSize;
            for (int iz = 0; iz < fp.y; iz++)
            {
                float oz = (iz - (fp.y / 2f) + 0.5f) * cellSize;
                yield return pivotCenterWorld + new Vector3(ox, 0f, oz);
            }
        }
    }

    private bool CheckFootprintSupportAndOverlap(Vector3 pivotCenterWorld)
    {
        // 최대 거리 밖이면 설치 불가
        if (Vector3.SqrMagnitude(pivotCenterWorld - SnapToGrid(playerTrans.position)) > maxDistance * maxDistance)
        {
            return false;
        }

        Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
        Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

        foreach (var cellCenter in EnumerateFootprintCellCenters(pivotCenterWorld))
        {
            // 바닥 체크
            if (!Physics.CheckBox(cellCenter, halfSize, orientation, platformLayer))
            {
                return false;
            }

            // 겹침 체크(다른 설치물)
            if (Physics.CheckBox(cellCenter, halfSize, orientation, creationLayer))
            {
                return false;
            }
        }

        return true;
    }

    private bool CheckNear(Vector3 center)
    {
        float[] xArr; // x 위치
        float[] zArr; // z 위치
        float[] xSign; // x 부호
        float[] zSign; // z 부호

        // 최대 거리 밖이면 설치 불가
        if (Vector3.SqrMagnitude(center - SnapToGrid(playerTrans.position)) > maxDistance * maxDistance)
        {
            return false;
        }

        #region 오브젝트별 설치 가능 판정
        switch (creationType)
        {
            case CreationType.Platform:
                // 플랫폼은 기존 갑판 확장 규칙을 그대로 사용
                if (MastManager.Instance != null)
                {
                    int currentDeckCount = MastManager.Instance.currentDeckCount;
                    int maxDeckCount = 30; // 기본 최대 개수

                    // 마스트가 있으면 실제 최대치 사용
                    MastSystem[] masts = FindObjectsOfType<MastSystem>();
                    if (masts.Length > 0)
                    {
                        maxDeckCount = masts[0].GetMaxDeckCount();
                    }

                    // 최대 개수 초과 시 설치 불가
                    if (currentDeckCount >= maxDeckCount)
                    {
                        Debug.Log($"갑판 설치 불가: {currentDeckCount}/{maxDeckCount}개 (최대치)");
                        return false;
                    }
                }

                //1.414213 * 0.5
                xArr = new float[] { 0.707106f, 0.707106f, -0.707106f, -0.707106f };
                zArr = new float[] { 0.707106f, -0.707106f, -0.707106f, 0.707106f };

                // 현재 위치에 이미 플랫폼이 있으면 설치 불가
                if (Physics.CheckBox(center, new Vector3(0.99f, 0.5f, 0.99f), Quaternion.Euler(new Vector3(0f, 45f, 0f)), platformLayer))
                {
                    return false;
                }

                // 주변 4방향 중 기존 플랫폼과 이어지면 설치 가능
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.99f, 0.5f, 0.249f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f * i + 45f, 0f));

                    if (Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                        return true;
                }

                return false;

            case CreationType.Wall:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Barricade:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Door:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.CraftingTable:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Ballista:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Trap:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Lantern:
                return CheckFootprintSupportAndOverlap(center);

            case CreationType.Chest:
                return CheckFootprintSupportAndOverlap(center);
        }
        #endregion

        return false;
    }

    private void MoveToCreate(Vector3 world, Vector3 local)
    {
        if (tempObj != null)
        {
            Destroy(tempObj);
        }

        tempObj = Instantiate(creationDict[creationType], worldSpaceParent);
        tempObj.transform.localPosition = new Vector3(local.x, 0f, local.z);
        tempObj.transform.rotation = onHand.transform.rotation;

        var col = tempObj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var rend = tempObj.GetComponent<Renderer>();
        if (rend != null && rend.material != null) rend.material.color = permitColor;

        Vector3 dir = (world - playerTrans.position).normalized;
        Vector3 stopPos = world - dir * stopDistance;

        // 이동 목적지 설정
        playerAgent.stoppingDistance = stopDistance;

        UnlockPlayerMovement();
        playerAgent.isStopped = false;
        playerAgent.ResetPath();
        playerAgent.SetDestination(stopPos);

        // 새 이동이므로 타이머 초기화
        arrivedTimer = 0f;
    }

    private void Trim()
    {
        if (tempObj == null) return;

        bool almostStopped = playerAgent.velocity.sqrMagnitude <= arrivedSpeedEps * arrivedSpeedEps;
        bool nearEnough = !playerAgent.pathPending &&
                          playerAgent.remainingDistance <= playerAgent.stoppingDistance + 0.05f;

        if (nearEnough || almostStopped)
        {
            // 도착 판정이 나면 설치 카운트다운 시작
            if (!isCountingDown && installRoutine == null)
            {
                installRoutine = StartCoroutine(InstallCountdownRoutine());
            }
        }
        else
        {
            arrivedTimer = 0f;
            // 다시 움직이면 카운트다운 취소
            if (installRoutine != null)
            {
                CancelInstallCountdown();
            }
        }
    }

    private void UpdateBuildMoveAnimation()
    {
        if (PlayerCore.Instance == null || PlayerController.instance == null || playerAgent == null)
            return;

        bool isArriving = !playerAgent.pathPending &&
                          playerAgent.remainingDistance <= playerAgent.stoppingDistance + 0.1f;

        Vector3 moveDir = playerAgent.desiredVelocity;
        moveDir.y = 0f;

        if (tempObj != null && !isArriving && moveDir.sqrMagnitude > 0.04f)
        {
            Vector3 dir = moveDir.normalized;
            PlayerController.instance.lastMoveDirection = dir;
            PlayerCore.Instance.Move(dir);
            return;
        }

        if (tempObj != null || isCountingDown || onHand != null)
        {
            PlayerCore.Instance.Idle(PlayerController.instance.lastMoveDirection);
        }
    }

    // 설치 모드 진입
    private IEnumerator InstallCountdownRoutine()
    {
        Debug.Assert(cost != null);

        isCountingDown = true;
        arrivedTimer = 0f;
        // UI 초기화
        if (ringFill != null)
        {
            ringFill.fillAmount = 0f;
            ringBackground.gameObject.SetActive(true);
        }

        float t = 0f;
        while (t < installTimeSec)
        {
            // 취소 입력: 우클릭 / F
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
            {
                CancelInstallCountdown();
                yield break;
            }

            //// 이동 입력이 들어오면 취소
            //if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            //{
            //    CancelInstallCountdown();
            //    yield break;
            //}

            t += Time.deltaTime;
            if (ringFill != null) ringFill.fillAmount = Mathf.Clamp01(t / Mathf.Max(0.001f, installTimeSec));
            yield return null;
        }

        var col = tempObj.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        var rend = tempObj.GetComponent<Renderer>();
        if (rend != null && rend.material != null) rend.material.color = Color.white;

        navMeshSurface.BuildNavMesh();
        GameManager.Instance?.NotifyPlatformLayoutChanged();

        // 플랫폼 설치 수 갱신
        if (creationType == CreationType.Platform && MastManager.Instance != null)
        {
            MastManager.Instance.UpdateCurrentDeckCount();
            Debug.Log($"현재 갑판 수 갱신: {MastManager.Instance.currentDeckCount}");
        }

        tempObj.GetComponent<InstalledObject>()?.OnPlaced();
        Debug.Log("[설치 완료]");

        // 여기서 재료 차감
        InventoryManager.Instance.Remove(cost);
        InventoryUiMain.instance.IconRefresh();

        playerAgent.ResetPath();
        playerAgent.isStopped = false;

        tempObj = null;
        arrivedTimer = 0f;

        if (ringFill != null) { ringBackground.gameObject.SetActive(false); ringFill.fillAmount = 0f; }

        installRoutine = null;
        isCountingDown = false;

        ExitInstallMode();
    }

    private void CancelInstallCountdown()
    {
        if (installRoutine != null)
        {
            StopCoroutine(installRoutine);
            installRoutine = null;
        }
        isCountingDown = false;
        if (ringFill != null) { ringBackground.gameObject.SetActive(false); ringFill.fillAmount = 0f; }
    }

    void CancelInstall()
    {
        if (installRoutine != null) CancelInstallCountdown();
        playerAgent.isStopped = true;
        playerAgent.ResetPath();
        if (tempObj != null) Destroy(tempObj);
        tempObj = null;
        arrivedTimer = 0f;
    }

    // InGameUI에서 설치형 아이템 선택 시 호출
    public void EnterInstallMode(SInstallableObjectDataSO installableSO, SItemStack[] mCost)
    {

        if (PlayerCore.Instance.currentState == PlayerCore.PlayerState.ActionFishing)
        {
            PlayerFishing.instance.StopFishingLoop();
            PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);
            // PlayerController.instance.currentChargeTime = 0f;
            PlayerController.instance.cencleChargeSlider.value = 0f;
            PlayerController.instance.fishingCencleUI.gameObject.SetActive(false);
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.InstallingObject);

        cost = mCost;

        Debug.Assert(cost.Length > 0);

        // 진행 중인 카운트다운 취소
        if (installRoutine != null) CancelInstallCountdown();

        // 기존 프리뷰 / 임시 오브젝트 제거
        if (onHand != null) { Destroy(onHand); onHand = null; }
        if (tempObj != null) { Destroy(tempObj); tempObj = null; }

        // NavMeshAgent 준비
        if (playerAgent == null) playerAgent = GetComponent<NavMeshAgent>();
        if (playerAgent != null && !playerAgent.enabled) playerAgent.enabled = true;

        // 설치 타입 / 시간 설정 (SO 기준)
        if (installableSO != null)
        {
            _activeInstallableSO = installableSO; // 현재 설치 SO 저장
            creationType = (CreationType)(int)installableSO.installType;
            installTimeSec = Mathf.Max(0.1f, installableSO.buildTime);
        }
        else
        {
            _activeInstallableSO = null;
        }

        // UI / 상태 초기화
        if (ringFill != null)
        {
            ringFill.fillAmount = 0f;
            ringBackground.gameObject.SetActive(false);
        }
        isCountingDown = false;
        arrivedTimer = 0f;
        rotateN = 0;

        CreateObjectInit();

        Debug.Log($"[설치 모드 시작] {creationType}, 시간 {installTimeSec:F2}s");
    }

    public void ExitInstallMode()
    {
        if (installRoutine != null) CancelInstallCountdown();
        HideInstallProgressUi();

        if (onHand != null)
        {
            Destroy(onHand);
            onHand = null;
        }

        if (tempObj != null)
        {
            Destroy(tempObj);
            tempObj = null;
        }

        playerAgent.ResetPath();
        playerAgent.isStopped = true;

        UnlockPlayerMovement();

        _activeInstallableSO = null;

        Debug.Log("[설치 모드 종료]");
    }

    private void LockPlayerMovement()
    {
        if (movementLocked) return;
        movementLocked = true;

        if (playerAgent != null)
        {
            playerAgent.isStopped = true;
            playerAgent.ResetPath();
            playerAgent.velocity = Vector3.zero;
            playerAgent.updateRotation = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;

        if (alsoZeroPlayerSpeed && PlayerCore.Instance != null)
        {
            originalPlayerSpeed = PlayerCore.Instance.speed;
            PlayerCore.Instance.speed = 0f;
        }
    }

    private void UnlockPlayerMovement()
    {
        if (!movementLocked) return;
        movementLocked = false;

        if (playerAgent != null)
        {
            playerAgent.updateRotation = true;
            playerAgent.isStopped = false;
        }

        if (alsoZeroPlayerSpeed && PlayerCore.Instance != null && originalPlayerSpeed >= 0f)
        {
            PlayerCore.Instance.speed = originalPlayerSpeed;
            originalPlayerSpeed = -1f;
        }
    }

    public bool EvaluatePlacement(CreationType type, Vector3 worldPos, Quaternion rot)
    {
        // onHand / rotateN 상태를 임시로 바꿔 평가
        var bakType = creationType;
        var bakOnHand = onHand;
        var bakRotateN = rotateN;
        var bakSO = _activeInstallableSO;

        try
        {
            creationType = type;

            // onHand 대체용 더미 트랜스폼
            if (_evalDummy == null) _evalDummy = new GameObject("~EvalDummy");
            onHand = _evalDummy;
            onHand.transform.rotation = rot;

            // 90도 단위 회전 인덱스 계산
            rotateN = Mathf.RoundToInt(rot.eulerAngles.y / 90f) % 4;
            return CheckNear(worldPos);
        }
        finally
        {
            // 복구
            creationType = bakType;
            onHand = bakOnHand;
            rotateN = bakRotateN;

            _activeInstallableSO = bakSO;
        }
    }
}
