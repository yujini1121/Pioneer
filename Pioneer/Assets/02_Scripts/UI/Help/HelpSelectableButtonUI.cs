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
    private readonly List<Color> runtimeColors = new List<Color>();

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
        runtimeColors.Clear();

        if (targetGraphics != null && targetGraphics.Count > 0)
        {
            for (int index = 0; index < targetGraphics.Count; ++index)
            {
                Graphic graphic = targetGraphics[index];
                if (graphic == null) continue;
                runtimeGraphics.Add(graphic);
                runtimeColors.Add(graphic.color);
            }
        }
        else
        {
            if (label != null)
            {
                runtimeGraphics.Add(label);
                runtimeColors.Add(label.color);
            }
            if (icon != null)
            {
                runtimeGraphics.Add(icon);
                runtimeColors.Add(icon.color);
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
        }
    }

    public void SetSelected(bool isSelected, float selectedAlpha, float normalAlpha)
    {
        float alpha = isSelected ? selectedAlpha : normalAlpha;

        for (int index = 0; index < runtimeGraphics.Count; ++index)
        {
            if (runtimeGraphics[index] == null) continue;

            Color color = runtimeColors[index];
            color.a = alpha;
            runtimeGraphics[index].color = color;
        }
    }
}
