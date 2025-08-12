using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class InstallableChecker : MonoBehaviour, IBegin
{
    [Header("기본 설정")]
    public Camera mainCamera;
    public Transform player;
    public Transform worldSpaceParent;
    public LayerMask installableLayer;
    public LayerMask blockLayerMask;
    public float maxPlaceDistance = 5f;
    public GameObject warningText;
    public float warningDuration = 1.5f;

    [Header("네브메시 설정")]
    public NavMeshSurface navMeshSurface;
    public NavMeshAgent playerAgent;
    public float stopDistance = 1.5f;

    [Header("설치 오브젝트 설정")]
    public SInstallableObjectDataSO currentInstallableData;

    private GameObject currentPreview;
    private Renderer previewRenderer;
    private Vector3 targetPosition;
    private Coroutine warningCoroutine;

    private bool isMovingToInstallPoint = false;
    private Vector3 destinationQueued;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (worldSpaceParent == null) { Debug.LogError("WorldSpace 부모를 할당해주세요."); return; }
        if (currentInstallableData == null) { Debug.LogError("설치할 오브젝트 데이터를 지정해주세요."); return; }

        InitPreview();
    }

    void Update()
    {
        HandlePreview();
        CheckArrivalAndInstall();

        if (isMovingToInstallPoint && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
            CancelInstall();
    }

    void InitPreview()
    {
        currentPreview = Instantiate(currentInstallableData.prefab, worldSpaceParent);
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localPosition = Vector3.zero;

        previewRenderer = currentPreview.GetComponent<Renderer>();
        if (previewRenderer == null) Debug.LogError("프리뷰에 Renderer가 없습니다.");

        var col = currentPreview.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void HandlePreview()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayer, QueryTriggerInteraction.Collide))
        {
            Vector3 localSnapped = SnapToGrid(hit.point) + new Vector3(0, 0, 0);
            targetPosition = localSnapped;

            currentPreview.transform.localPosition = targetPosition;
            currentPreview.SetActive(true);

            bool canPlace = IsPlaceable(localSnapped);

            if (previewRenderer != null)
                previewRenderer.material.color = canPlace ? Color.green : Color.red;

            if (canPlace && Input.GetMouseButtonDown(0) && !isMovingToInstallPoint)
                StartMovingToInstall(localSnapped);
            else if (!canPlace && Input.GetMouseButtonDown(0))
                //ShowWarningText();

            Debug.Log($"[PREVIEW] hit: {hit.point}, snapped(local): {localSnapped}");
        }
        else
        {
            currentPreview.SetActive(false);
            //warningText.SetActive(false);
        }
    }

    void StartMovingToInstall(Vector3 localSnapped)
    {
        if (playerAgent == null || !playerAgent.isOnNavMesh) return;

        Vector3 worldTarget = worldSpaceParent.TransformPoint(localSnapped);
        Vector3 dir = (worldTarget - player.position).normalized;
        Vector3 stopPos = worldTarget - dir * stopDistance;

        playerAgent.isStopped = false;
        playerAgent.SetDestination(stopPos);

        destinationQueued = localSnapped;
        isMovingToInstallPoint = true;

        Debug.Log($"[MOVE] moving to install position: {worldTarget}");
    }

    void CheckArrivalAndInstall()
    {
        if (!isMovingToInstallPoint) return;

        bool arrived = !playerAgent.pathPending && playerAgent.remainingDistance <= playerAgent.stoppingDistance;

        if (arrived)
        {
            InstallObject(destinationQueued);
            isMovingToInstallPoint = false;
            destinationQueued = Vector3.zero;

            playerAgent.ResetPath();
            playerAgent.isStopped = false;
        }
    }

    void InstallObject(Vector3 localPosition)
    {
        GameObject placed = Instantiate(currentInstallableData.prefab, worldSpaceParent);
        placed.transform.localPosition = localPosition;
        placed.transform.localRotation = Quaternion.identity;

        var c = placed.GetComponent<Collider>();
        if (c != null) c.isTrigger = false;

        placed.layer = LayerMask.NameToLayer(currentInstallableData.typeName.Contains("Floor") ? "Installable" : "Block");

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        Debug.Log($"[INSTALL] placed at: {localPosition} (world: {worldSpaceParent.TransformPoint(localPosition)})");
    }

    bool IsPlaceable(Vector3 localSnappedPos)
    {
        Vector3 worldSnappedPos = worldSpaceParent.TransformPoint(localSnappedPos);
        Vector3 halfSize = currentInstallableData.size * 0.5f;

        float dist = Vector3.Distance(player.position, worldSnappedPos);
        if (dist > maxPlaceDistance)
        {
            Debug.Log("[BLOCK] 거리 초과");
            return false;
        }

        // 오차범위 0.6f로 확장
        Collider[] overlaps = Physics.OverlapBox(worldSnappedPos, Vector3.one * 0.6f, Quaternion.identity, blockLayerMask);
        if (overlaps.Length > 0)
        {
            Debug.Log("[BLOCK] Overlap 감지됨");
            return false;
        }

        Vector3[] baseDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var dir in baseDirs)
        {
            Vector3 checkPos = worldSnappedPos + dir;

            if (Physics.CheckBox(checkPos, Vector3.one * 0.6f, Quaternion.identity, blockLayerMask))
            {
                Debug.Log("[PASS] 인접한 타일 존재함");
                return true;
            }
        }

        Debug.Log("[BLOCK] 인접 타일 없음");
        return false;
    }

    void CancelInstall()
    {
        playerAgent.isStopped = true;
        playerAgent.ResetPath();
        isMovingToInstallPoint = false;
        destinationQueued = Vector3.zero;
        Debug.Log("[CANCEL] 플레이어 조작에 의해 설치 취소");
    }

    Vector3 SnapToGrid(Vector3 worldPos)
    {
        float cellSize = 1f;
        Vector3 local = worldSpaceParent.InverseTransformPoint(worldPos);
        int x = Mathf.RoundToInt(local.x / cellSize);
        int z = Mathf.RoundToInt(local.z / cellSize);
        return new Vector3(x * cellSize, 0f, z * cellSize);
    }

    void ShowWarningText()
    {
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningText.SetActive(true);
        warningCoroutine = StartCoroutine(HideWarningTextAfterDelay());
    }

    IEnumerator HideWarningTextAfterDelay()
    {
        yield return new WaitForSeconds(warningDuration);
        warningText.SetActive(false);
        warningCoroutine = null;
    }
}

public static class DebugExtension
{
    public static void DebugBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration = 0f)
    {
        Vector3 halfSize = size * 0.5f;
        Vector3[] points = new Vector3[8];

        points[0] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        points[1] = center + rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        points[2] = center + rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        points[3] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

        points[4] = center + rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        points[5] = center + rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        points[6] = center + rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z);
        points[7] = center + rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z);

        Debug.DrawLine(points[0], points[1], color, duration);
        Debug.DrawLine(points[1], points[2], color, duration);
        Debug.DrawLine(points[2], points[3], color, duration);
        Debug.DrawLine(points[3], points[0], color, duration);

        Debug.DrawLine(points[4], points[5], color, duration);
        Debug.DrawLine(points[5], points[6], color, duration);
        Debug.DrawLine(points[6], points[7], color, duration);
        Debug.DrawLine(points[7], points[4], color, duration);

        Debug.DrawLine(points[0], points[4], color, duration);
        Debug.DrawLine(points[1], points[5], color, duration);
        Debug.DrawLine(points[2], points[6], color, duration);
        Debug.DrawLine(points[3], points[7], color, duration);
    }
}
