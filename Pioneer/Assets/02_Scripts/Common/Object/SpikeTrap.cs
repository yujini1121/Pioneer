using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [SerializeField] private SInstallableObjectDataSO objectData;

    [Header("트랩 옵션")]
    [SerializeField] private float timeToStart;
    [SerializeField] private float duration;
    [SerializeField] private float timeToReset;
    [SerializeField] private float attackPower;
    [SerializeField] private float attackInterval;
    [SerializeField] private int howManyTime;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Vector3 hidePos;
    [SerializeField] private Vector3 triggeredPos;
    [SerializeField] private GameObject niddles;
    [SerializeField] private AnimationCurve curve;

    [Header("디버그")]
    [SerializeField] private int numberOfUses = 0;
    [SerializeField] private bool isTriggerd;

    private bool spikesRaised;
    private float nextScan;
    private Collider floor;
    private readonly Dictionary<CommonBase, float> nextHit = new Dictionary<CommonBase, float>();

    private void Awake()
    {
        floor = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (niddles != null) niddles.transform.localPosition = hidePos;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || Time.time < nextScan
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        nextScan = Time.time + 0.1f;
        // The installed trap has a solid floor collider, so trigger callbacks alone never fire.
        if (floor == null || floor.isTrigger) return;
        Bounds bounds = floor.bounds;
        Vector3 center = new Vector3(bounds.center.x, bounds.max.y + 1f, bounds.center.z);
        foreach (Collider hit in Physics.OverlapBox(center,
            new Vector3(bounds.extents.x, 1f, bounds.extents.z), Quaternion.identity, enemyLayer, QueryTriggerInteraction.Ignore))
            OnTriggerStay(hit);
    }

    private void OnTriggerStay(Collider other)
    {
        if (floor == null || floor.isTrigger
            || other == null || ((1 << other.gameObject.layer) & enemyLayer) == 0
            || Time.timeScale <= 0f || niddles == null) return;
        CommonBase target = other.GetComponentInParent<CommonBase>();
        if (target == null || target.IsDead) return;
        if (!isTriggerd) StartCoroutine(Trigger());
        if (!spikesRaised || (nextHit.TryGetValue(target, out float next) && Time.time < next)) return;
        nextHit[target] = Time.time + Mathf.Max(0.1f, attackInterval);
        target.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(attackPower)), gameObject);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isTriggerd = false;
        spikesRaised = false;
        nextHit.Clear();
        if (niddles != null) niddles.transform.localPosition = hidePos;
    }

    private IEnumerator Trigger()
    {
        isTriggerd = true;
        nextHit.Clear();

        //발동
        float elapsed = 0f;
        while (elapsed < timeToStart)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToStart);

            niddles.transform.localPosition = Vector3.Lerp(hidePos, triggeredPos, curve.Evaluate(t));
            yield return null;
        }
        niddles.transform.localPosition = triggeredPos;

        spikesRaised = true;

        //유지
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.ActivatedSpiketrap);

            UtilityFunctions.Log("따끔");
            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }

        spikesRaised = false;

        //종료
        elapsed = 0f;
        while (elapsed < timeToReset)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToReset);

            niddles.transform.localPosition = Vector3.Lerp(triggeredPos, hidePos, curve.Evaluate(t));
            yield return null;
        }
        niddles.transform.localPosition = hidePos;

        isTriggerd = false;
        numberOfUses++;

        if (numberOfUses >= howManyTime)
        {
            StructureBase structure = GetComponent<StructureBase>();
            if (structure != null) structure.WhenDestroy();
            else Destroy(gameObject);
        }
    }
}
