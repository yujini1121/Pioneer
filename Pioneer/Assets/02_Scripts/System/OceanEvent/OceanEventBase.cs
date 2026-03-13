using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class OceanEventBase
{
    public string EventName { get; protected set; }
    public bool IsRunning { get; protected set; }

    public virtual void EventRun()
    {
        IsRunning = true;
    }

    public virtual void EnterNight() { }

    public virtual void EventEnd()
    {
        IsRunning = false;
    }
}