using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemGetNoticeSingleUI : MonoBehaviour
{
    public Coroutine myCoroutine;
    public Image icon;
    public TextMeshProUGUI text;
    public int index;

    public void Show(SItemStack target)
    {
        icon.sprite = target.itemBaseType.image;
        text.text = $"{target.itemBaseType.typeName} {target.amount}°³ È¹µæ";
    }

    public void Begin()
    {
        IEnumerator mCoroutine()
        {
            yield return new WaitForSeconds(5);
            ItemGetNoticeUI.Instance.RemoveUI(index, this);
        }
        myCoroutine = StartCoroutine(mCoroutine());
    }

    


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



}
