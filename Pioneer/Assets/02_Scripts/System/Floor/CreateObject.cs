using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CreateObject : MonoBehaviour
{
    enum CreationType { Platform, Wall, Door, Barricade, CraftingTable , Ballista, Trap, Lantern }

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

    [Header("생성 타입")]
    [SerializeField] private CreationType creationType;

    [Space(10f)]
    [SerializeField] private Transform worldSpaceParent;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask creationLayer;
    [SerializeField] private float maxDistance;

    [SerializeField] private CreationList creationList;
    private Dictionary<CreationType, GameObject> creationDict = new Dictionary<CreationType, GameObject>();

    [SerializeField] private Color rejectColor;
    [SerializeField] private Color permitColor;

    [Header("네브메시 설정")]
    [SerializeField] private NavMeshSurface navMeshSurface;
    private NavMeshAgent playerAgent;
    [SerializeField] private float stopDistance = 1.5f;

    [SerializeField] private bool onGizmos;

    private Transform playerTrans;

    private Camera mainCamera;
    private GameObject onHand;

    private Renderer creationRender;

    private GameObject createModel;

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

    private void Init()
    {
        onHand = Instantiate(creationDict[creationType], worldSpaceParent);
        onHand.transform.localRotation = Quaternion.identity;
        onHand.transform.localPosition = Vector3.zero;
        onHand.layer = 0;

        creationRender = onHand.GetComponent<Renderer>();
    }

    private void Update()
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

            //플랫폼 설치 가능 유무 판별
            if (CheckPlatformCreatable(worldPos))
            {
                creationRender.material.color = permitColor;

                if (Input.GetMouseButtonDown(0))
                {
                    if (createModel != null)
                    {
                        Destroy(createModel);
                    }

                    //설치 할 위치에 표시
                    createModel = Instantiate(creationDict[creationType], worldSpaceParent);
                    createModel.transform.localPosition = localPos;
                    createModel.transform.localRotation = Quaternion.identity;
                    createModel.GetComponent<Collider>().isTrigger = true;
                    createModel.GetComponent<Renderer>().material.color = permitColor;

                    //설치하러 이동
                    Vector3 dir = (worldPos - playerTrans.position).normalized;
                    Vector3 stopPos = worldPos - dir * stopDistance;

                    playerAgent.isStopped = false;
                    playerAgent.SetDestination(stopPos);
                }
            }
            else
            {
                creationRender.material.color = rejectColor;
            }
        }

        bool arrived = !playerAgent.pathPending && playerAgent.remainingDistance <= playerAgent.stoppingDistance;

        if (arrived)
        {
            if (createModel == null) return;

            createModel.GetComponent<Collider>().isTrigger = false;
            createModel.GetComponent<Renderer>().material.color = Color.white;

            navMeshSurface.BuildNavMesh();

            playerAgent.ResetPath();
            playerAgent.isStopped = false;

            createModel = null;
        }
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
    private bool CheckPlatformCreatable(Vector3 center)
    {
        //1.414213 * 0.5
        float[] xArr = { 0.707106f, 0.707106f, -0.707106f, -0.707106f };
        float[] yArr = { 0.707106f, -0.707106f, -0.707106f, 0.707106f };

        //maxDistance보다 멀면 설치 불가능
        if (Vector3.SqrMagnitude(center - SnapToGrid(playerTrans.position)) > Mathf.Pow(maxDistance, 2))
        {
            return false;
        }

        //마우스 위치에 플랫폼 있으면 설치 불가
        if (Physics.CheckBox(center, new Vector3(0.99f, 0.5f, 0.99f), Quaternion.Euler(new Vector3(0f, 45f, 0f)), platformLayer))
        {
            return false;
        }

        //마우스 위치 기준 4방향에 직육면체(1.98, 1, 0.48) 범위에 플랫폼 있으면 설치 가능
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3(xArr[i], 0f, yArr[i]);
            Vector3 origin = center + offset;
            if (Physics.CheckBox(origin, new Vector3(0.99f, 0.5f, 0.249f), Quaternion.Euler(new Vector3(0f, 45f * i + 45f, 0f)), platformLayer))
            {
                return true;
            }
        }

        //그 외 설치 불가
        return false;
    }

    void MoveToCreate(Vector3 moveTo, GameObject obj)
    {
        Vector3 dir = (moveTo - playerTrans.position).normalized;
        Vector3 stopPos = moveTo - dir * stopDistance;

        playerAgent.isStopped = false;
        playerAgent.SetDestination(stopPos);

        bool arrived = !playerAgent.pathPending && playerAgent.remainingDistance <= playerAgent.stoppingDistance;

        if (arrived)
        {
            //obj.GetComponent<Renderer>().material = defaultMat;
            obj.GetComponent<Collider>().isTrigger = false;

            playerAgent.ResetPath();
            playerAgent.isStopped = false;
        }
    }

    private void OnDrawGizmos()
    {
    }
}