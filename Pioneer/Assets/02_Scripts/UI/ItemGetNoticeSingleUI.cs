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
    public CanvasGroup canvasGroup;
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
            for (float t = 0.0f; t < 0.2f; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(0.0f, 1.0f, t / 0.2f);
                yield return null;
            }

            canvasGroup.alpha = 1.0f;
            yield return new WaitForSeconds(4.5f);

            for (float t = 0.0f; t < 0.5f; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, t / 0.5f);
                yield return null;
            }


            ItemGetNoticeUI.Instance.RemoveUI(index, this);
        }
        myCoroutine = StartCoroutine(mCoroutine());
    }


    private void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }
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
