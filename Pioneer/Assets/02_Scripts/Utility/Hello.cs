using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hello : MonoBehaviour
{
    public void Say()
    {
        UtilityFunctions.Log($">> Hello.Say()");
    }
}
