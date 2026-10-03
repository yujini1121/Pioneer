using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpSelectableButtonUI : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI label;
    public Image icon;
    public List<Graphic> targetGraphics = new List<Graphic>();

    private readonly List<Graphic> runtimeGraphics = new List<Graphic>();

    private void Awake()
    {
        CacheGraphics();
    }

    private void OnEnable()
    {
        CacheGraphics();
    }

    private void CacheGraphics()
    {
        runtimeGraphics.Clear();

        if (targetGraphics != null && targetGraphics.Count > 0)
        {
            for (int index = 0; index < targetGraphics.Count; ++index)
            {
                Graphic graphic = targetGraphics[index];
                if (graphic == null) continue;
                runtimeGraphics.Add(graphic);
            }
        }
        else
        {
            if (label != null)
            {
                runtimeGraphics.Add(label);
            }
            if (icon != null)
            {
                runtimeGraphics.Add(icon);
            }
        }
    }

    public void SetData(string displayName, Sprite displaySprite, bool useIcon)
    {
        if (label != null)
        {
            label.text = displayName;
        }

        if (icon != null)
        {
            icon.sprite = displaySprite;
            icon.enabled = useIcon && displaySprite != null;
            icon.preserveAspect = true;
        }
    }

    public void SetSelected(bool isSelected, Color selectedColor, Color normalColor)
    {
        Color color = isSelected ? selectedColor : normalColor;

        for (int index = 0; index < runtimeGraphics.Count; ++index)
        {
            if (runtimeGraphics[index] == null) continue;

            runtimeGraphics[index].color = color;
        }
    }
}
