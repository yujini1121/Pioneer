using UnityEngine;

/// <summary>
/// 설치 완료된 오브젝트의 상호작용 컨트롤(회전/이동/삭제).
/// - 회전: UI에서 A/D 키로 호출(InstalledObjectUI가 current.RotateXX() 호출)
/// - 이동: Move 버튼 클릭 후, InstalledObjectUI가 매 프레임 TickRelocate(Camera) 호출
/// - 삭제: RemoveWithRefund()
/// - 배치 가능 여부(초록/빨강)는 CreateObject의 EvaluatePlacement()를 1줄 호출하여 재사용
/// </summary>
public class InstalledObject : MonoBehaviour
{
    [Header("기본 옵션")]
    public bool canRotate = true;
    public bool canMove = true;
    [Range(0, 100)] public int refundPercent = 70;

    [Header("설치 타입(검증용)")]
    // 프리팹마다 이 값을 올바른 CreationType으로 지정해야 EvaluatePlacement가 정확히 동작합니다.
    public CreateObject.CreationType installType;

    [Header("미리보기(이동 중)")]
    [SerializeField] float grid = 2f;                // 스냅 간격(설치 때와 동일 값 권장)
    [SerializeField] Color permitColor = Color.green;
    [SerializeField] Color rejectColor = Color.red;

    // 상태 노출(InstalledObjectUI에서 사용)
    public bool IsRelocating => _relocating;

    // 내부 상태
    bool _relocating;
    Vector3 _origPos;
    Quaternion _origRot;
    Renderer _rend;
    Color _origColor;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend && _rend.material) _origColor = _rend.material.color;
    }

    public void OnPlaced()
    {
        // 필요 시 초기화 훅
    }

    // ---------- UI 버튼에서 호출 ----------
    public void BeginMove()
    {
        if (!canMove || _relocating) return;
        _relocating = true;
        _origPos = transform.position;
        _origRot = transform.rotation;
        // 이동 시작 시 시각효과가 필요하면 여기서 추가(예: 반투명 등)
    }

    public void RemoveWithRefund()
    {
        // TODO: 환급(refundPercent) 로직을 프로젝트 규칙에 맞게 구현
        Destroy(gameObject);
    }

    // 회전: InstalledObjectUI가 A/D 키 입력을 받아 이 메서드를 호출
    public void RotateCW() { if (canRotate && !_relocating) transform.Rotate(0f, +90f, 0f); } // 3D(Y축)
    public void RotateCCW() { if (canRotate && !_relocating) transform.Rotate(0f, -90f, 0f); }

    // ---------- 이동 모드 틱: UI가 매 프레임 호출 ----------
    public void TickRelocate(Camera cam)
    {
        if (!_relocating || cam == null) return;

        // 1) 마우스 포인터의 월드 위치를 바닥 평면에 투영
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out var enter)) return;

        // 2) 그리드 스냅
        var p = ray.GetPoint(enter);
        p = new Vector3(Mathf.Round(p.x / grid) * grid, 0f, Mathf.Round(p.z / grid) * grid);

        // 3) 현재 회전을 90도 단위로 정리(이동 중엔 회전값 유지)
        float y = Mathf.Round(transform.eulerAngles.y / 90f) * 90f;
        var rot = Quaternion.Euler(0f, y, 0f);

        // 4) 설치 가능 여부 평가(초록/빨강) — CreateObject의 체크 재사용
        bool permitted = false;
        if (CreateObject.instance != null)
            permitted = CreateObject.instance.EvaluatePlacement(installType, p, rot);

        // 5) 미리보기(색/위치) 반영
        if (_rend && _rend.material) _rend.material.color = permitted ? permitColor : rejectColor;
        transform.SetPositionAndRotation(p, rot);

        // 6) 확정/취소
        if (Input.GetMouseButtonDown(0)) // 좌클릭 확정
        {
            if (permitted)
            {
                // 확정: 색상 복구 및 종료
                if (_rend && _rend.material) _rend.material.color = _origColor;

                // 필요 시 훅(네브메시/플랫폼 레이아웃 갱신 등)
                // GameManager.Instance?.NotifyPlatformLayoutChanged();
                // navMeshSurface.BuildNavMesh();

                _relocating = false;
            }
            // 미허용일 땐 무시(자리를 더 옮기게 둠)
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) // 우클릭/ESC 취소
        {
            transform.SetPositionAndRotation(_origPos, _origRot);
            if (_rend && _rend.material) _rend.material.color = _origColor;
            _relocating = false;
        }
    }
}
