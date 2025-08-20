using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 몇몇 메서드의 기하학적 매개변수 세팅을 위한 클래스입니다.
// 매개변수 너무 많으면 클래스나 구조체로 묶으란 말이 있습니다.
[Serializable]
public class ArgumentGeometry
{
    public GameObject parent;
    public int index;
    public int rowCount;
    public Vector2 delta2D;
    public Vector2 start2D;
    public Vector2 size;
}