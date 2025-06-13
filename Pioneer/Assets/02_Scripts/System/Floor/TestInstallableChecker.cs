using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class TestInstallableChecker : MonoBehaviour
{
    [Header("카메라·플레이어")]
    [SerializeField] Camera mainCamera;
    [SerializeField] Transform player;
    [SerializeField] float maxPlaceDistance = 5f;

    [Header("레이어 마스크")]
    [SerializeField] LayerMask installableLayerMask; // Raycast 용
    [SerializeField] LayerMask blockLayerMask;       // 충돌 검사용

    [Header("NavMesh 이동")]
    [SerializeField] NavMeshSurface navMeshSurface;
    [SerializeField] float stopDistance = 1.5f;

    [Header("현재 SO")]
    [SerializeField] SInstallableObjectDataSO currentData;

    [Header("프리뷰 부모")]
    [SerializeField] Transform previewParent;

    // 런타임에 생성하는 머티리얼
    Material _previewValidMat;
    Material _previewInvalidMat;

    GameObject _previewInstance;
    NavMeshAgent _agent;
    Vector3 _queuedLocalPos;
    bool _isMovingToInstall;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        SetupPreviewMats();
        CreatePreviewInstance();
    }

    void Update()
    {
        if (currentData == null) return;

        UpdatePreview();
        CheckArrival();

        // 이동 도중 입력 시 설치 취소
        if (_isMovingToInstall &&
            (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
        {
            CancelInstallation();
        }
    }

    // 1) SO.previewMaterial 기반으로 Valid/Invalid 머티리얼 생성
    void SetupPreviewMats()
    {
        var baseMat = currentData.previewMaterial;
        _previewValidMat = new Material(baseMat);
        _previewInvalidMat = new Material(baseMat);

        // 색상만 덧씌우기 (0.5 알파 예시)
        _previewValidMat.color = new Color(0f, 1f, 0f, 0.5f);
        _previewInvalidMat.color = new Color(1f, 0f, 0f, 0.5f);
    }

    // 2) 프리뷰 인스턴스 생성 (처음에 한 번)
    void CreatePreviewInstance()
    {
        if (_previewInstance != null) Destroy(_previewInstance);
        _previewInstance = Instantiate(currentData.prefab, previewParent);
        _previewInstance.transform.localRotation = Quaternion.identity;
        _previewInstance.SetActive(false);

        // 트리거로 만들어서 물리 충돌 무시
        foreach (var c in _previewInstance.GetComponentsInChildren<Collider>())
            c.isTrigger = true;
    }

    // 3) 매 프레임 마우스 위치 → 프리뷰 갱신
    void UpdatePreview()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayerMask))
        {
            _previewInstance.SetActive(false);
            return;
        }

        // 그리드 스냅
        Vector3 localHit = previewParent.InverseTransformPoint(hit.point);
        Vector3 snapped = new Vector3(
            Mathf.Round(localHit.x),
            0,
            Mathf.Round(localHit.z)
        );
        _previewInstance.transform.localPosition = snapped + Vector3.up * currentData.yOffset;
        _previewInstance.SetActive(true);

        // 설치 가능 여부 판단
        bool canPlace = CanPlaceAt(_previewInstance.transform.position);
        ApplyPreviewMat(canPlace);

        // 클릭 시 이동 또는 워닝
        if (Input.GetMouseButtonDown(0) && !_isMovingToInstall)
        {
            if (canPlace) BeginMoveToInstall(snapped);
            else Debug.LogWarning("설치할 수 없는 위치입니다.");
        }
    }

    // 4) 설치 가능 판정 (거리 + OverlapBox + Floor 연결)
    bool CanPlaceAt(Vector3 worldPos)
    {
        if (Vector3.Distance(player.position, worldPos) > maxPlaceDistance)
            return false;

        if (Physics.OverlapBox(
                worldPos,
                currentData.size * 0.5f,
                Quaternion.identity,
                blockLayerMask
            ).Length > 0)
            return false;

        if (currentData.type == InstallableType.Floor)
        {
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (var d in dirs)
            {
                Vector3 nb = worldPos + Vector3.Scale(d, currentData.size);
                if (Physics.CheckBox(nb, currentData.size * 0.5f, Quaternion.identity, blockLayerMask))
                    return true;
            }
            return false;
        }

        return true;
    }

    // 5) 프리뷰 머티리얼 적용
    void ApplyPreviewMat(bool valid)
    {
        var mat = valid ? _previewValidMat : _previewInvalidMat;
        foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
        {
            var arr = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.materials = arr;
        }
    }

    // 6) NavMeshAgent 이동 시작
    void BeginMoveToInstall(Vector3 localPos)
    {
        if (!_agent.isOnNavMesh) return;
        _queuedLocalPos = localPos;
        _isMovingToInstall = true;

        Vector3 worldTarget = previewParent.TransformPoint(localPos + Vector3.up * currentData.yOffset);
        Vector3 dir = (worldTarget - player.position).normalized;
        Vector3 stopPt = worldTarget - dir * stopDistance;

        _agent.isStopped = false;
        _agent.SetDestination(stopPt);
    }

    // 7) 도착 체크 후 설치 코루틴 실행
    void CheckArrival()
    {
        if (!_isMovingToInstall) return;
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            StartCoroutine(DoBuild(_queuedLocalPos));
            _isMovingToInstall = false;
        }
    }

    IEnumerator DoBuild(Vector3 localPos)
    {
        yield return new WaitForSeconds(currentData.buildTime);

        // 실제 설치 인스턴스 생성
        Vector3 placePos = previewParent.TransformPoint(localPos + Vector3.up * currentData.yOffset);
        var placed = Instantiate(currentData.prefab, placePos, Quaternion.identity, previewParent);

        // 콜라이더 복원
        foreach (var c in placed.GetComponentsInChildren<Collider>())
            c.isTrigger = false;

        // SO.defaultMaterial로 머티리얼 일괄 설정
        foreach (var r in placed.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = currentData.defaultMaterial;
            r.sharedMaterials = mats;
        }

        // NavMesh 갱신
        navMeshSurface?.BuildNavMesh();
    }

    void CancelInstallation()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        _isMovingToInstall = false;
        Debug.Log("설치 취소됨");
    }
}
