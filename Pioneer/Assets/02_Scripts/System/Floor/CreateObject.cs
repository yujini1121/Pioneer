using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI; // UI 레이캐스트용

#warning TODO : CreateObject 수정이 필요
// 현재 : 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 배치
// 필요 : 레시피에서 제작 여부 가능 -> 제작 버튼 눌림 -> 제작 UI 끄기 -> 건설 UI 전환 -> 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 시간 소모 및 방해받지 않는지 항상 체크 -> 아이템 소모 -> 배치

public class CreateObject : MonoBehaviour, IBegin
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable, Ballista, Trap, Lantern }

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
    }

    public static CreateObject instance;

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
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private float stopDistance = 1.5f;
    private NavMeshAgent playerAgent;

    [Header("UI 레이캐스트 설정")]
    [SerializeField] private GraphicRaycaster uiRaycaster;  // null이어야 정상작동; 일부러 할당 안 해둠 ㅜㅜ..

    [Header("이동 잠금 설정")]
    [SerializeField] private bool lockMovementWhileOrienting = true;
    [SerializeField] private bool alsoZeroPlayerSpeed = true;
    private bool isOrienting = false;
    private bool movementLocked = false;
    private float originalPlayerSpeed = -1f;

    [Header("버그해결하고싶어요")]
    [SerializeField] private float arrivedSpeedEps;   // 이 속도보다 느리면 "멈춤"으로 간주
    [SerializeField] private float arrivedHoldTime;    // 멈춤이 이 시간 이상 지속되면 설치
    private float arrivedTimer = 0f;
    
    private GameObject _evalDummy;


    private void Awake()
    {
        Debug.Log($">> CreateObject : {gameObject.name}");
        instance = this;

        mainCamera = Camera.main;
        playerTrans = transform;
        playerAgent = GetComponent<NavMeshAgent>();

        // 프리팹 딕셔너리 빌드 (이렇게 안 하면 안됨....)
        creationDict.Add(CreationType.Platform, creationList.platform);
        creationDict.Add(CreationType.Wall, creationList.wall);
        creationDict.Add(CreationType.Door, creationList.door);
        creationDict.Add(CreationType.Barricade, creationList.barricade);
        creationDict.Add(CreationType.CraftingTable, creationList.craftingTable);
        creationDict.Add(CreationType.Ballista, creationList.ballista);
        creationDict.Add(CreationType.Trap, creationList.trap);
        creationDict.Add(CreationType.Lantern, creationList.lantern);

        CreateObjectInit();
    }

    private void Start()
    {
        ExitInstallMode(); // 게임 시작 시 설치 모드 OFF
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

        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>();
        var col = onHand.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private static Vector3 SnapToGrid(Vector3 worldPos)
    {
        const float cellSize = 2f;
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
        #region UI 위에선 설치가능 여부 프리뷰부터 보이지 않게 처리함 
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

        Vector3 localPos = SnapToGrid(worldSpaceParent.InverseTransformPoint(mouseWorldPos));
        ApplyPreviewTransform(localPos);

        Vector3 worldPos = onHand.transform.position;
        TryPlaceIfPermitted(worldPos, localPos);
    }

    private void HandleOrientationInput()
    {
        int newIdx = -1;
        if (Input.GetKeyDown(KeyCode.W)) newIdx = 0;
        else if (Input.GetKeyDown(KeyCode.D)) newIdx = 1;
        else if (Input.GetKeyDown(KeyCode.S)) newIdx = 2;
        else if (Input.GetKeyDown(KeyCode.A)) newIdx = 3;

        if (newIdx >= 0 && onHand != null)
        {
            rotateN = newIdx;
            onHand.transform.localRotation = Quaternion.Euler(0f, 90f * rotateN, 0f);
        }

        if (!lockMovementWhileOrienting || playerAgent == null) return;

        bool holdingAny = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (holdingAny && !isOrienting)
        {
            isOrienting = true;
            LockPlayerMovement();
        }
        else if (!holdingAny && isOrienting)
        {
            isOrienting = false;
            UnlockPlayerMovement();
        }
    }

    private bool IsBlockedByUI()
    {
        if (EventSystem.current == null || uiRaycaster == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        return false;
    }

    private bool CheckNear(Vector3 center)
    {
        float[] xArr; //x위치
        float[] zArr; //y위치
        float[] xSign; //x부호
        float[] zSign; //y부호

        //maxDistance보다 멀면 설치 불가능
        if (Vector3.SqrMagnitude(center - SnapToGrid(playerTrans.position)) > maxDistance * maxDistance)
        {
            return false;
        }

		#region 오브젝트에 따른 옵션 설정
		switch (creationType)
        {
            case CreationType.Platform:
                // 주훈 추가
                if (MastManager.Instance != null)
                {
                    int currentDeckCount = MastManager.Instance.currentDeckCount;
                    int maxDeckCount = 30; // 1레벨 최대 갯수

                    // 돗대 레벨에 따른 최대 개수 확인
                    MastSystem[] masts = FindObjectsOfType<MastSystem>();
                    if (masts.Length > 0)
                    {
                        maxDeckCount = masts[0].GetMaxDeckCount();
                    }

                    // 최대 개수 초과 시 설치 불가
                    if (currentDeckCount >= maxDeckCount)
                    {
                        Debug.Log($"갑판 설치 불가: {currentDeckCount}/{maxDeckCount}개 (최대 도달)");
                        return false;
                    }
                }
                // 여기까지

                //1.414213 * 0.5
                xArr = new float[] { 0.707106f, 0.707106f, -0.707106f, -0.707106f };
                zArr = new float[] { 0.707106f, -0.707106f, -0.707106f, 0.707106f };

                //마우스 위치에 플랫폼 있으면 설치 불가
                if (Physics.CheckBox(center, new Vector3(0.99f, 0.5f, 0.99f), Quaternion.Euler(new Vector3(0f, 45f, 0f)), platformLayer))
                {
                    return false;
                }

                //마우스 위치 기준 4방향에 직육면체(1.98, 1, 0.48) 범위에 플랫폼 있으면 설치 가능
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.99f, 0.5f, 0.249f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f * i + 45f, 0f));

                    // 여기에요 여기!!!!!!!!!!!!!!!! 바닥끼리 떨어져있을때 조건문!!!!!!!!!!!!!!!!!!!!!!!!!
                    if (Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                        return true;
                    //else
                    //ItemDeckDisconnect.instance.DestroyDeck();
                }

                //그 외 설치 불가
                return false;

            case CreationType.Wall:
                xArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };
                zArr = new float[] { -1.060659f, -0.353553f, 0.353553f, 1.060659f };

                for (int i = 0; i < xArr.Length; i++)
                {
                    float angle = onHand.transform.rotation.y;
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;

                    Vector3 offset = new Vector3(xArr[i] * xSign[rotateN % 4], 0f, zArr[i] * zSign[rotateN % 4]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;
                    int zIndex = rotateN % 2 == 0 ? i : xArr.Length - 1 - i;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[zIndex]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
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
                    float angle = onHand.transform.rotation.y;

                    Vector3 offset = new Vector3(xArr[i], 0f, zArr[i]);
                    Vector3 origin = center + offset;
                    Vector3 halfSize = new Vector3(0.49f, 0.5f, 0.49f);
                    Quaternion orientation = Quaternion.Euler(new Vector3(0f, 45f, 0f));

                    //오브젝트가 설치될 위치에 플랫폼이 없으면 설치 불가
                    if (!Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        Debug.Log("플랫폼 없음");
                        return false;
                    }
                    //마우스 위치에 오브젝트가 있으면 설치 불가
                    if (Physics.CheckBox(origin, halfSize, orientation, creationLayer))
                    {
                        Debug.Log("오브젝트 있음");
                        return false;
                    }
                }
                return true;
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

        // 보정 : 도착 판정 !!
        playerAgent.stoppingDistance = stopDistance;

        UnlockPlayerMovement();
        playerAgent.isStopped = false;
        playerAgent.ResetPath();
        playerAgent.SetDestination(stopPos);

        // 새 이동 시작이므로 타이머 리셋
        arrivedTimer = 0f;
    }

    private void Trim()
    {
        if (tempObj == null) return;

        // 도착 판정 : 속도 느림이 잠깐동안 지속되면 도착지점에 거의 다 왔다는 것으로 간주하고 설치 확정~
        bool almostStopped = playerAgent.velocity.sqrMagnitude <= arrivedSpeedEps * arrivedSpeedEps;
        bool nearEnough = !playerAgent.pathPending &&
                          playerAgent.remainingDistance <= playerAgent.stoppingDistance + 0.05f;

        if (nearEnough || almostStopped)
        {
            arrivedTimer += Time.deltaTime;
            if (arrivedTimer >= arrivedHoldTime)
            {
                var col = tempObj.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;

                var rend = tempObj.GetComponent<Renderer>();
                if (rend != null && rend.material != null) rend.material.color = Color.white;

                navMeshSurface.BuildNavMesh();
                GameManager.Instance?.NotifyPlatformLayoutChanged();

                //주훈 추가
                if (creationType == CreationType.Platform && MastManager.Instance != null)
                {
                    MastManager.Instance.UpdateCurrentDeckCount();
                    Debug.Log($"현재 갑판 갯수: {MastManager.Instance.currentDeckCount}");
                }
                //여기까지
                tempObj.GetComponent<InstalledObject>()?.OnPlaced();
                Debug.Log("[설치 완료됨]");

                playerAgent.ResetPath();
                playerAgent.isStopped = false;

                tempObj = null;
                arrivedTimer = 0f;
            }
        }
        else
        {
            arrivedTimer = 0f;
        }
    }

    void CancelInstall()
    {
        playerAgent.isStopped = true;
        playerAgent.ResetPath();
        if (tempObj != null) Destroy(tempObj);
        tempObj = null;
        arrivedTimer = 0f;
    }

    public void EnterInstallMode(SInstallableObjectDataSO installableSO)
    {
        if (onHand != null)
        {
            Destroy(onHand);
            onHand = null;
        }

        if (!playerAgent.enabled)
            playerAgent.enabled = true;

        // 설치 타입 설정
        creationType = (CreationType)(int)installableSO.installType;

        Debug.Log($"[설치모드 진입] 선택된 오브젝트: {installableSO.name}");

        CreateObjectInit(); // 새 프리뷰 오브젝트 생성
    }

    public void ExitInstallMode()
    {
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
        // 백업
        var bakType = creationType;
        var bakOnHand = onHand;
        var bakRotateN = rotateN;

        try
        {
            creationType = type;

            // onHand 대체용 더미 트랜스폼
            if (_evalDummy == null) _evalDummy = new GameObject("~EvalDummy");
            onHand = _evalDummy;
            onHand.transform.rotation = rot;

            // rotateN은 90도 단위 회전 지표
            rotateN = Mathf.RoundToInt(rot.eulerAngles.y / 90f) % 4;

            // 기존 설치 검증 로직 그대로 사용
            return CheckNear(worldPos);
        }
        finally
        {
            // 복원
            creationType = bakType;
            onHand = bakOnHand;
            rotateN = bakRotateN;
        }
    }
}
