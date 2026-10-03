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

    private Coroutine revealRoutine;

    private void Awake()
    {
        instance = this;
    }

    public void ShowItem(SItemStack sItemStack)
    {
        UtilityFunctions.Log(">> TreasureBoxUI.ShowItem : 보상 받음");

        SItemTypeSO itemType = ItemTypeManager.Instance.FindType(sItemStack);

        UITweenHelper.PlayOpen(TreasureWindow);
        itemImage.sprite = itemType.image;
        UITweenHelper.PunchScale(itemImage.transform, 0.12f, 0.18f);

        itemName.text = itemType.typeName;
        itemCount.text = $"x{sItemStack.amount} 획득";
        foreach (Button button in TreasureWindow.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method == nameof(PressDeny)) button.gameObject.SetActive(false);
                if (method == nameof(PressAccept))
                {
                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = "확인";
                }
            }
        }
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(AutoDismiss());
    }

    private IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(2f);
        revealRoutine = null;
        TreasureBoxManager.instance?.Accept();
    }

    public void CloseWindow()
    {
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = null;
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
