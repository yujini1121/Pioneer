using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TBallista : StructureBase
{
    [SerializeField] private bool isUsing = false;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private float attackRange;


    // TODO: 추후, ScriptableObject의 hp와 연동 필요함~~~
    private int currentHP = 80;


    #region 기본
    public override void Init()
    {
        
    }

    void Update()
    {
        
    }
    #endregion 

    void Use()
    {
        if (isUsing) return;
        isUsing = true;
    }

    bool Detect()
    {
        enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
        return true;
    }

    void LookAt()
    {
        
    }
    
    void Fire()
    {

    }
}
