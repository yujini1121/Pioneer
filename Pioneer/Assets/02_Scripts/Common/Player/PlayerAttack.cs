using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlayerAttack : MonoBehaviour, IBegin
{
    public int damage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<CreatureBase>()?.TakeDamage(damage, this.gameObject);
        }
    }
}
