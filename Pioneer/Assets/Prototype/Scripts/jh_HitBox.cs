using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jh_HitBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.gameObject.SetActive(false); 
        }
    }
}
