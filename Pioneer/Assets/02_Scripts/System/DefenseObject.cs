using UnityEngine;

public class DefenseObject : MonoBehaviour
{
    public int maxHP = 100;    
    public int currentHP = 40; 

    public void Repair(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        Debug.Log(" 오브젝트 수리 완료 HP + 30 (임시)");
    }

    public bool IsRepaired()
    {
        return currentHP >= maxHP * 0.5f;  
    }
}
