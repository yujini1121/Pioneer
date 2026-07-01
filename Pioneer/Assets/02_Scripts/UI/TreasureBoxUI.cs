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

        UITweenHelper.PlayOpen(TreasureWindow);
        itemImage.sprite = itemType.image;
        UITweenHelper.PunchScale(itemImage.transform, 0.12f, 0.18f);

        itemName.text = itemType.typeName;
        itemCount.text = $"x{sItemStack.amount}";
    }

    public void CloseWindow()
    {
        UITweenHelper.PlayClose(TreasureWindow, () => TreasureWindow.SetActive(false));
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
