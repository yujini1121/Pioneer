using Unity.AI.Navigation;
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
    public CreateObject.CreationType installType;

    [Header("미리보기(이동 중)")]
    [SerializeField] float grid = 2f;                // 스냅 간격(설치 때와 동일 값이어야 함
    [SerializeField] Color permitColor = Color.green;
    [SerializeField] Color rejectColor = Color.red;
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
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.InstallObject);

        // 설치 직후 1회 초기화 훅
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_rend && _rend.material) _origColor = _rend.material.color;

        if (CreatureEffect.Instance != null)
        {
            ParticleSystem ps = CreatureEffect.Instance.Effects[0]; 
            CreatureEffect.Instance.PlayEffect(ps, transform.position + new Vector3(0f,1f,0f));
            Debug.Log("설치 이펙트 호출");
        }

        // 이동 상태는 반드시 해제
        _relocating = false;
    }

    public void BeginMove()
    {
        if (!canMove || _relocating) return;
        _relocating = true;
        _origPos = transform.position;
        _origRot = transform.rotation;
    }

    public void Remove()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.DestroyedObject);


        if (CreatureEffect.Instance != null)
        {
            var ps = CreatureEffect.Instance.Effects[1]; 
            CreatureEffect.Instance.PlayEffect(ps, transform.position);
        }

        Destroy(gameObject);
    }

    // 회전
    public void RotateRight() { if (canRotate && !_relocating) transform.Rotate(0f, +90f, 0f); } 
    public void RotateLeft() { if (canRotate && !_relocating) transform.Rotate(0f, -90f, 0f); }


    public void TickRelocate(Camera cam)
    {
        if (!_relocating || cam == null) return;

        // 마우스 포인터의 월드 위치 Ray
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out var enter)) return;

        // 그리드 스냅
        var p = ray.GetPoint(enter);
        p = new Vector3(Mathf.Round(p.x / grid) * grid, 0f, Mathf.Round(p.z / grid) * grid);

        // 이동 중엔 회전값 유지
        float y = Mathf.Round(transform.eulerAngles.y / 90f) * 90f;
        var rot = Quaternion.Euler(0f, y + 45f, 0f);

        // 설치 가능 여부 평가(초록/빨강) — CreateObject의 체크 재사용
        bool permitted = false;
        if (CreateObject.instance != null)
            permitted = CreateObject.instance.EvaluatePlacement(installType, p, rot);

        if (_rend && _rend.material) _rend.material.color = permitted ? permitColor : rejectColor;
        transform.SetPositionAndRotation(p, rot);

        // 6) 확정/취소
        if (Input.GetMouseButtonDown(0)) // 좌클릭 확정
        {
            if (permitted)
            {
                // 확정: 색상 복구 및 종료
                if (_rend && _rend.material)
                {
                    _rend.material.color = _origColor;
                    CreateObject.instance.navMeshSurface.BuildNavMesh();
                }
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
