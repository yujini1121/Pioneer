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

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0, 2f, 0); // 머리 위 높이 조절용

    public float moveDuration = 1.5f;
    private bool isSuccess = false;

    // UI 위치를 업데이트하는 로직
    private void LateUpdate()
    {
        // UI가 켜져 있을 때만 플레이어 위치를 따라감
        if (fishingEvent_UI.activeSelf)
        {
            UpdateUIPosition();
        }
    }

    private void UpdateUIPosition()
    {
        // PlayerCore.Instance.transform.position을 기준으로 UI 위치 고정
        // World Space Canvas가 아니라면 Camera.main.WorldToScreenPoint를 사용해야 할 수도 있습니다.
        Vector3 screenPos = Camera.main.WorldToScreenPoint(PlayerCore.Instance.transform.position + offset);
        fishingEvent_UI.transform.position = screenPos;
    }

    public IEnumerator StartQTE(Action<bool> eventResult)
    {
        // 시작 시 위치 초기화
        UpdateUIPosition();

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
