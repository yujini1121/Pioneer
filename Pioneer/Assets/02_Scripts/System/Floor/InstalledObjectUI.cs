using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// 우클릭으로 설치된 오브젝트를 선택하고,
/// 패널(회전/이동/제거/닫기/내구도)을 대상 위치에 표시한다.
/// - 회전: A/D 키
/// - 이동/제거/닫기: 버튼 클릭
/// 패널이 루트(InstalledObjectUI 자신)여도 절대 루트를 비활성화하지 않는다(업데이트 정지 방지).
/// </summary>
public class InstalledObjectUI : MonoBehaviour
{
    public static InstalledObjectUI Instance { get; private set; }

    [Header("UI 참조(전부 활성화 대상)")]
    [SerializeField] RectTransform panel;      // 메뉴 루트(자식 권장). 비워두면 자동으로 자신(transform)을 사용.
    [SerializeField] Button rotationButton;
    [SerializeField] Button moveButton;
    [SerializeField] Button removeButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button repairButton;
    [SerializeField] GameObject durabilityUI;  // 필요시 연결(표시만)
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

    // 패널이 루트인 경우를 위한 가시성 토글용
    CanvasGroup panelCg;
    bool panelIsRoot => panel && panel.gameObject == gameObject;

    

    void Awake()
    {
        // 싱글톤
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 기본 참조
        if (!panel) panel = (RectTransform)transform; // 비워둔 경우 루트 사용
        cam = Camera.main;

        // 루트 비활성화 방지: 패널에 CanvasGroup 부착(없으면 추가)
        panelCg = panel.GetComponent<CanvasGroup>();
        if (!panelCg) panelCg = panel.gameObject.AddComponent<CanvasGroup>();

        // 버튼 리스너
        rotationButton.onClick.AddListener(() => { if (current) mode = Mode.Rotate; });
        moveButton.onClick.AddListener(() => { if (current) { current.BeginMove(); mode = Mode.Move; } });
        removeButton.onClick.AddListener(() => { if (current) { current.Remove(); Hide(); } });
        closeButton.onClick.AddListener(Hide);
        repairButton.onClick.AddListener(Repair);

        Hide(); // 시작 시 패널은 숨김(루트는 활성 상태 유지)
    }

    void Update()
    {
        // 우클릭: 대상 선택 + 전체 UI 활성화 + 위치 고정
        if (Input.GetMouseButtonDown(1))
        {
            CreateObject.instance.ExitInstallMode();

            if (TryPick(out var obj))
            {
                SetSelection(obj);
                ShowAt(current.transform.position);   // ★ 활성화 + 위치 지정

                List<Material> materials = new List<Material>(obj.GetComponent<MeshRenderer>().sharedMaterials);
                materials.Add(outlineMat);
                obj.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();

                structure = current.gameObject.GetComponent<StructureBase>();
                UpdateDurability();
            }
            else Hide();
        }

        if (!current) return;

        // 대상 움직임에 맞춰 패널 위치 추적
        RepositionToCurrent();

        // 모드 처리
        switch (mode)
        {
            case Mode.Rotate:
                if (Input.GetKeyDown(keyRotateLeft)) current.RotateLeft();
                if (Input.GetKeyDown(keyRotateRight)) current.RotateRight();
                break;

            case Mode.Move:
                Debug.Log("뭐야 이동 진입했잖아요");
                current.TickRelocate(cam);
                if (!current.IsRelocating)          // 이동 종료 → Idle 복귀
                {
                    mode = Mode.Idle;
                    RepositionToCurrent();
                }
                break;
        }

        // 패널 외 클릭 or ESC키로 닫기
        if (IsPanelVisible() && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, null) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                Hide();
                List<Material> materials = new List<Material>(current.GetComponent<MeshRenderer>().sharedMaterials);
                materials.RemoveAt(materials.Count - 1);
                current.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            }
        }
        if (IsPanelVisible() && Input.GetKeyDown(KeyCode.Escape)) Hide();

        UpdateDurability();
    }


    void SetSelection(InstalledObject obj)
    {
        current = obj;
        mode = Mode.Idle;
    }

    bool TryPick(out InstalledObject obj)
    {
        obj = null;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 1000f, interactableMask))
            obj = hit.collider.GetComponentInParent<InstalledObject>();
        return obj != null;
    }

    /// <summary>
    /// 패널 다시 보이게 활성화 
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
        moveButton.gameObject.SetActive(true);
        removeButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        if (durabilityUI) durabilityUI.SetActive(true);
        repairButton.gameObject.SetActive(true);
        Reposition(worldPos);
    }

    /// <summary>
    /// 현재 선택 대상 위치로 패널 고정
    /// </summary>
    void RepositionToCurrent()
    {
        if (!current) return;
        Reposition(current.transform.position);
    }

    /// <summary>
    /// 월드 좌표 → 로컬 좌표로 변환
    /// </summary>
    void Reposition(Vector3 worldPos)
    {
        var sp = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)panel.parent, sp, null, out var lp);
        panel.anchoredPosition = lp;
    }

    public void Hide()
    {

        current = null;
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
        if (InventoryManager.Instance.Get(40007) <= 0) return;

        Debug.Log($"수리 버튼 눌림");

        if (structure.ObjectData.id == 50005)
        {
            RepairUI.instance.Open();
        }
        else
        {
            PlayerRepair.instance.Repair(structure);
        }

    }

    private void UpdateDurability()
    {
        repairImage1.color = (InventoryManager.Instance.Get(40007) > 0) ? Color.white : Color.red;
        repairImage2.color = (InventoryManager.Instance.Get(40007) > 0) ? Color.white : Color.red;

        durabilityText.text = $"{(structure.hp * 100) / structure.maxHp}%";
    }
}
