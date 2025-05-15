using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool IsDaytime = true;
    private List<DefenseObject> repairTargets = new List<DefenseObject>(); 

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        UpdateRepairTargets();
    }

    public void UpdateRepairTargets()
    {
        repairTargets.Clear();  

        DefenseObject[] defenseObjects = FindObjectsOfType<DefenseObject>(); 

        foreach (var obj in defenseObjects)
        {
            if (obj.currentHP < obj.maxHP * 0.5f)
            {
                repairTargets.Add(obj); 
                Debug.Log($"[GameManager] 수리 대상 추가: {obj.name}/ HP: {obj.currentHP}/{obj.maxHP}");
            }
        }
    }

    public List<DefenseObject> GetRepairTargetsNeedingRepair()
    {
        List<DefenseObject> needRepair = new List<DefenseObject>();
        foreach (var obj in repairTargets)
        {
            if (obj.currentHP < obj.maxHP * 0.5f)
                needRepair.Add(obj);
        }
        return needRepair;
    }

    public bool CanMarinerRepair(int marinerId, DefenseObject target)
    {
        return true;  
    }

    private void Update()
    {
        
    }
}
