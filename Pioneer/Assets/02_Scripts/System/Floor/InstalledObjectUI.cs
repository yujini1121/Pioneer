using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ��Ŭ������ ��ġ�� ������Ʈ�� �����ϰ�,
/// �г�(ȸ��/�̵�/����/�ݱ�/������)�� ��� ��ġ�� ǥ���Ѵ�.
/// - ȸ��: A/D Ű
/// - �̵�/����/�ݱ�: ��ư Ŭ��
/// �г��� ��Ʈ(InstalledObjectUI �ڽ�)���� ���� ��Ʈ�� ��Ȱ��ȭ���� �ʴ´�(������Ʈ ���� ����).
/// </summary>
public class InstalledObjectUI : MonoBehaviour
{
    public static InstalledObjectUI Instance { get; private set; }

    [Header("UI ����(���� Ȱ��ȭ ���)")]
    [SerializeField] RectTransform panel;      // �޴� ��Ʈ(�ڽ� ����). ����θ� �ڵ����� �ڽ�(transform)�� ���.
    [SerializeField] Button rotationButton;
    //[SerializeField] Button moveButton;
    [SerializeField] Button removeButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button repairButton;
    [SerializeField] GameObject durabilityUI;  // �ʿ�� ����(ǥ�ø�)
    [SerializeField] TextMeshProUGUI durabilityText;
    [SerializeField] Image repairImage1;
    [SerializeField] Image repairImage2;

    [Header("����/����ĳ��Ʈ")]
    [SerializeField] LayerMask interactableMask; // ��ġ ������Ʈ ���̾�

    [Header("ȸ�� Ű")]
    [SerializeField] KeyCode keyRotateLeft = KeyCode.A; // -90
    [SerializeField] KeyCode keyRotateRight = KeyCode.D; // +90

    [Header("�ƿ����")]
    [SerializeField] Material outlineMat;

    private enum Mode { Idle, Rotate, Move }
    private Mode mode = Mode.Idle;

    Camera cam;
    InstalledObject current;
    StructureBase structure;

    // �г��� ��Ʈ�� ��츦 ���� ���ü� ��ۿ�
    CanvasGroup panelCg;
    bool panelIsRoot => panel && panel.gameObject == gameObject;
    private NavMeshSurface nav;
    bool selectedThisFrame;


    void Awake()
    {
        // �̱���
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // �⺻ ����
        if (!panel) panel = (RectTransform)transform; // ����� ��� ��Ʈ ���
        cam = Camera.main;

        // ��Ʈ ��Ȱ��ȭ ����: �гο� CanvasGroup ����(������ �߰�)
        panelCg = panel.GetComponent<CanvasGroup>();
        if (!panelCg) panelCg = panel.gameObject.AddComponent<CanvasGroup>();

        // ��ư ������
        rotationButton.onClick.AddListener(() => { if (current) mode = Mode.Rotate; });
        //moveButton.onClick.AddListener(() => { if (current) { current.BeginMove(); mode = Mode.Move; } });
        removeButton.onClick.AddListener(() => { if (current) { current.Remove(); Hide(); RebuildStart(); } });
        closeButton.onClick.AddListener(Hide);
        repairButton.onClick.AddListener(Repair);

        Hide(); // ���� �� �г��� ����(��Ʈ�� Ȱ�� ���� ����)
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

        // ��Ŭ��: ��� ���� + ��ü UI Ȱ��ȭ + ��ġ ����
        if (Input.GetMouseButtonDown(0))
        {
            
            if (TryPick(out var obj) && TryPick<StructureBase>(out var structureBase) && structureBase.CanInteract)
            {
                SetSelection(obj);
                selectedThisFrame = true;
                ShowAt(current.transform.position);   // �� Ȱ��ȭ + ��ġ ����

                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer != null && outlineMat != null)
                {
                    List<Material> materials = new List<Material>(meshRenderer.sharedMaterials);
                    materials.Add(outlineMat);
                    meshRenderer.sharedMaterials = materials.ToArray();
                }

                structure = current.gameObject.GetComponent<StructureBase>();
                UpdateDurability();
            }
            else Hide();
        }
        // ��Ŭ��: �ش� ��� ����
        if (Input.GetMouseButtonDown(1))
        {
            if (TryPick<StructureBase>(out var structureBase))
            {
                PlayerRepair.instance.Repair(structureBase);
            }
        }

        if (!current) return;

        // ��� �����ӿ� ���� �г� ��ġ ����
        RepositionToCurrent();

        // ��� ó��
        switch (mode)
        {
            case Mode.Rotate:
                //if (Input.GetKeyDown(keyRotateLeft)) current.RotateLeft();
                //if (Input.GetKeyDown(keyRotateRight)) current.RotateRight();

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f) // ����
                {
                    current.RotateLeft();
                }
                else if (scroll < 0f) // �Ʒ���
                {
                    current.RotateRight();
                }

                break;

            case Mode.Move:
                Debug.Log("���� �̵� �������ݾƿ�");
                current.TickRelocate(cam);
                if (!current.IsRelocating)          // �̵� ���� �� Idle ����
                {
                    mode = Mode.Idle;
                    RepositionToCurrent();
                }
                break;
        }

        // �г� �� Ŭ�� or ESCŰ�� �ݱ�
        if (!selectedThisFrame && IsPanelVisible() && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, null) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                MeshRenderer currentRenderer = current != null ? current.GetComponent<MeshRenderer>() : null;
                Hide();
                if (currentRenderer != null)
                {
                    List<Material> materials = new List<Material>(currentRenderer.sharedMaterials);
                    if (materials.Count > 0 && outlineMat != null && materials[materials.Count - 1] == outlineMat)
                    {
                        materials.RemoveAt(materials.Count - 1);
                        currentRenderer.sharedMaterials = materials.ToArray();
                    }
                }
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
    /// �г� �ٽ� ���̰� Ȱ��ȭ 
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
    /// ���� ���� ��� ��ġ�� �г� ����
    /// </summary>
    void RepositionToCurrent()
    {
        if (!current) return;
        Reposition(current.transform.position);
    }

    /// <summary>
    /// ���� ��ǥ �� ���� ��ǥ�� ��ȯ
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
        if (RepairSystem.instance == null || structure == null) return;
        if (RepairSystem.instance.remainRepairCount <= 0) return;

        Debug.Log($"���� ��ư ����");

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
}
