using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabHolderTeaYoon : MonoBehaviour
{
    public static PrefabHolderTeaYoon instance;

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
