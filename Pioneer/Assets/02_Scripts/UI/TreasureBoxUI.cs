using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreasureBoxUI : MonoBehaviour
{
    public static TreasureBoxUI instance;

    [SerializeField] GameObject TreasureWindow;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] TextMeshProUGUI itemCount;

    private void Awake()
    {
        instance = this;
    }

    public void ShowItem(SItemStack sItemStack)
    {
        Debug.Log(">> TreasureBoxUI.ShowItem : 보상 받음");

        SItemTypeSO itemType = ItemTypeManager.Instance.FindType(sItemStack);

        TreasureWindow.SetActive(true);
        itemImage.sprite = itemType.image;

        itemName.text = itemType.typeName;
        itemCount.text = $"(x{sItemStack.amount})";
    }

    public void CloseWindow()
    {
        TreasureWindow.SetActive(false);
    }

    public void PressAccept()
    {
        TreasureBoxManager.instance.Accept();
    }

    public void PressDeny()
    {
        TreasureBoxManager.instance.Deny();
    }
}
