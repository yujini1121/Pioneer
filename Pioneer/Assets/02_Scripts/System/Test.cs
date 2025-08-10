using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public float WaveHeight;
    public float WaveSpeed;

    Vector3 origin;

    private void Start()
    {
        origin = transform.position;
    }

    void Update()
    {
        transform.position = origin + new Vector3(0f, Mathf.Sin(Time.time * WaveSpeed) * WaveHeight, 0f);
    }
}