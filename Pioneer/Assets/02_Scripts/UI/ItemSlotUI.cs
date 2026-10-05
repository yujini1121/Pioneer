using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using DG.Tweening;

public class ItemSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    const float DURABILITY_FILL_SCALE = 0.9f;
    static Sprite durabilityFillSprite;
    private SlotTypeBorder typeBorder;
    private Tween selectionTween;
    private Vector3 restingScale;


    public int index;
    public UnityEngine.UI.Image image;
    public TextMeshProUGUI hotKey;
    public TextMeshProUGUI count;
    public TextMeshProUGUI durability;

    [SerializeField] private UnityEngine.UI.Image durabilityFill;
    [SerializeField] private Color durabilityHighColor = new Color(0.24f, 0.78f, 0.54f, 0.38f);
    [SerializeField] private Color durabilityMidColor = new Color(1f, 0.72f, 0.24f, 0.42f);
    [SerializeField] private Color durabilityLowColor = new Color(1f, 0.22f, 0.18f, 0.48f);
    [SerializeField] private float durabilityFillInset = 3f;

    public bool isSlot;
    public bool isRepairSlot;
    public List<System.Action> buttonClickAction;


    private void Awake()
    {
        buttonClickAction = new List<System.Action>();
        restingScale = transform.localScale;
        EnsureDurabilityFill();
    }

    public void Show(SItemStack item,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {

        if (item == null || item.id == 0)
        {
            Clear();
            return;
        }


        UtilityFunctions.Assert(item != null);
        UtilityFunctions.Assert(item.id != 0);
        UtilityFunctions.Assert(ItemTypeManager.Instance != null);
        UtilityFunctions.Assert(ItemTypeManager.Instance.itemTypeSearch != null);
        UtilityFunctions.Assert(ItemTypeManager.Instance.itemTypeSearch[item.id] != null);

        SItemTypeSO itemType = ItemTypeManager.Instance.itemTypeSearch[item.id];
        SetTypeBorder(itemType);
        bool isNeedShowDuability = itemType.categories == EDataType.WeaponItem;

        if (ItemTypeManager.Instance.itemTypeSearch[item.id].image != null)
        {
            image.enabled = true;
            image.sprite = ItemTypeManager.Instance.itemTypeSearch[item.id].image;
        }
        count.text = item.amount.ToString();

        if (isNeedShowDuability)
        {
            durability.text = "";
            SetDurabilityFill(item.duability);

            image.color = (item.duability > 0) ? Color.white : Color.red;
        }
        else
        {
            durability.text = "";
            HideDurabilityFill();

            image.color = Color.white;
        }
    }
    public void Clear()
    {

        image.enabled = false;
        count.text = "";
        durability.text = "";
        HideDurabilityFill();
        if (typeBorder != null) typeBorder.enabled = false;
    }

    private void SetTypeBorder(SItemTypeSO type)
    {
        if (!isSlot) return;
        if (typeBorder == null)
        {
            var frame = new GameObject("TypeBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(SlotTypeBorder));
            frame.transform.SetParent(transform, false);
            typeBorder = frame.GetComponent<SlotTypeBorder>();
            typeBorder.raycastTarget = false;
            var rect = typeBorder.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 3f); rect.offsetMax = new Vector2(-3f, -3f);
        }
        typeBorder.color = ItemPresentation.CategoryColor(type.categories);
        typeBorder.enabled = true;
    }

    #region
    public void ShowDurabilityPreview(int durabilityValue)
    {
        if (durability != null)
            durability.text = "";

        SetDurabilityFill(durabilityValue);
    }

    private void EnsureDurabilityFill()
    {
        if (durabilityFill == null)
        {
            GameObject fillObject = new GameObject("DurabilityFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            fillObject.transform.SetParent(transform, false);
            fillObject.transform.localScale = new Vector3(DURABILITY_FILL_SCALE, DURABILITY_FILL_SCALE, DURABILITY_FILL_SCALE);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(durabilityFillInset, durabilityFillInset);
            fillRect.offsetMax = new Vector2(-durabilityFillInset, -durabilityFillInset);

            durabilityFill = fillObject.GetComponent<UnityEngine.UI.Image>();
            durabilityFill.raycastTarget = false;
        }

        if (image != null && image.transform.parent == transform)
            durabilityFill.transform.SetSiblingIndex(image.transform.GetSiblingIndex() + 1);

        durabilityFill.transform.localScale = new Vector3(DURABILITY_FILL_SCALE, DURABILITY_FILL_SCALE, DURABILITY_FILL_SCALE);
        durabilityFill.sprite = GetDurabilityFillSprite();
        durabilityFill.type = UnityEngine.UI.Image.Type.Filled;
        durabilityFill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        durabilityFill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        durabilityFill.fillClockwise = true;
        HideDurabilityFill();
    }

    private void SetDurabilityFill(int durabilityValue)
    {
        EnsureDurabilityFill();

        float normalized = Mathf.Clamp01(durabilityValue / 100f);
        durabilityFill.enabled = true;
        durabilityFill.fillAmount = normalized;
        durabilityFill.color = GetDurabilityFillColor(normalized);
    }

    private void HideDurabilityFill()
    {
        if (durabilityFill == null)
            return;

        durabilityFill.enabled = false;
        durabilityFill.fillAmount = 0f;
    }

    private Color GetDurabilityFillColor(float normalized)
    {
        if (normalized <= 0.2f)
            return durabilityLowColor;

        if (normalized <= 0.5f)
            return durabilityMidColor;

        return durabilityHighColor;
    }

    private static Sprite GetDurabilityFillSprite()
    {
        if (durabilityFillSprite != null)
            return durabilityFillSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        durabilityFillSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        durabilityFillSprite.hideFlags = HideFlags.HideAndDontSave;
        return durabilityFillSprite;
    }
    #endregion

#region
    public void PlaySelectedFeedback()
    {
        selectionTween?.Kill();
        transform.DOKill();
        transform.localScale = restingScale;
        selectionTween = transform.DOPunchScale(restingScale * 0.07f, 0.18f, 1, 0.3f)
            .OnKill(() => { if (this != null) transform.localScale = restingScale; });
    }

    public void PlayClickFeedback()
    {
        Transform target = image != null && image.enabled ? image.transform : transform;
        UITweenHelper.PunchScale(target, 0.08f, 0.12f);
    }
#endregion

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSlot) InventoryUiMain.instance.currentSelectedSlot.Add(this);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSlot) InventoryUiMain.instance.currentSelectedSlot.Remove(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSlot)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryUiMain.instance.ClickSlot(index);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 우클릭 시
                InventoryUiMain.instance.RightClickSlot(index);
            }



        }
        else if (isRepairSlot)
        {
            RepairUI.instance.ClickSlot(index);
        }

        if (!isSlot) PlayClickFeedback();

        foreach (var one in buttonClickAction)
        {
            one();
        }
    }

    private void OnDisable()
    {
        selectionTween?.Kill(); selectionTween = null;
        transform.DOKill();
        transform.localScale = restingScale;
        if (image != null) image.transform.DOKill();
        InventoryUiMain.instance?.currentSelectedSlot?.Remove(this);
    }
}
