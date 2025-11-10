using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoxUI : MonoBehaviour
{
    static public BoxUI instance;

    public GameObject BoxWindow;
    public CanvasGroup cg;
    [SerializeField] List<GameObject> slotGameObjects;
    [SerializeField] Canvas canvas;
    ItemSlotUI[] itemSlotUIs;

    public void ClickClose()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void Open()
    {
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        IconRefresh();
    }

    public void ClickSlot(int index)
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            InventoryManager.Instance.MouseSplit(index);
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            InventoryManager.Instance.MouseSingle(index);
        }
        else
        {
            InventoryManager.Instance.MouseSwitch(index);
        }

        InventoryUiMain.instance.MouseUI.Show(InventoryManager.Instance.mouseInventory);
        itemSlotUIs[index].Show(InventoryManager.Instance.itemLists[index]);

        InventoryManager.Instance.UpdateSlot();

        IconRefresh();
        PlayerStatUI.Instance.UpdateBasicStatUI();
    }

    public void ClickOut()
    {
        // 마우스 아이탬 핸들
        // 플레이어 아이템 핸들

        if (SItemStack.IsEmpty(InventoryManager.Instance.mouseInventory) == false)
        {
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭 {InventoryManager.Instance.mouseInventory.id} / {InventoryManager.Instance.mouseInventory.amount}");
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭1");
            InventoryManager.Instance.MouseDrop();
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭2");
            InventoryUiMain.instance.MouseUI.Clear();
            //Debug.Log($">> {gameObject.name} -> InventoryUiMain.ClickOut() : 아이템 드롭3");
            InventoryUiMain.instance.IconRefresh();
            PlayerStatUI.Instance.UpdateBasicStatUI();
            return;
        }

        if (PlayerCore.Instance.currentState != PlayerCore.PlayerState.ActionFishing)
        {
            // 플레이어 아이템 핸들
            Debug.Log($">> InventoryUiMain.ClickOut() : 아이템이 비어 있습니다.");
            if (SItemStack.IsEmpty(InventoryManager.Instance.SelectedSlotInventory))
            {
                // 빈 아이템 주먹 공격

                PlayerCore.Instance.BeginCoroutine(WeaponUseUtils.AttackCoroutine(
                    PlayerCore.Instance,
                    PlayerCore.Instance.dummyHandAttackItem,
                    PlayerCore.Instance.CalculatedHandAttack));
            }
            else
            {
                PlayerCore.Instance.BeginCoroutine(
                    ItemTypeManager.Instance.itemTypeSearch[
                        InventoryManager.Instance.SelectedSlotInventory.id].Use(
                                PlayerCore.Instance,
                                InventoryManager.Instance.SelectedSlotInventory
                            )
                    );
            }

            InventoryUiMain.instance.IconRefresh();
            PlayerStatUI.Instance.UpdateBasicStatUI();
        }
    }

    public void Sort()
    {
        InventoryManager.Instance.SortSelf();
        IconRefresh();
    }

    private void Awake()
    {
        instance = this;

        itemSlotUIs = new ItemSlotUI[slotGameObjects.Count];
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            itemSlotUIs[index] = slotGameObjects[index].GetComponent<ItemSlotUI>();
        }
    }

    // Start is called before the first frame update
    public void Start()
    //void Start()
    {

        IconRefresh();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mMousePos
        );
    }

    public void IconRefresh()
    {
        // 모든 아이템을
        // + 선택되지 않은 상태로 바꿈
        // + 내구도 체크
        for (int index = 0; index < slotGameObjects.Count; ++index)
        {
            //if (InventoryManager.Instance.itemLists[index] == null) continue;

            ItemSlotUI _forUi = slotGameObjects[index].GetComponent<ItemSlotUI>();

            _forUi.Show(InventoryManager.Instance.itemLists[index]);
            _forUi.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        }
        InventoryUiMain.instance.MouseUI.Show(InventoryManager.Instance.mouseInventory);
        InventoryUiMain.instance.MouseUI.image.gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        PlayerStatUI.Instance.UpdateBasicStatUI();

    }
}
