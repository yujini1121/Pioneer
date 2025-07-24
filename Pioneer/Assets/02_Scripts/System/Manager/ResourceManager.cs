using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum ResourceType { Wood, Energy, Hammer }

public class ResourceManager : MonoBehaviour, IBegin
{
    public static ResourceManager Instance;

    private Dictionary<ResourceType, int> resourceDict = new Dictionary<ResourceType, int>();
    public Dictionary<ResourceType, TextMeshProUGUI> resourceTexts = new Dictionary<ResourceType, TextMeshProUGUI>();

    [Header("UI 연동")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI hammerText;

    [Header("자동 에너지 생산")]
    public float energyInterval = 6f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 초기화
        resourceDict[ResourceType.Wood] = 0;
        resourceDict[ResourceType.Energy] = 0;
        resourceDict[ResourceType.Hammer] = 0;

        resourceTexts[ResourceType.Wood] = woodText;
        resourceTexts[ResourceType.Energy] = energyText;
        resourceTexts[ResourceType.Hammer] = hammerText;
    }

    private void Init()
    {
        UpdateAllTexts();
        StartCoroutine(EnergyAutoGain());
    }

    public void AddResource(ResourceType type, int amount)
    {
        resourceDict[type] += amount;
        UpdateText(type);
    }

    public bool UseResource(ResourceType type, int amount)
    {
        if (resourceDict[type] >= amount)
        {
            resourceDict[type] -= amount;
            UpdateText(type);
            return true;
        }
        return false;
    }

    public int GetAmount(ResourceType type)
    {
        return resourceDict[type];
    }

    private void UpdateText(ResourceType type)
    {
        if (resourceTexts.ContainsKey(type))
            resourceTexts[type].text = resourceDict[type].ToString();
    }

    private void UpdateAllTexts()
    {
        foreach (var kvp in resourceDict)
        {
            UpdateText(kvp.Key);
        }
    }

    IEnumerator EnergyAutoGain()
    {
        while (true)
        {
            yield return new WaitForSeconds(energyInterval);
            AddResource(ResourceType.Energy, 1);
        }
    }

    public void OnClickAddHammer()
    {
        ResourceManager.Instance.AddResource(ResourceType.Hammer, 1);
    }
}
