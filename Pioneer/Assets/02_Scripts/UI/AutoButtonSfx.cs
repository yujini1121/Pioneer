using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class AutoButtonSfx : MonoBehaviour
{
    public AudioManager.SFX defaultSfx = AudioManager.SFX.Click;

    void Start()
    {
        HashSet<GameObject> target = new HashSet<GameObject>();

        foreach (var btn in FindObjectsOfType<Button>(true))
        {
            if (btn.gameObject.name == "Outside")
                continue;

            target.Add(btn.gameObject);

            btn.onClick.AddListener(() =>
            {
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(defaultSfx);
            });
        }

        foreach (var btn in FindObjectsOfType<ItemSlotUI>(true))
        {
            if (target.Contains(btn.gameObject)) continue; // 중복 제거

            btn.buttonClickAction.Add(() =>
            {
                if (AudioManager.instance != null)
                    AudioManager.instance.PlaySfx(defaultSfx);
            });
        }
    }
}
