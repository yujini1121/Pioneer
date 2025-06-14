using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarinerStatus : MonoBehaviour
{
    public int maxHP = 70;
    public int currentHP;
    public int attackPower = 6; // 기본 공격력은 6

    public bool IsDead = false;
    public bool IsConfused = false;

    private void Start()
    {
        currentHP = maxHP;
    }

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
        Debug.Log("승무원 사망");
        Destroy(gameObject);
    }
}