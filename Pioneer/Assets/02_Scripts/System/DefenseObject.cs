using UnityEngine;

public class DefenseObject : MonoBehaviour
{
    public int maxHP = 100;    
    public int currentHP = 40; 

    public void Repair(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        Debug.Log($"[{gameObject.name}]를 수리했습니다 : {currentHP}/{maxHP} HP");
    }

    public bool IsRepaired()
    {
        return currentHP >= maxHP * 0.5f;  
    }
}
