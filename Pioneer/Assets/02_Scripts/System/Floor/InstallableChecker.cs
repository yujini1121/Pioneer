// ===================================================================================================
// 플레이어가 바닥 설치 명령을 받을 경우, 지정된 위치로 이동 후 설치
// 도중 조작 시 명령 취소되며, 인접 타일이 있을 경우에만 설치 허용됨
// ===================================================================================================

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class InstallableChecker : MonoBehaviour
{
    [Header("설치 조건")]
    public Camera mainCamera;
    public GameObject previewFloorPrefab;
    public LayerMask installableLayer;
    public LayerMask blockLayerMask;
    public Material validMaterial;
    public Material invalidMaterial;
    public Material placedMaterial;
    public Transform player;
    public float maxPlaceDistance;
    public Transform worldSpaceParent;
    public GameObject warningText;
    public float warningDuration = 1.5f;
    private Coroutine warningCoroutine;

    [Header("NavMesh 연결")]
    public NavMeshSurface navMeshSurface;
    public NavMeshAgent playerAgent;
    public float stopDistance = 1.5f; // 설치 지점 도달 전 멈출 거리

    private GameObject currentPreview;
    private Renderer previewRenderer;
    private Vector3 targetPosition;

    private const float positionOffset = 0.001f;

    // 설치 명령 이동 중인지 여부
    private bool isMovingToInstallPoint = false;
    private Vector3 destinationQueued;


    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (worldSpaceParent == null)
        {
            Debug.LogError("WorldSpace 부모를 할당해주세요.");
            return;
        }

        currentPreview = Instantiate(previewFloorPrefab, worldSpaceParent);
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localPosition = Vector3.zero;

        previewRenderer = currentPreview.GetComponent<Renderer>();

        Collider previewCollider = currentPreview.GetComponent<Collider>();
        if (previewCollider != null)
            previewCollider.isTrigger = true;

        if (previewRenderer == null)
            Debug.LogError("프리뷰에 Renderer가 없습니다.");
    }

    void Update()
    {
        HandlePreview();
        CheckArrivalAndInstall();

        // 설치 도중 플레이어 조작 감지 시 설치 명령 취소
        if (isMovingToInstallPoint)
        {
            float moveInputH = Input.GetAxisRaw("Horizontal");
            float moveInputV = Input.GetAxisRaw("Vertical");

            if (moveInputH != 0 || moveInputV != 0)
            {
                CancelInstall();
            }
        }
    }

    void HandlePreview()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayer, QueryTriggerInteraction.Collide))
        {
            Vector3 snappedPos = SnapToGrid(hit.point);
            targetPosition = snappedPos;

            // 지터링 보정
            snappedPos += new Vector3(positionOffset, positionOffset, -positionOffset);
            currentPreview.transform.localPosition = snappedPos;
            currentPreview.SetActive(true);

            bool canPlace = IsPlaceable(snappedPos);
            previewRenderer.material = canPlace ? validMaterial : invalidMaterial;

            if (canPlace)
            {
                if (Input.GetMouseButtonDown(0) && !isMovingToInstallPoint)
                {
                    StartMovingToInstall(snappedPos);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ShowWarningText();
                }
            }
        }
        else
        {
            currentPreview.SetActive(false);
            warningText.SetActive(false);
        }
    }

    void StartMovingToInstall(Vector3 snappedPos)
    {
        if (playerAgent == null || !playerAgent.isOnNavMesh)
            return;

        Vector3 worldTarget = worldSpaceParent.TransformPoint(snappedPos);

        // 방향 계산 → stopDistance 앞에서 멈춤
        Vector3 directionToTarget = (worldTarget - player.position).normalized;
        Vector3 stopBeforeTarget = worldTarget - directionToTarget * stopDistance;

        playerAgent.isStopped = false;
        playerAgent.SetDestination(stopBeforeTarget);

        destinationQueued = snappedPos;
        isMovingToInstallPoint = true;

        Debug.Log("설치 지점 인근으로 이동 시작");
    }

    void CheckArrivalAndInstall()
    {
        if (!isMovingToInstallPoint) return;

        bool arrived = !playerAgent.pathPending &&
                       playerAgent.remainingDistance <= playerAgent.stoppingDistance;

        if (arrived)
        {
            InstallTile(destinationQueued);

            isMovingToInstallPoint = false;
            destinationQueued = Vector3.zero;

            playerAgent.ResetPath();
            playerAgent.isStopped = false;

            Debug.Log("설치 완료 및 상태 초기화");
        }
    }

    void InstallTile(Vector3 localPosition)
    {
        GameObject tile = Instantiate(previewFloorPrefab, worldSpaceParent);
        tile.transform.localPosition = localPosition;
        tile.transform.localRotation = Quaternion.identity;

        Renderer r = tile.GetComponent<Renderer>();
        if (r != null && placedMaterial != null)
            r.material = placedMaterial;

        Collider c = tile.GetComponent<Collider>();
        if (c != null)
            c.isTrigger = false;

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        tile.name = $"Tile ({localPosition.x}, {localPosition.y}, {localPosition.z})";
        Debug.Log($"설치 완료: {localPosition}");
    }

    bool IsPlaceable(Vector3 snappedPos)
    {
        float distance = Vector3.Distance(player.position, worldSpaceParent.TransformPoint(snappedPos));
        if (distance > maxPlaceDistance)
            return false;

        Vector3 worldSnappedPos = worldSpaceParent.TransformPoint(snappedPos);
        Collider[] overlaps = Physics.OverlapBox(worldSnappedPos, Vector3.one * 0.45f, Quaternion.identity, blockLayerMask, QueryTriggerInteraction.Ignore);
        if (overlaps.Length > 0)
            return false;

        // 바닥 연결성 체크 (상하좌우)
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        float checkDistance = 1f;
        foreach (Vector3 dir in directions)
        {
            Vector3 checkPos = worldSnappedPos + dir * checkDistance;
            if (Physics.CheckBox(checkPos, Vector3.one * 0.45f, Quaternion.identity, blockLayerMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }

    void CancelInstall()
    {
        playerAgent.isStopped = true;
        playerAgent.ResetPath();
        isMovingToInstallPoint = false;
        destinationQueued = Vector3.zero;

        Debug.Log("플레이어 조작에 의해 설치 명령이 취소됨");
    }

    Vector3 SnapToGrid(Vector3 worldPos)
    {
        float cellSize = 1f;
        Vector3 localPos = worldSpaceParent.InverseTransformPoint(worldPos);
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);
        return new Vector3(x * cellSize, 0f, z * cellSize);
    }

    void ShowWarningText()
    {
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

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
