using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayUI : MonoBehaviour
{
    public TextMeshProUGUI currentDay;

    private void Update()
    {
        if (currentDay == null || GameManager.Instance == null)
            return;

        currentDay.text = "Day " + GameManager.Instance.currentDay.ToString();
    }
}
