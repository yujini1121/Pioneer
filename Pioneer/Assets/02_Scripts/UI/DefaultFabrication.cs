using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DefaultFabrication : MonoBehaviour
{
    public const string CraftStart = "제작 시작";
    public const string CraftEnd = "제작 중단";

    [Header("UI")]
    public GameObject pivotMaterial1;
    public GameObject pivotMaterial2;
    public GameObject pivotMaterial3;
    public TextMeshProUGUI craftName;
    public UnityEngine.UI.Image material1iconImage;
    public UnityEngine.UI.Image material2iconImage;
    public UnityEngine.UI.Image material3iconImage;
    public TextMeshProUGUI material1eaText;
    public TextMeshProUGUI material2eaText;
    public TextMeshProUGUI material3eaText;
    public TextMeshProUGUI craftLore;
    public TextMeshProUGUI timeLeft;
    public TextMeshProUGUI craftButtonWord;
    public UnityEngine.UI.Button craftButton;
    public UnityEngine.UI.Button closeTabButton;

    public UnityEngine.UI.Image[] materialIconImage => new UnityEngine.UI.Image[]
    {
        material1iconImage,
        material2iconImage,
        material3iconImage
    };
    public TextMeshProUGUI[] materialEachText => new TextMeshProUGUI[]
    {
        material1eaText,
        material2eaText,
        material3eaText
    };
    public GameObject[] materialPivots => new GameObject[]
    {
        pivotMaterial1,
        pivotMaterial2,
        pivotMaterial3
    };
    private void Start()
    {
        closeTabButton.onClick.AddListener(() => CommonUI.instance.CloseTab(this));
    }
}
