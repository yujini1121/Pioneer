using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Ballista : StructureBase, IBegin
{
    [Header("회전")]
    [SerializeField] private float rotationSpeed = 50f;

    [Header("발리스타 옵션")]
    [SerializeField] private float attackPower = 25f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackSpeed = 4f;
    [SerializeField] private Vector3 boltHalfSize = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] private Transform gunnerPos;
    [SerializeField] private Transform boltPool;

    [Header("디버그")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool enemyDetect;
    [SerializeField] private Collider[] colliders;
    [SerializeField] private Transform nearestTrans;

    private readonly List<GameObject> bolts = new List<GameObject>();
    private GameObject gunner;
    private PlayerController gunnerController;
    private Rigidbody gunnerRb;
    private float centerVecY;
    private int poolIndex = 0;
    private float curCooldown = 0f;
    private bool isDestroyed = false;
    private PlayerCore gunnerCore;

    protected override void Awake()
    {
        base.Awake();

        if (interactRange < 3.5f)
            interactRange = 3.5f;
    }

    private void Start()
    {
        var sc = GetComponent<SphereCollider>();
        centerVecY = sc ? sc.center.y : transform.position.y;

        bolts.Clear();
        if (boltPool != null)
        {
            for (int i = 0; i < boltPool.childCount; i++)
                bolts.Add(boltPool.GetChild(i).gameObject);
        }
    }

    private void Update()
    {
        if (isDestroyed || !isUsing)
            return;

        if (gunner == null)
        {
            UnUse();
            return;
        }

        Vector3 center = transform.position;
        center.y = centerVecY;

        colliders = Physics.OverlapSphere(center, attackRange, enemyLayer, QueryTriggerInteraction.Ignore);
        enemyDetect = colliders != null && colliders.Length > 0;

        if (!enemyDetect)
        {
            nearestTrans = null;
            return;
        }

        LookAt();
        Fire();
    }

    public override void Use()
    {
        if (isDestroyed || isUsing)
            return;

        GameObject player = ThisIsPlayer.Player;
        if (player == null)
            return;

        if (gunnerPos == null)
            gunnerPos = transform;

        base.Use();

        gunner = player;
        gunnerController = gunner.GetComponent<PlayerController>();
        gunnerCore = gunner.GetComponent<PlayerCore>();
        gunnerRb = gunner.GetComponent<Rigidbody>();

        if (gunnerController != null)
            gunnerController.enabled = false;

        if (gunnerRb != null)
        {
            gunnerRb.velocity = Vector3.zero;
            gunnerRb.angularVelocity = Vector3.zero;
        }

        gunner.transform.SetParent(gunnerPos);
        gunner.transform.localPosition = Vector3.zero;
        gunner.transform.localRotation = Quaternion.identity;

        curCooldown = 0f;
        nearestTrans = null;
        enemyDetect = false;
        colliders = null;

        SetGunnerIdleOnce();
    }

    public override void UnUse()
    {
        if (!isUsing)
            return;

        base.UnUse();
        ForceUnmount();

        nearestTrans = null;
        enemyDetect = false;
        colliders = null;
    }

    private void ForceUnmount()
    {
        if (gunner == null)
            return;

        gunner.transform.SetParent(null);

        if (gunnerPos != null)
        {
            gunner.transform.position = gunnerPos.position;
            gunner.transform.rotation = gunnerPos.rotation;
        }
        else
        {
            gunner.transform.position = transform.position;
        }

        if (gunnerController != null)
            gunnerController.enabled = true;

        if (gunnerRb != null)
        {
            gunnerRb.velocity = Vector3.zero;
            gunnerRb.angularVelocity = Vector3.zero;
        }

        gunner = null;
        gunnerController = null;
        gunnerCore = null;
        gunnerRb = null;
    }

    private void UpdateGunnerAimAnimation(Vector3 dir)
    {
        if (gunner == null || gunnerCore == null)
            return;

        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        if (gunnerController != null)
            gunnerController.lastMoveDirection = dir;

        gunnerCore.Idle(dir);
    }

    private void LookAt()
    {
        if (colliders == null || colliders.Length == 0)
            return;

        nearestTrans = colliders[0].transform;
        float minSqr = Mathf.Infinity;
        Vector3 selfPos = transform.position;

        foreach (var col in colliders)
        {
            if (col == null)
                continue;

            float d = (selfPos - col.transform.position).sqrMagnitude;
            if (d < minSqr)
            {
                minSqr = d;
                nearestTrans = col.transform;
            }
        }

        if (nearestTrans == null)
            return;

        Vector3 dir = nearestTrans.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        UpdateGunnerAimAnimation(dir);
    }

    private void Fire()
    {
        if (nearestTrans == null || bolts.Count == 0 || boltPool == null)
            return;

        if (curCooldown > 0f)
        {
            curCooldown -= Time.deltaTime;
            return;
        }

        curCooldown = attackCooldown;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.SFX.BalistaAttack);

        GameObject bolt = bolts[poolIndex];
        poolIndex = (poolIndex + 1) % bolts.Count;

        StartCoroutine(FireBolt(bolt, nearestTrans));
    }

    private IEnumerator FireBolt(GameObject bolt, Transform target)
    {
        if (bolt == null || boltPool == null)
            yield break;

        Transform boltTransform = bolt.transform;
        Vector3 firePosition = boltPool.position;
        Vector3 dir = target != null
            ? (target.position - firePosition).normalized
            : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            dir = transform.forward;

        boltTransform.SetParent(null);
        boltTransform.SetPositionAndRotation(firePosition, Quaternion.LookRotation(dir));
        bolt.SetActive(true);

        Vector3 prevPos = firePosition;
        float traveled = 0f;

        while (traveled < attackRange)
        {
            float step = attackSpeed * Time.deltaTime;
            Vector3 nextPos = boltTransform.position + dir * step;

            if (Physics.BoxCast(prevPos, boltHalfSize, dir, out RaycastHit hit, boltTransform.rotation, step, enemyLayer, QueryTriggerInteraction.Ignore))
            {
                CommonBase targetBase = hit.collider.GetComponentInParent<CommonBase>();
                if (targetBase == null)
                    targetBase = hit.collider.GetComponent<CommonBase>();

                if (targetBase != null)
                    targetBase.TakeDamage(Mathf.RoundToInt(attackPower), gameObject);

                break;
            }

            boltTransform.position = nextPos;
            traveled += step;
            prevPos = nextPos;
            yield return null;
        }

        boltTransform.SetParent(boltPool);
        boltTransform.localPosition = Vector3.zero;
        boltTransform.localRotation = Quaternion.identity;
        bolt.SetActive(false);
    }

    public override void WhenDestroy()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (isUsing)
            UnUse();
        else
            ForceUnmount();

        base.WhenDestroy();
    }

    private void SetGunnerIdleOnce()
    {
        if (gunner == null || gunnerCore == null)
            return;

        Vector3 dir = transform.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        if (gunnerController != null)
            gunnerController.lastMoveDirection = dir;

        gunnerCore.Idle(dir);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!drawGizmos)
            return;

        Vector3 center = GetComponent<Collider>() ? GetComponent<Collider>().bounds.center : transform.position;
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, Vector3.up, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + transform.forward * attackRange);
    }
#endif
}
