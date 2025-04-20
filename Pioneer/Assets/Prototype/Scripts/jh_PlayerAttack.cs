using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jh_PlayerAttack : MonoBehaviour
{  
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    private void Attack()
    {
        Debug.Log("player left click attack");
    }
}
