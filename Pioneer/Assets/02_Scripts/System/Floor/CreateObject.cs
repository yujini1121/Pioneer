using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#warning TODO : CreateObject 수정이 필요
// 현재 : 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 배치
// 필요 : 레시피에서 제작 여부 가능 -> 제작 버튼 눌림 -> 제작 UI 끄기 -> 건설 UI 전환 -> 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 시간 소모 및 방해받지 않는지 항상 체크 -> 아이템 소모 -> 배치

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
    [SerializeField] private CreationList creationList;
    private GameObject onHand;
    private GameObject tempObj;
    private Renderer creationRender;
    private readonly Dictionary<CreationType, GameObject> creationDict = new Dictionary<CreationType, GameObject>();
    private int rotateN = 0;

    [Header("네브메시 설정")]
    [SerializeField] public NavMeshSurface navMeshSurface;
    [SerializeField] private float stopDistance = 1.5f;
    private NavMeshAgent playerAgent;

    [Header("UI 레이캐스트 설정")]
    [SerializeField] private GraphicRaycaster uiRaycaster;

    [Header("이동 잠금 설정")]
    [SerializeField] private bool lockMovementWhileOrienting = true;
    [SerializeField] private bool alsoZeroPlayerSpeed = true;
    private bool isOrienting = false;
    private bool movementLocked = false;
    private float originalPlayerSpeed = -1f;

    [Header("버그해결하고싶어요")]
    [SerializeField] private float arrivedSpeedEps;
    [SerializeField] private float arrivedHoldTime;
    private float arrivedTimer = 0f;

    [Header("제작 대기")]
    [SerializeField] private float installTimeSec = 2f;
    [SerializeField] private Image ringBackground;
    [SerializeField] private Image ringFill;

    // ✅ 전역 스냅 단위(하나만 사용)
    [Header("Grid / Snap")]
    [SerializeField] private float globalCellSize = 2f;

    private Coroutine installRoutine;
    private bool isCountingDown = false;
    private int rotateAngleIndex = 0;

    private GameObject _evalDummy;

    private SItemStack[] cost;

    private void Awake()
    {
        Debug.Log($">> CreateObject : {gameObject.name}");
        instance = this;

        mainCamera = Camera.main;
        playerTrans = transform;
        playerAgent = GetComponent<NavMeshAgent>();

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
        ExitInstallMode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ExitInstallMode();
            return;
        }

        if (onHand == null) return;

        CheckCreatable();
        HandleOrientationInput();
        Trim();

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
        rotateAngleIndex = 0;

        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>();
        var col = onHand.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // ✅ 전역 스냅만 사용 (타입별 cellSizeByType 제거)
    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float cellSize = Mathf.Max(0.0001f, globalCellSize);

        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int z = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector3(x * cellSize, 0f, z * cellSize);
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
            creationRender.material.color = c;
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
        if (IsBlockedByUI())
        {
            SetPreviewVisible(false);
            return;
        }
        else
        {
            SetPreviewVisible(true);
        }

        if (!TryGetMouseGroundPoint(out var mouseWorldPos)) return;

        Vector3 localPos = SnapToGrid(worldSpaceParent.InverseTransformPoint(mouseWorldPos));
        ApplyPreviewTransform(localPos);

        Vector3 worldPos = onHand.transform.position;
        TryPlaceIfPermitted(worldPos, localPos);
    }

    private void HandleOrientationInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            rotateAngleIndex++;
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        }
        else if (scroll < 0f)
        {
            rotateAngleIndex--;
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.RotateInstallTypeObject);
        }

        if (rotateAngleIndex > 3) rotateAngleIndex = 0;
        else if (rotateAngleIndex < 0) rotateAngleIndex = 3;

        // ✅ 실제 회전
        onHand.transform.localRotation = Quaternion.Euler(0f, 90f * rotateAngleIndex, 0f);

        // ✅ 설치 판정용 회전 인덱스 동기화 (rotateN이 안 바뀌던 문제 해결)
        rotateN = rotateAngleIndex;

        if (!lockMovementWhileOrienting || playerAgent == null) return;
    }

    // ✅ 원래 코드가 무조건 false 반환이라 UI 위에서도 프리뷰가 나옴
    private bool IsBlockedByUI()
    {
        if (EventSystem.current == null) return false;

        // GraphicRaycaster가 없으면: EventSystem의 기본 PointerOverGameObject라도 체크
        if (uiRaycaster == null)
            return EventSystem.current.IsPointerOverGameObject();

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        return results != null && results.Count > 0;
    }

    private bool CheckNear(Vector3 center)
    {
        float[] xArr;
        float[] zArr;
        float[] xSign;
        float[] zSign;

        //maxDistance보다 멀면 설치 불가능
        if (Vector3.SqrMagnitude(center - SnapToGrid(playerTrans.position)) > maxDistance * maxDistance)
        {
            return false;
        }

        switch (creationType)
        {
            case CreationType.Platform:
                if (MastManager.Instance != null)
                {
                    int currentDeckCount = MastManager.Instance.currentDeckCount;
                    int maxDeckCount = 30;

                    MastSystem[] masts = FindObjectsOfType<MastSystem>();
                    if (masts.Length > 0)
                    {
                        maxDeckCount = masts[0].GetMaxDeckCount();
                    }

                    if (currentDeckCount >= maxDeckCount)
                    {
                        Debug.Log($"갑판 설치 불가: {currentDeckCount}/{maxDeckCount}개 (최대 도달)");
                        return false;
                    }
                }

                xArr = new float[] { 0.707106f, 0.707106f, -0.707106f, -0.707106f };
                zArr = new float[] { 0.707106f, -0.707106f, -0.707106f, 0.707106f };

                if (Physics.CheckBox(center, new Vector3(0.99f, 0.5f, 0.99f), Quaternion.Euler(new Vector3(0f, 45f, 0f)), platformLayer))
                {
                    return false;
                }

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
                xArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };
                zArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.Barricade:
                xArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };
                zArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.Door:
                xArr = new float[] { 0.353553f, 1.060659f };
                zArr = new float[] { 0.353553f, 1.060659f };
                xSign = new float[] { -1f, -1f, 1f, 1f };
                zSign = new float[] { -1f, 1f, 1f, -1f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    Vector3 offset = new Vector3(xArr[i] * xSign[rotateN % 4], 0f, zArr[i] * zSign[rotateN % 4]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.CraftingTable:
                xArr = new float[] { -0.353553f, 0.353553f };
                zArr = new float[] { -0.353553f, 0.353553f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.Ballista:
                xArr = new float[] { 0f, 0.707106f, 1.414213f, 0.707106f, 0f, -0.707106f, -1.414213f, -0.707106f, 0f };
                zArr = new float[] { 0f, 0.707106f, 0f, -0.707106f, -1.414213f, -0.707106f, 0f, 0.707106f, 1.414213f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.Trap:
                xArr = new float[] { 0.353553f, 0.353553f, -0.353553f, -0.353553f };
                zArr = new float[] { 0.353553f, -0.353553f, -0.353553f, 0.353553f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;

            case CreationType.Lantern:
                xArr = new float[] { 0f };
                zArr = new float[] { 0f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;
        }

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

        playerAgent.stoppingDistance = stopDistance;

        UnlockPlayerMovement();
        playerAgent.isStopped = false;
        playerAgent.ResetPath();
        playerAgent.SetDestination(stopPos);

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
            if (!isCountingDown && installRoutine == null)
            {
                installRoutine = StartCoroutine(InstallCountdownRoutine());
            }
        }
        else
        {
            arrivedTimer = 0f;
            if (installRoutine != null)
            {
                CancelInstallCountdown();
            }
        }
    }

    private IEnumerator InstallCountdownRoutine()
    {
        Debug.Assert(cost != null);

        isCountingDown = true;
        arrivedTimer = 0f;

        if (ringFill != null)
        {
            ringFill.fillAmount = 0f;
            ringBackground.gameObject.SetActive(true);
        }

        float t = 0f;
        while (t < installTimeSec)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
            {
                CancelInstallCountdown();
                yield break;
            }

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

        if (creationType == CreationType.Platform && MastManager.Instance != null)
        {
            MastManager.Instance.UpdateCurrentDeckCount();
            Debug.Log($"현재 갑판 갯수: {MastManager.Instance.currentDeckCount}");
        }

        tempObj.GetComponent<InstalledObject>()?.OnPlaced();
        Debug.Log("[설치 완료됨]");

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

    public void EnterInstallMode(SInstallableObjectDataSO installableSO, SItemStack[] mCost)
    {
        if (PlayerCore.Instance.currentState == PlayerCore.PlayerState.ActionFishing)
        {
            PlayerFishing.instance.StopFishingLoop();
            PlayerCore.Instance.SetState(PlayerCore.PlayerState.Default);
            PlayerController.instance.cencleChargeSlider.value = 0f;
            PlayerController.instance.fishingCencleUI.gameObject.SetActive(false);
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.InstallingObject);

        cost = mCost;
        Debug.Assert(cost.Length > 0);

        if (installRoutine != null) CancelInstallCountdown();

        if (onHand != null) { Destroy(onHand); onHand = null; }
        if (tempObj != null) { Destroy(tempObj); tempObj = null; }

        if (playerAgent == null) playerAgent = GetComponent<NavMeshAgent>();
        if (playerAgent != null && !playerAgent.enabled) playerAgent.enabled = true;

        if (installableSO != null)
        {
            creationType = (CreationType)(int)installableSO.installType;
            installTimeSec = Mathf.Max(0.1f, installableSO.buildTime);
        }

        if (ringFill != null)
        {
            ringFill.fillAmount = 0f;
            ringBackground.gameObject.SetActive(false);
        }
        isCountingDown = false;
        arrivedTimer = 0f;

        rotateN = 0;
        rotateAngleIndex = 0;

        CreateObjectInit();

        Debug.Log($"[설치모드 진입] {creationType}, 제작 {installTimeSec:F2}s");
    }

    public void ExitInstallMode()
    {
        if (installRoutine != null) CancelInstallCountdown();
        if (ringBackground != null) ringBackground.gameObject.SetActive(false);

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

        Debug.Log("[설치 모드 종료됨]");
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
        var bakType = creationType;
        var bakOnHand = onHand;
        var bakRotateN = rotateN;

        try
        {
            creationType = type;

            if (_evalDummy == null) _evalDummy = new GameObject("~EvalDummy");
            onHand = _evalDummy;
            onHand.transform.rotation = rot;

            rotateN = Mathf.RoundToInt(rot.eulerAngles.y / 90f) % 4;

            return CheckNear(worldPos);
        }
        finally
        {
            creationType = bakType;
            onHand = bakOnHand;
            rotateN = bakRotateN;
        }
    }
}
