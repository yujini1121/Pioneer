using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityFunctions
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, string message = null)
    {
        Debug.Assert(condition, message);
    }
}
