using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class crew : MonoBehaviour
{
    public Transform player;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //랜덤 돌리기
        float r = Random.Range(-180f, 181f) * Time.deltaTime;
        //방향 정하기
        transform.Rotate(0, r, 0);
        //플레이어 쳐다보기
        transform.LookAt(player);
    }
}
