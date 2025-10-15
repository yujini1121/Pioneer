using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;


#warning TODO : CreateObject 수정이 필요
// 현재 : 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 배치
// 필요 : 레시피에서 제작 여부 가능 -> 제작 버튼 눌림 -> 제작 UI 끄기 -> 건설 UI 전환 -> 마우스 스냅 -> 건설 가능 여부 -> 이동 -> 시간 소모 및 방해받지 않는지 항상 체크 -> 아이템 소모 -> 배치

public class CreateObject : MonoBehaviour, IBegin
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable , Ballista, Trap, Lantern }

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
    [SerializeField] private float maxDistance;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask creationLayer;
    [SerializeField] private Color rejectColor;
    [SerializeField] private Color permitColor;
    [SerializeField] private CreationList creationList;
    private GameObject onHand;
    private GameObject tempObj;
    private Renderer creationRender;
    private Dictionary<CreationType, GameObject> creationDict = new Dictionary<CreationType, GameObject>();
    private int rotateN = 0;

    [Header("네브메시 설정")]
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private float stopDistance = 1.5f;
    private NavMeshAgent playerAgent;

    private void Awake()
    {
        Debug.Log($">> CreateObject : {gameObject.name}");
		instance = this;

        mainCamera = Camera.main;
        playerTrans = transform;
        playerAgent = GetComponent<NavMeshAgent>();

        creationDict.Add(CreationType.Platform,         creationList.platform);
        creationDict.Add(CreationType.Wall,             creationList.wall);
        creationDict.Add(CreationType.Door,             creationList.door);
        creationDict.Add(CreationType.Barricade,        creationList.barricade);
        creationDict.Add(CreationType.CraftingTable,    creationList.craftingTable);
        creationDict.Add(CreationType.Ballista,         creationList.ballista);
        creationDict.Add(CreationType.Trap,             creationList.trap);
        creationDict.Add(CreationType.Lantern,          creationList.lantern);

        CreateObjectInit();
    }

    //외부에서 'creationType' 수정 후 'Init'메서드 호출하여 초기화
    public void CreateObjectInit()
    {
        rotateN = 0;

        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>();
        onHand.GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        //ExitInstallMode(); // 게임 시작 시 설치 모드 OFF
    }

    private void Update()
    {
        if (onHand == null) return;

        CheckCreatable();
        Trim();

        if (tempObj != null && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
            CancelInstall();
    }

    //좌표 스냅
    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float cellSize = 1f;
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int z = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector3(x * cellSize, 0f, z * cellSize);
    }

    //설치 가능 유무 판별
    private void CheckCreatable()
    {
        #region UI 위에선 설치가능 여부 프리뷰부터 보이지 않게 처리함 
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            onHand.SetActive(false);    // 프리뷰 숨김
            return;                     // UI 클릭 중이면 설치/이동/회전 전부 무시
        }
        else
        {
            onHand.SetActive(true);     // UI에서 벗어나면 다시 보이게
        }
        #endregion

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        //마우스 위치로부터 y = 0인 지점 좌표 구하기
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorldPos = ray.GetPoint(enter);
            Vector3 localPos = SnapToGrid(worldSpaceParent.InverseTransformPoint(mouseWorldPos));
            onHand.transform.localPosition = localPos;
            Vector3 worldPos = onHand.transform.position;
            onHand.transform.position += Vector3.up * 0.01f;

            if (CheckNear(worldPos))
            {
                creationRender.material.color = permitColor;

                if (Input.GetMouseButtonDown(0))
                {
                    MoveToCreate(worldPos, localPos);
                }
            }
            else
            {
                creationRender.material.color = rejectColor;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            rotateN++;
            onHand.transform.localRotation = Quaternion.Euler(new Vector3(0f, 90f * rotateN, 0f));
        }
    }

    //주변 검사
    private bool CheckNear(Vector3 center)
    {
        float[] xArr; //x위치
        float[] zArr; //y위치
        float[] xSign; //x부호
        float[] zSign; //y부호

        //maxDistance보다 멀면 설치 불가능
        if (Vector3.SqrMagnitude(center - SnapToGrid(playerTrans.position)) > Mathf.Pow(maxDistance, 2))
        {
            return false;
        }

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
                xArr = new float[]{ 0.707106f, 0.707106f, -0.707106f, -0.707106f };
                zArr = new float[]{ 0.707106f, -0.707106f, -0.707106f, 0.707106f };

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

        return false;
    }

    //생성하러 이동
    private void MoveToCreate(Vector3 world, Vector3 local)
    {
        if (tempObj != null)
        {
            Destroy(tempObj);
        }

        //생성 할 위치 표시
        tempObj = Instantiate(creationDict[creationType], worldSpaceParent);
        tempObj.transform.localPosition = new Vector3(local.x, 0f, local.z);
        tempObj.transform.rotation = onHand.transform.rotation;
        tempObj.GetComponent<Collider>().isTrigger = true;
        tempObj.GetComponent<Renderer>().material.color = permitColor;

        Vector3 dir = (world - playerTrans.position).normalized;
        Vector3 stopPos = world - dir * stopDistance;

        playerAgent.isStopped = false;
        playerAgent.SetDestination(stopPos);
    }

    //생성 완료 절차
    private void Trim()
    {
        if (tempObj == null) return;

        float dist = Vector3.Distance(playerAgent.transform.position, tempObj.transform.position);
        if (dist < 2.0f)
        {
            // 여기서 시간을 소모한 뒤 물건을 빼앗아야 함.


            tempObj.GetComponent<Collider>().isTrigger = false;
            tempObj.GetComponent<Renderer>().material.color = Color.white;

            navMeshSurface.BuildNavMesh();

            //주훈 추가
            if (creationType == CreationType.Platform && MastManager.Instance != null)
            {
                MastManager.Instance.UpdateCurrentDeckCount();
                Debug.Log($"현재 갑판 갯수: {MastManager.Instance.currentDeckCount}");
            }
            //여기까지

            Debug.Log($"[설치 완료됨] 거리: {dist}");

            playerAgent.ResetPath();
            playerAgent.isStopped = false;

            tempObj = null;
        }
 
    }



    //조작으로 인한 움직임 캔슬
    void CancelInstall()
    {
        playerAgent.isStopped = true;
        playerAgent.ResetPath();
        Destroy(tempObj);
        tempObj = null;
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
        // 프리뷰 오브젝트 제거
        if (onHand != null)
        {
            Destroy(onHand);
            onHand = null;
        }

        // 임시 생성 오브젝트 제거 (이동 중 설치 예약된 오브젝트)
        if (tempObj != null)
        {
            Destroy(tempObj);
            tempObj = null;
        }

        // NavMeshAgent 상태 초기화
        playerAgent.ResetPath();
        playerAgent.isStopped = true;

        Debug.Log("[설치 모드 종료됨]");
    }
}