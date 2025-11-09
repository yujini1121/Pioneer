using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StructureBase : CommonBase
{
    [Header("설치 데이터")]
    [SerializeField] protected SInstallableObjectDataSO objectData;

    [field: SerializeField] public bool isUsing { get; private set; }

    [Header("플레이어 상호작용 감지")]
    [SerializeField] protected float interactRange = 1.5f;

    [Header("적 감지")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected Collider[] detectedEnemies;

    protected virtual void Awake()
    {
        if (objectData != null)
        {
            maxHp = Mathf.Max(1, objectData.maxHp); 
            hp = maxHp;
        }
        else
        {
            // 범위 보정
            maxHp = Mathf.Max(1, maxHp);
            hp = Mathf.Clamp(hp, 0, maxHp);
        }

    }

    private void Update()
    {
        if (!isUsing) return;
        // 전투/탐지는 자식에서 처리
    }

    #region HP 세팅
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Min(maxHp, hp + amount);
    }

    public void ResetHp()
    {
        hp = maxHp;
    }

    public virtual void ApplyData(SInstallableObjectDataSO data)
    {
        objectData = data;
        if (objectData != null)
        {
            maxHp = Mathf.Max(1, objectData.maxHp);
            hp = maxHp;
        }
    }
    #endregion

    #region 상호작용
    public virtual void Interactive() { }
    public virtual void Use() { isUsing = true; }
    public virtual void UnUse() { isUsing = false; }
    #endregion

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, interactRange);
    }
#endif
}