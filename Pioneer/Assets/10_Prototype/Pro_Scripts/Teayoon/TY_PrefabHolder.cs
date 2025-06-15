using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TY_PrefabHolder : MonoBehaviour
{
    public static TY_PrefabHolder instance;

    public GameObject woods;


    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
