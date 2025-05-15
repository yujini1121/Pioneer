using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerStatus : MonoBehaviour
{
    public int maxHP = 70;
    public int currentHP;

    public bool IsDead = false;
    public bool IsConfused = false;

    public void UpdateStatus()
    {
        if (currentHP <= 0 && !IsDead)
        {
            Die();
        }
    }

    public void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
}

