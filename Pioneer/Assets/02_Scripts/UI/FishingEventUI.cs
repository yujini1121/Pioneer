using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishingEventUI : MonoBehaviour
{
    public GameObject fishingEvent_UI;
    public Slider slider;
    public RectTransform SuccessRange;
    public RectTransform arrow;

    public float moveDuration = 1.5f;
    private bool isSuccess = false;

    public IEnumerator StartQTE(Action<bool> eventResult)
    {
        fishingEvent_UI.SetActive(true);
        isSuccess = false;

        float startVal = UnityEngine.Random.Range(0.5f, 0.9f);
        SuccessRange.anchorMin = new Vector2 (startVal, 0);
        SuccessRange.anchorMax = new Vector2(startVal + 0.1f, 1);

        SuccessRange.offsetMin = Vector2.zero;
        SuccessRange.offsetMax = Vector2.zero;

        float timer = 0;
        bool forward = true;

        while (!Input.GetKeyDown(KeyCode.Space))
        {
            if(forward)
            {
                timer += Time.deltaTime / moveDuration;
                if (timer >= 1f) forward = false;
            }
            else
            {
                timer -= Time.deltaTime / moveDuration;
                if (timer <= 0f) forward = true;
            }

            slider.value = timer;

            arrow.anchorMin = new Vector2(timer, 1);
            arrow.anchorMax = new Vector2(timer, 1);
            arrow.anchoredPosition = new Vector2(0, arrow.anchoredPosition.y);

            yield return null;
        }

        if(slider.value >= startVal && slider.value <= startVal + 0.1f)
        {
            isSuccess = true;
        }

        fishingEvent_UI.SetActive(false);
        eventResult?.Invoke(isSuccess);
    }

    public void CloseUI()
    {
        StopAllCoroutines(); // 진행 중인 QTE 중단
        fishingEvent_UI.SetActive(false);
    }
}
