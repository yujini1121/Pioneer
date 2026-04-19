using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 클릭한 설치 오브젝트를 선택하고,
/// 패널(회전/이동/삭제/닫기/수리)을 해당 오브젝트 위에 표시한다.
/// - 회전: A/D 키 또는 마우스 휠
/// - 이동/삭제/닫기: 버튼 클릭
/// 패널이 루트(InstalledObjectUI 자신)일 때는 오브젝트를 비활성화하지 않고 CanvasGroup으로 숨긴다.
/// </summary>
public class InstalledObjectUI : MonoBehaviour
{
    public static InstalledObjectUI Instance { get; private set; }

    [Header("UI 설정(루트 활성화 방식)")]
    [SerializeField] RectTransform panel;      // 메뉴 루트(자식 권장). 비워두면 자동으로 자신(transform)을 사용.
    [SerializeField] Button rotationButton;
    //[SerializeField] Button moveButton;
    [SerializeField] Button removeButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button repairButton;
    [SerializeField] GameObject durabilityUI;  // 내구도 표시용(옵션)
    [SerializeField] TextMeshProUGUI durabilityText;
    [SerializeField] Image repairImage1;
    [SerializeField] Image repairImage2;

    [Header("선택/레이캐스트")]
    [SerializeField] LayerMask interactableMask; // 설치 오브젝트 레이어

    [Header("회전 키")]
    [SerializeField] KeyCode keyRotateLeft = KeyCode.A; // -90
    [SerializeField] KeyCode keyRotateRight = KeyCode.D; // +90

    [Header("아웃라인")]
    [SerializeField] Material outlineMat;

    private enum Mode { Idle, Rotate, Move }
    private Mode mode = Mode.Idle;

    Camera cam;
    InstalledObject current;
    StructureBase structure;

    // 패널이 루트일 때를 위한 상태 캐시
    CanvasGroup panelCg;
    bool panelIsRoot => panel && panel.gameObject == gameObject;
    private NavMeshSurface nav;
    bool selectedThisFrame;
    MeshRenderer outlinedRenderer;

    void Awake()
    {
        // 싱글톤
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 기본 설정
        if (!panel) panel = (RectTransform)transform; // 비워두면 자기 자신을 루트로 사용
        cam = Camera.main;

        // 루트 패널 숨김 처리: 패널에 CanvasGroup 보장(없으면 추가)
        panelCg = panel.GetComponent<CanvasGroup>();
        if (!panelCg) panelCg = panel.gameObject.AddComponent<CanvasGroup>();

        // 버튼 이벤트 연결
        rotationButton.onClick.AddListener(() => { if (current) mode = Mode.Rotate; });
        //moveButton.onClick.AddListener(() => { if (current) { current.BeginMove(); mode = Mode.Move; } });
        removeButton.onClick.AddListener(() => { if (current) { current.Remove(); Hide(); RebuildStart(); } });
        closeButton.onClick.AddListener(Hide);
        repairButton.onClick.AddListener(Repair);

        Hide(); // 시작 시 패널 숨김(루트면 활성 상태만 유지)
    }

    private void Start()
    {
        nav = FindObjectOfType<NavMeshSurface>();
    }

    void Update()
    {
        selectedThisFrame = false;

        if (structure != null && (structure.hp <= 0 || (!structure.CanInteract)))
        {
            Hide();
        }

        // 좌클릭: 대상 선택 + 오브젝트 UI 활성화 + 위치 갱신
        if (Input.GetMouseButtonDown(0))
        {
            
            if (TryPick(out var obj) && TryPick<StructureBase>(out var structureBase) && structureBase.CanInteract)
            {
                SetSelection(obj);
                selectedThisFrame = true;
                ShowAt(current.transform.position);   // UI 활성화 + 위치 갱신
                
                structure = current.gameObject.GetComponent<StructureBase>();
                UpdateDurability();
            }
            else Hide();
        }
        // 우클릭: 대상 즉시 수리
        if (Input.GetMouseButtonDown(1))
        {
            if (TryPick<StructureBase>(out var structureBase))
            {
                PlayerRepair.instance.Repair(structureBase);
            }
        }

        if (!current) return;

        // 현재 선택 대상에 맞춰 패널 위치 갱신
        RepositionToCurrent();

        // 모드 처리
        switch (mode)
        {
            case Mode.Rotate:
                //if (Input.GetKeyDown(keyRotateLeft)) current.RotateLeft();
                //if (Input.GetKeyDown(keyRotateRight)) current.RotateRight();

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f) // 위로
                {
                    current.RotateLeft();
                }
                else if (scroll < 0f) // 아래로
                {
                    current.RotateRight();
                }

                break;

            case Mode.Move:
                Debug.Log("설치물 이동 모드 진행 중");
                current.TickRelocate(cam);
                if (!current.IsRelocating)          // 이동 종료 시 Idle 복귀
                {
                    mode = Mode.Idle;
                    RepositionToCurrent();
                }
                break;
        }

        // 패널 밖 클릭 또는 ESC 키로 닫기
        if (!selectedThisFrame && IsPanelVisible() && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, null) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                if (!selectedThisFrame && IsPanelVisible() && Input.GetMouseButtonDown(0))
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, null) &&
                        (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                    {
                        Hide();
                    }
                }
            }
        }
        if (IsPanelVisible() && Input.GetKeyDown(KeyCode.Escape)) Hide();

        UpdateDurability();
    }

    void SetSelection(InstalledObject obj)
    {
        if (current == obj)
            return;

        RemoveOutline();

        current = obj;
        mode = Mode.Idle;

        if (current != null)
            ApplyOutline(current);
    }

    public bool TryPick(out InstalledObject obj)
    {
        obj = null;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f, interactableMask))
            obj = hit.collider.GetComponentInParent<InstalledObject>();
        return obj != null;
    }

    public bool TryPick<ParentGoComponent>(out ParentGoComponent obj) where ParentGoComponent : class
    {
        obj = null;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f, interactableMask))
            obj = hit.collider.GetComponentInParent<ParentGoComponent>();
        return obj != null;
    }


    /// <summary>
    /// 패널을 다시 보이게 하고 활성화
    /// </summary>
    /// <param name="worldPos"></param>
    void ShowAt(Vector3 worldPos)
    {
        if (panelIsRoot)
        {
            panelCg.alpha = 1f;
            panelCg.interactable = true;
            panelCg.blocksRaycasts = true;
        }
        else
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
        }

        rotationButton.gameObject.SetActive(true);
        //moveButton.gameObject.SetActive(true);
        removeButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        if (durabilityUI) durabilityUI.SetActive(true);
        repairButton.gameObject.SetActive(true);
        Reposition(worldPos);
    }

    /// <summary>
    /// 현재 선택된 대상 위치로 패널 재배치
    /// </summary>
    void RepositionToCurrent()
    {
        if (!current) return;
        Reposition(current.transform.position);
    }

    /// <summary>
    /// 월드 좌표를 패널 좌표로 변환
    /// </summary>
    void Reposition(Vector3 worldPos)
    {
        if (cam == null || panel == null || panel.parent == null) return;

        var sp = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)panel.parent, sp, null, out var lp);
        panel.anchoredPosition = lp;
    }

    public void RebuildStart()
    {
        StartCoroutine(Rebuild());
    }

    public IEnumerator Rebuild()
    {
        yield return new WaitForSeconds(0.2f);

        nav.BuildNavMesh();
        yield return null;
    }
    public void Hide()
    {
        RemoveOutline();

        current = null;
        structure = null;
        mode = Mode.Idle;

        if (panelIsRoot)
        {
            panelCg.alpha = 0f;
            panelCg.interactable = false;
            panelCg.blocksRaycasts = false;
        }
        else
        {
            if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        }

        if (durabilityUI) durabilityUI.SetActive(false);
    }

    bool IsPanelVisible()
    {
        return panelIsRoot ? panelCg.alpha > 0.0001f : panel.gameObject.activeSelf;
    }

    private void Repair()
    {
        if (RepairSystem.instance == null || structure == null) return;
        if (RepairSystem.instance.remainRepairCount <= 0) return;

        Debug.Log("수리 버튼 클릭");

        if (structure.ObjectData != null && structure.ObjectData.id == 50005)
        {
            if (RepairUI.instance != null)
                RepairUI.instance.Open();
        }
        else
        {
            if (PlayerRepair.instance != null)
                PlayerRepair.instance.Repair(structure);
        }

    }

    private void UpdateDurability()
    {
        if (structure == null || RepairSystem.instance == null) return;

        if (repairImage1 != null) repairImage1.color = (RepairSystem.instance.remainRepairCount > 0) ? Color.white : Color.red;
        if (repairImage2 != null) repairImage2.color = (RepairSystem.instance.remainRepairCount > 0) ? Color.white : Color.red;
        if (durabilityText != null && structure.maxHp > 0)
            durabilityText.text = $"{(structure.hp * 100) / structure.maxHp}%";
    }

    private MeshRenderer GetTargetRenderer(InstalledObject obj)
    {
        if (obj == null) return null;

        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = obj.GetComponentInChildren<MeshRenderer>();

        return meshRenderer;
    }

    private void ApplyOutline(InstalledObject obj)
    {
        if (obj == null || outlineMat == null)
            return;

        MeshRenderer meshRenderer = GetTargetRenderer(obj);
        if (meshRenderer == null)
            return;

        List<Material> materials = new List<Material>(meshRenderer.sharedMaterials);
        if (!materials.Contains(outlineMat))
        {
            materials.Add(outlineMat);
            meshRenderer.sharedMaterials = materials.ToArray();
        }

        outlinedRenderer = meshRenderer;
    }

    private void RemoveOutline()
    {
        if (outlinedRenderer == null || outlineMat == null)
        {
            outlinedRenderer = null;
            return;
        }

        List<Material> materials = new List<Material>(outlinedRenderer.sharedMaterials);
        bool removed = false;

        for (int i = materials.Count - 1; i >= 0; i--)
        {
            if (materials[i] == outlineMat)
            {
                materials.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
            outlinedRenderer.sharedMaterials = materials.ToArray();

        outlinedRenderer = null;
    }

    private void OnDisable()
    {
        RemoveOutline();
    }
}
