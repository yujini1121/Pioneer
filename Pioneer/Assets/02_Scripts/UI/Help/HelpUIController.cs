using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpUIController : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private List<SHelpCategorySO> categories = new List<SHelpCategorySO>();

    [Header("동적 버튼")]
    [SerializeField] private Transform categoryButtonRoot;
    [SerializeField] private Transform subCategoryButtonRoot;
    [SerializeField] private GameObject categoryButtonPrefab;
    [SerializeField] private GameObject subCategoryButtonPrefab;
    [SerializeField] private GameObject subCategoryPanel;

    [Header("표시 영역")]
    [SerializeField] private TextMeshProUGUI categoryTitleText;
    [SerializeField] private TextMeshProUGUI subCategoryTitleText;
    [SerializeField] private TextMeshProUGUI contentTitleText;
    [SerializeField] private TextMeshProUGUI contentDescriptionText;
    [SerializeField] private Image contentImage;
    [SerializeField] private Button closeButton;

    [Header("선택 표시")]
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.55f;

    private readonly List<HelpSelectableButtonUI> categoryButtons = new List<HelpSelectableButtonUI>();
    private readonly List<HelpSelectableButtonUI> subCategoryButtons = new List<HelpSelectableButtonUI>();
    private readonly List<SHelpCategorySO> sortedCategories = new List<SHelpCategorySO>();
    private readonly List<SHelpEntryData> currentEntries = new List<SHelpEntryData>();

    private int currentCategoryIndex = -1;
    private int currentEntryIndex = -1;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnEnable()
    {
        OpenDefaultPage();
    }

    public void OpenDefaultPage()
    {
        RebuildCategoryButtons();

        if (sortedCategories.Count > 0)
        {
            SelectCategory(0);
        }
        else
        {
            ClearSubCategoryButtons();
            ApplyContent(null, null, null);
        }
    }

    public void Close()
    {
        InGameUI.instance.CloseUI(InGameUI.ID_ESC_OPTION_HELP);
    }

    private void RebuildCategoryButtons()
    {
        ClearCategoryButtons();
        sortedCategories.Clear();

        if (categories == null) return;

        sortedCategories.AddRange(categories.Where(category => category != null)
            .OrderBy(category => category.order)
            .ThenBy(category => category.categoryName));

        if (categoryButtonRoot == null || categoryButtonPrefab == null) return;

        for (int index = 0; index < sortedCategories.Count; ++index)
        {
            int buttonIndex = index;
            SHelpCategorySO category = sortedCategories[index];
            GameObject one = Instantiate(categoryButtonPrefab, categoryButtonRoot);
            HelpSelectableButtonUI buttonUI = one.GetComponent<HelpSelectableButtonUI>();
            if (buttonUI == null) continue;

            buttonUI.SetData(category.categoryName, category.categorySprite, true);
            buttonUI.button.onClick.RemoveAllListeners();
            buttonUI.button.onClick.AddListener(() => SelectCategory(buttonIndex));
            categoryButtons.Add(buttonUI);
        }
    }

    private void RebuildSubCategoryButtons(SHelpCategorySO category)
    {
        ClearSubCategoryButtons();
        currentEntries.Clear();

        if (category == null || category.entries == null || category.entries.Count == 0)
        {
            if (subCategoryPanel != null) subCategoryPanel.SetActive(false);
            return;
        }

        currentEntries.AddRange(category.entries.Where(entry => entry != null)
            .OrderBy(entry => entry.order)
            .ThenBy(entry => entry.entryName));

        if (subCategoryPanel != null) subCategoryPanel.SetActive(true);
        if (subCategoryButtonRoot == null || subCategoryButtonPrefab == null) return;

        for (int index = 0; index < currentEntries.Count; ++index)
        {
            int buttonIndex = index;
            SHelpEntryData entry = currentEntries[index];
            GameObject one = Instantiate(subCategoryButtonPrefab, subCategoryButtonRoot);
            HelpSelectableButtonUI buttonUI = one.GetComponent<HelpSelectableButtonUI>();
            if (buttonUI == null) continue;

            buttonUI.SetData(entry.entryName, null, false);
            buttonUI.button.onClick.RemoveAllListeners();
            buttonUI.button.onClick.AddListener(() => SelectEntry(buttonIndex));
            subCategoryButtons.Add(buttonUI);
        }
    }

    private void SelectCategory(int index)
    {
        if (index < 0 || index >= sortedCategories.Count) return;

        currentCategoryIndex = index;
        currentEntryIndex = -1;

        for (int buttonIndex = 0; buttonIndex < categoryButtons.Count; ++buttonIndex)
        {
            categoryButtons[buttonIndex].SetSelected(buttonIndex == currentCategoryIndex, selectedAlpha, normalAlpha);
        }

        SHelpCategorySO category = sortedCategories[index];

        if (categoryTitleText != null)
        {
            categoryTitleText.text = category.categoryName;
        }

        RebuildSubCategoryButtons(category);

        if (currentEntries.Count > 0)
        {
            SelectEntry(0);
        }
        else
        {
            if (subCategoryTitleText != null)
            {
                subCategoryTitleText.text = string.Empty;
            }
            ApplyContent(category.contentTitle, category.description, category.contentImage);
        }
    }

    private void SelectEntry(int index)
    {
        if (index < 0 || index >= currentEntries.Count) return;

        currentEntryIndex = index;

        for (int buttonIndex = 0; buttonIndex < subCategoryButtons.Count; ++buttonIndex)
        {
            subCategoryButtons[buttonIndex].SetSelected(buttonIndex == currentEntryIndex, selectedAlpha, normalAlpha);
        }

        SHelpEntryData entry = currentEntries[index];

        if (subCategoryTitleText != null)
        {
            subCategoryTitleText.text = entry.entryName;
        }

        ApplyContent(entry.contentTitle, entry.description, entry.contentImage);
    }

    private void ApplyContent(string title, string description, Sprite sprite)
    {
        if (contentTitleText != null)
        {
            contentTitleText.text = string.IsNullOrEmpty(title) ? string.Empty : title;
        }

        if (contentDescriptionText != null)
        {
            contentDescriptionText.text = string.IsNullOrEmpty(description) ? string.Empty : description;
        }

        if (contentImage != null)
        {
            contentImage.sprite = sprite;
            contentImage.enabled = sprite != null;
        }
    }

    private void ClearCategoryButtons()
    {
        for (int index = 0; index < categoryButtons.Count; ++index)
        {
            if (categoryButtons[index] != null)
            {
                Destroy(categoryButtons[index].gameObject);
            }
        }
        categoryButtons.Clear();
    }

    private void ClearSubCategoryButtons()
    {
        for (int index = 0; index < subCategoryButtons.Count; ++index)
        {
            if (subCategoryButtons[index] != null)
            {
                Destroy(subCategoryButtons[index].gameObject);
            }
        }
        subCategoryButtons.Clear();
    }
}
