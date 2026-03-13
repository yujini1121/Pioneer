using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventWind : OceanEventBase
{
    public OceanEventWind()
    {
        EventName = "돌풍";
    }

    public override void EventRun()
    {
        base.EventRun();
        Debug.Log("[OceanEventWind][돌풍 이벤트 시작]");
    }

    public override void EventEnd()
    {
        base.EventEnd();
        Debug.Log("[OceanEventWind][돌풍 이벤트 종료]");
    }
}