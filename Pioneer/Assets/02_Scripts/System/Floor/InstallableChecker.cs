using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class InstallableChecker : MonoBehaviour
{
    [Header("필수 연결")]
    public Camera mainCamera;
    public Transform player;
    public Transform worldSpaceParent;
    public NavMeshSurface navMeshSurface;

    [Header("레이어 설정")]
    public LayerMask installableLayer;
    public LayerMask blockLayerMask;

    [Header("설치 거리 설정")]
    public float maxPlaceDistance = 5f;
    public float stopDistance = 1.5f;

    [Header("프리뷰 유지 설정")]
    public float rayMissTolerance = 0.2f; // 마우스가 약간 벗어나도 유지
    private Vector3 lastValidHit = Vector3.zero;

    private SInstallableObjectDataSO currentInstallableData;
    private GameObject previewObject;
    private Renderer previewRenderer;
    private Color originalColor;

    private bool isMovingToInstallPoint = false;
    private Vector3 destinationQueued;

    void Update()
    {
        if (isMovingToInstallPoint)
        {
            CheckArrivalAndInstall();
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
                CancelInstall();
            return;
        }

        if (previewObject != null && currentInstallableData != null)
            HandlePreviewRaycast();
    }

    public void SetCurrentInstallableObject(SInstallableObjectDataSO data)
    {
        if (previewObject != null)
            Destroy(previewObject);

        currentInstallableData = data;

        previewObject = Instantiate(data.prefab, worldSpaceParent);
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localPosition = Vector3.zero;

        previewRenderer = previewObject.GetComponent<Renderer>();
        if (previewRenderer != null)
        {
            originalColor = previewRenderer.material.color;
            previewRenderer.material = new Material(data.previewMaterial); // 인스턴싱
        }

        Collider col = previewObject.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        previewObject.SetActive(false);
    }

    void HandlePreviewRaycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetWorldPos;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayer))
        {
            lastValidHit = hit.point;
            targetWorldPos = hit.point;
        }
        else if (lastValidHit != Vector3.zero)
        {
            Vector3 projected = ray.origin + ray.direction * 10f;
            if (Vector3.Distance(projected, lastValidHit) <= rayMissTolerance)
            {
                targetWorldPos = lastValidHit;
            }
            else
            {
                previewObject.SetActive(false);
                return;
            }
        }
        else
        {
            previewObject.SetActive(false);
            return;
        }

        Vector3 snappedLocalPos = SnapToGrid(targetWorldPos);
        Vector3 finalWorldPos = worldSpaceParent.TransformPoint(snappedLocalPos + new Vector3(0, currentInstallableData.yOffset, 0));

        previewObject.transform.localPosition = snappedLocalPos + new Vector3(0, currentInstallableData.yOffset, 0);
        previewObject.SetActive(true);

        bool canPlace = IsPlaceable(previewObject.transform.position, currentInstallableData.size);
        ApplyPreviewColor(canPlace ? Color.green : Color.red);

        if (canPlace && Input.GetMouseButtonDown(0))
            MoveToInstall(snappedLocalPos);
    }

    void MoveToInstall(Vector3 localInstallPos)
    {
        destinationQueued = localInstallPos;
        isMovingToInstallPoint = true;

        Vector3 worldTarget = worldSpaceParent.TransformPoint(localInstallPos);
        Vector3 direction = (worldTarget - player.position).normalized;
        Vector3 stopBeforeTarget = worldTarget - direction * stopDistance;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(stopBeforeTarget);
        }
    }

    void CheckArrivalAndInstall()
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                InstallObject(destinationQueued);
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

    void InstallObject(Vector3 localPos)
    {
        GameObject placed = Instantiate(currentInstallableData.prefab, worldSpaceParent);
        placed.transform.localPosition = localPos + new Vector3(0, currentInstallableData.yOffset, 0);
        placed.transform.localRotation = Quaternion.identity;

        Renderer r = placed.GetComponent<Renderer>();
        if (r != null)
            r.material = currentInstallableData.defaultMaterial;

        Collider c = placed.GetComponent<Collider>();
        if (c != null)
            c.isTrigger = false;

        if (placed.GetComponent<NavMeshObstacle>() == null)
        {
            NavMeshObstacle obstacle = placed.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = currentInstallableData.size;
            obstacle.carving = true;
        }

        StartCoroutine(RebakeNavMesh());
        Debug.Log("설치 완료: " + placed.name);
    }

    IEnumerator RebakeNavMesh()
    {
        yield return null;
        navMeshSurface?.BuildNavMesh();
    }

    bool IsPlaceable(Vector3 worldPos, Vector3 size)
    {
        Vector3 halfExtents = size / 2f;
        Collider[] hits = Physics.OverlapBox(worldPos, halfExtents * 0.95f, Quaternion.identity, blockLayerMask);
        float distance = Vector3.Distance(player.position, worldPos);
        return hits.Length == 0 && distance <= maxPlaceDistance;
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

        float finalX = (x + offsetX) * cellX;
        float finalZ = (z + offsetZ) * cellZ;

        return new Vector3(finalX, 0f, finalZ);
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
