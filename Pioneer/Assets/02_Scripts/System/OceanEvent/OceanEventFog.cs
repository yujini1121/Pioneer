using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventFog : OceanEventBase
{
    public OceanEventFog()
    {
        EventName = "안개";
    }

    public override void EventRun()
    {
        base.EventRun();
        Debug.Log("[OceanEventFog][안개 이벤트 시작]");
    }

    public override void EnterNight()
    {
        Debug.Log("[OceanEventFog][안개 밤 효과 적용]");
    }

    public override void EventEnd()
    {
        base.EventEnd();
        Debug.Log("[OceanEventFog][안개 이벤트 종료]");
    }
}