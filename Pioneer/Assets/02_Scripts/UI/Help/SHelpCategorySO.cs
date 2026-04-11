using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SHelpEntryData
{
    public int order;
    public string entryName;
    public string contentTitle;
    [TextArea(3, 10)] public string description;
    public Sprite contentImage;
}

[CreateAssetMenu(fileName = "HelpCategory", menuName = "ScriptableObjects/Help/Category", order = 1)]
public class SHelpCategorySO : ScriptableObject
{
    public int order;
    public string categoryName;
    public Sprite categorySprite;
    public string contentTitle;
    [TextArea(3, 10)] public string description;
    public Sprite contentImage;
    public List<SHelpEntryData> entries = new List<SHelpEntryData>();
}
