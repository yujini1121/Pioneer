using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TY_PrefabHolder : MonoBehaviour, IBegin
{
    public static TY_PrefabHolder instance;

    public GameObject woods;


    // Start is called before the first frame update
    void Init()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
