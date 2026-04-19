using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class StructureBase : CommonBase
{
    [Header("설치 데이터")]
    [SerializeField] protected SInstallableObjectDataSO objectData;
    public SInstallableObjectDataSO ObjectData => objectData;
    [field: SerializeField] public bool isUsing { get; private set; }
    [Header("플레이어 상호작용 범위")]
    [SerializeField] protected float interactRange = 3f;
    [Header("적 레이어")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected Collider[] detectedEnemies;
    private NavMeshSurface nav;
    protected virtual void Awake()
    {
        if (objectData != null)
        {
            maxHp = Mathf.Max(1, objectData.maxHp);
            hp = maxHp;
        }
        else
        {
            maxHp = Mathf.Max(1, maxHp);
            hp = Mathf.Clamp(hp, 0, maxHp);
        }
        nav = FindObjectOfType<NavMeshSurface>();
    }
    private void Update()
    {
        if (!isUsing) return;
    }
    private void LateUpdate()
    {
        if (CanInteract) PlayerInteract.Add(this);
    }
    #region HP 관리
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
    public virtual bool IsInteractionTarget => true;
    public virtual void Interactive() { }
    public virtual void Use()
    {
        Debug.Log(">> StructureBase.Use()");
        isUsing = true;
    }
    public virtual void UnUse() { isUsing = false; }
    public virtual bool CanInteract
    {
        get
        {
            if (ThisIsPlayer.Player == null)
                return false;
            return (transform.position - ThisIsPlayer.Player.transform.position).sqrMagnitude < interactRange * interactRange;
        }
    }
    #endregion
    public override void WhenDestroy()
    {
        Debug.LogError("\uC798 \uD30C\uAD34\uB410\uC5B4\uC6A9");
        DisableDestroyTargets();
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlatformLayoutChanged();
        if (nav == null)
            nav = FindObjectOfType<NavMeshSurface>();
        if (nav != null)
            nav.BuildNavMesh();
        base.WhenDestroy();
    }
    protected virtual void DisableDestroyTargets()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
        NavMeshObstacle[] obstacles = GetComponentsInChildren<NavMeshObstacle>(true);
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i] != null)
                obstacles[i].enabled = false;
        }
    }
#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, interactRange);
    }
#endif
}
