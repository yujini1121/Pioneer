using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class CreateObject : MonoBehaviour
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

        Init();
    }

    //외부에서 'creationType' 수정 후 'Init'메서드 호출하여 초기화
    public void Init()
    {
        rotateN = 0;

        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>();
        onHand.GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
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

                    if (Physics.CheckBox(origin, halfSize, orientation, platformLayer))
                    {
                        return true;
                    }
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
        tempObj.transform.localPosition = new Vector3(local.x, 0f, local.y);
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
        bool arrived = !playerAgent.pathPending && playerAgent.remainingDistance <= playerAgent.stoppingDistance;

        if (arrived)
        {
            if (tempObj == null)
            {
                return;
            }

            tempObj.GetComponent<Collider>().isTrigger = false;
            tempObj.GetComponent<Renderer>().material.color = Color.white;

            navMeshSurface.BuildNavMesh();

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
}