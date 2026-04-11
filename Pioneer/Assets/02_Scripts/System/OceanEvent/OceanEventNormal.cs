using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanEventNormal : OceanEventBase
{
    public OceanEventNormal()
    {
        EventName = "평범";
    }

    public override void EventRun()
    {
        base.EventRun();
        Debug.Log("[OceanEventNormal][평범 이벤트 시작]");
    }

    public override void EventEnd()
    {
        base.EventEnd();
        Debug.Log("[OceanEventNormal][평범 이벤트 종료]");
    }
}