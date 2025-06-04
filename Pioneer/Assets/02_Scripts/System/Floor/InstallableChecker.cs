using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class InstallableChecker : MonoBehaviour
{
    [Header("기본 설정")]
    public Camera mainCamera;
    public Transform player;
    public Transform worldSpaceParent;
    public NavMeshSurface navMeshSurface;

    [Header("레이어 설정")]
    public LayerMask installableLayer;
    public LayerMask blockLayerMask;

    [Header("설치 거리 설정")]
    public float maxPlaceDistance = 3f;
    public float stopDistance = 1.5f;

    [Header("설치 데이터")]
    public SInstallableObjectDataSO currentInstallableData;

    private GameObject previewObject;
    private Renderer previewRenderer;
    private Color originalColor;

    private Vector3 targetPosition;
    private bool isMovingToInstallPoint = false;
    private Vector3 destinationQueued;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (currentInstallableData != null)
            InitPreview(currentInstallableData);
    }

    void Update()
    {
        if (isMovingToInstallPoint)
        {
            CheckArrivalAndInstall();
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
                CancelInstall();
            return;
        }

        if (previewObject == null) return;
        HandlePreviewRaycast();
    }

    void HandlePreviewRaycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayer))
        {
            Vector3 snappedPos = SnapToGrid(hit.point);
            Vector3 finalPos = snappedPos + new Vector3(0, currentInstallableData.yOffset, 0);
            targetPosition = finalPos;

            previewObject.transform.localPosition = finalPos;
            previewObject.SetActive(true);

            bool canPlace = IsPlaceable(finalPos, currentInstallableData.size);
            ApplyPreviewColor(canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f));

            if (canPlace && Input.GetMouseButtonDown(0))
                MoveToInstall(finalPos);
        }
        else
        {
            previewObject.SetActive(false);
            ResetPreviewColor();
        }
    }

    public void SetCurrentInstallableObject(SInstallableObjectDataSO data)
    {
        if (previewObject != null)
            Destroy(previewObject);

        currentInstallableData = data;
        InitPreview(data);
    }

    void InitPreview(SInstallableObjectDataSO data)
    {
        previewObject = Instantiate(data.prefab, worldSpaceParent);
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localPosition = Vector3.zero;

        previewRenderer = previewObject.GetComponent<Renderer>();
        if (previewRenderer != null)
            originalColor = previewRenderer.material.color;

        Collider col = previewObject.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        previewObject.SetActive(false);
    }

    void MoveToInstall(Vector3 finalPos)
    {
        destinationQueued = finalPos;
        isMovingToInstallPoint = true;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        Vector3 worldTarget = worldSpaceParent.TransformPoint(finalPos);
        Vector3 direction = (worldTarget - player.position).normalized;
        Vector3 stopBeforeTarget = worldTarget - direction * stopDistance;

        // NavMesh 위치 샘플링으로 경로 보정
        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(stopBeforeTarget, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning("NavMeshAgent가 이동할 수 없는 위치입니다.");
                isMovingToInstallPoint = false;
            }
        }
    }

    void CheckArrivalAndInstall()
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                InstallTile();
                isMovingToInstallPoint = false;
                destinationQueued = Vector3.zero;

                agent.ResetPath();
                agent.isStopped = true;
            }
        }
    }

    void CancelInstall()
    {
        isMovingToInstallPoint = false;
        destinationQueued = Vector3.zero;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        Debug.Log("설치 명령 취소됨");
    }

    void InstallTile()
    {
        GameObject tile = Instantiate(currentInstallableData.prefab, worldSpaceParent);
        tile.transform.localPosition = destinationQueued;
        tile.transform.localRotation = Quaternion.identity;

        Collider c = tile.GetComponent<Collider>();
        if (c != null) c.isTrigger = false;

        Renderer r = tile.GetComponent<Renderer>();
        if (r != null) r.material.color = originalColor;

        tile.layer = LayerMask.NameToLayer("ClickTarget");

        if (tile.GetComponent<NavMeshObstacle>() == null)
        {
            NavMeshObstacle obstacle = tile.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = currentInstallableData.size;
            obstacle.carving = true;
        }

        StartCoroutine(DelayedRebakeNavMesh());
        Debug.Log("설치 완료");
    }

    IEnumerator DelayedRebakeNavMesh()
    {
        yield return null;
        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
    }

    bool IsPlaceable(Vector3 worldPos, Vector3 size)
    {
        Vector3 halfExtents = size / 2f;
        Collider[] overlaps = Physics.OverlapBox(worldPos, halfExtents, Quaternion.identity, blockLayerMask);
        if (overlaps.Length > 0) return false;

        Vector3 checkOrigin = worldSpaceParent.TransformPoint(worldPos) + Vector3.up * 0.5f;
        if (!Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit floorHit, 1f, installableLayer))
            return false;

        float distance = Vector3.Distance(player.position, worldPos);
        return distance <= maxPlaceDistance;
    }

    Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldSpaceParent.InverseTransformPoint(worldPos);

        float cellX = Mathf.Max(currentInstallableData.size.x, 1f);
        float cellZ = Mathf.Max(currentInstallableData.size.z, 1f);

        int x = Mathf.FloorToInt(localPos.x / cellX);
        int z = Mathf.FloorToInt(localPos.z / cellZ);

        float offsetX = (currentInstallableData.size.x % 2 == 0) ? 0.5f : 0f;
        float offsetZ = (currentInstallableData.size.z % 2 == 0) ? 0.5f : 0f;

        return new Vector3((x + offsetX) * cellX, 0f, (z + offsetZ) * cellZ);
    }

    void ApplyPreviewColor(Color color)
    {
        if (previewRenderer != null)
            previewRenderer.material.color = color;
    }

    void ResetPreviewColor()
    {
        if (previewRenderer != null)
            previewRenderer.material.color = originalColor;
    }
}
