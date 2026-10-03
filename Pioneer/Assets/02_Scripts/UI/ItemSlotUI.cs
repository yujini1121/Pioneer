using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class ItemSlotUI : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    const bool IS_DEBUG_LOG = false;

    const float DURABILITY_FILL_SCALE = 0.9f;
    static Sprite durabilityFillSprite;
    static Sprite itemTypeBorderSprite;


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

    [Header("Item type border")]
    [SerializeField, Range(0f, 1f)] private float itemTypeBorderOpacity = 0.9f;
    [SerializeField, Min(0.1f)] private float itemTypeBorderWidth = 4f;
    private UnityEngine.UI.Image itemTypeBorder;

    public bool isSlot;
    public bool isRepairSlot;
    public List<System.Action> buttonClickAction;


    private void Awake()
    {
        buttonClickAction = new List<System.Action>();
        EnsureDurabilityFill();
    }

    public void Show(SItemStack item,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = "")
    {
        if (IS_DEBUG_LOG) UtilityFunctions.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : 호출됨");

        if (item == null || item.id == 0)
        {
            Clear();
            return;
        }
        if (IS_DEBUG_LOG) UtilityFunctions.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : 내구도 = {item.duability}");


        UtilityFunctions.Assert(item != null);
        UtilityFunctions.Assert(item.id != 0);
        UtilityFunctions.Assert(ItemTypeManager.Instance != null);
        UtilityFunctions.Assert(ItemTypeManager.Instance.itemTypeSearch != null);
        UtilityFunctions.Assert(ItemTypeManager.Instance.itemTypeSearch[item.id] != null);
        if (IS_DEBUG_LOG) 
            UtilityFunctions.Log($">> ItemSlotUI.Show(SItemStack item)/IS_DEBUG_LOG : {item.id} / {item.amount}");

        SItemTypeSO itemType = ItemTypeManager.Instance.itemTypeSearch[item.id];
        SetItemTypeBorder(itemType.categories);
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
        //Debug.Log($">> {gameObject.name} -> ItemSlotUI.Clear() : 호출됨");
        
        image.enabled = false;
        count.text = "";
        durability.text = "";
        HideDurabilityFill();
        HideItemTypeBorder();
    }

    #region
    private void SetItemTypeBorder(EDataType category)
    {
        // Only inventory/hotbar slots get a border; the dragged icon and previews do not.
        Color borderColor = GetItemTypeBorderColor(category);
        if (!isSlot || borderColor.a == 0f)
        {
            HideItemTypeBorder();
            return;
        }

        if (itemTypeBorder == null)
        {
            GameObject borderObject = new GameObject("ItemTypeBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            borderObject.layer = gameObject.layer;
            borderObject.transform.SetParent(transform, false);
            borderObject.transform.SetAsFirstSibling();

            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            itemTypeBorder = borderObject.GetComponent<UnityEngine.UI.Image>();
            itemTypeBorder.raycastTarget = false;
            itemTypeBorder.sprite = GetItemTypeBorderSprite();
            itemTypeBorder.type = UnityEngine.UI.Image.Type.Sliced;
            itemTypeBorder.fillCenter = false;
        }

        itemTypeBorder.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, itemTypeBorderWidth);
        borderColor.a *= itemTypeBorderOpacity;
        itemTypeBorder.color = borderColor;
        itemTypeBorder.enabled = true;
    }

    private void HideItemTypeBorder()
    {
        if (itemTypeBorder != null)
            itemTypeBorder.enabled = false;
    }

    private static Color GetItemTypeBorderColor(EDataType category)
    {
        switch (category)
        {
            case EDataType.CommonResource: return new Color(1f, 0.76f, 0.36f);
            case EDataType.WeaponItem: return new Color(1f, 0.38f, 0.34f);
            case EDataType.NormalItem: return new Color(0.78f, 0.85f, 0.96f);
            case EDataType.ConsumeItem: return new Color(0.36f, 0.94f, 0.48f);
            case EDataType.BuildObject: return new Color(0.32f, 0.68f, 1f);
            case EDataType.Recipe: return new Color(0.80f, 0.43f, 1f);
            case EDataType.Unit: return new Color(0.27f, 0.94f, 0.86f);
            default: return Color.clear;
        }
    }

    private static Sprite GetItemTypeBorderSprite()
    {
        if (itemTypeBorderSprite != null)
            return itemTypeBorderSprite;

        Texture2D texture = new Texture2D(3, 3, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[9];
        for (int i = 0; i < pixels.Length; ++i)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();

        itemTypeBorderSprite = Sprite.Create(texture, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, Vector4.one);
        itemTypeBorderSprite.hideFlags = HideFlags.HideAndDontSave;
        return itemTypeBorderSprite;
    }

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
        Transform target = image != null && image.enabled ? image.transform : transform;
        UITweenHelper.PunchScale(target, 0.14f, 0.18f);
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

        PlayClickFeedback();

        foreach (var one in buttonClickAction)
        {
            one();
        }
    }
}
