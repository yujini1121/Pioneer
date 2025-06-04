using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DefaultFabrication : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI craftName;
    public UnityEngine.UI.Image material1iconImage;
    public UnityEngine.UI.Image material2iconImage;
    public UnityEngine.UI.Image material3iconImage;
    public TextMeshProUGUI material1eaText;
    public TextMeshProUGUI material2eaText;
    public TextMeshProUGUI material3eaText;
    public TextMeshProUGUI craftLore;
    public UnityEngine.UI.Button craftButton;

    public UnityEngine.UI.Image[] materialIconImage => new UnityEngine.UI.Image[]
    {
        material1iconImage,
        material1iconImage,
        material1iconImage
    };
    public TextMeshProUGUI[] materialEachText => new TextMeshProUGUI[]
    {
        material1eaText,
        material2eaText,
        material3eaText
    };
}
