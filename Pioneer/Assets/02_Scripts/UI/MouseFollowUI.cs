using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseFollowUI : MonoBehaviour, IBegin
{
    RectTransform rectTransform;

    void Init()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        Vector2 mousePosition = Input.mousePosition;
        rectTransform.position = mousePosition;
    }
}
