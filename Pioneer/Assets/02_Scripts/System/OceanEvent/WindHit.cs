using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WindHit : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;
    private float lifeTime;
    private float airborneHeight;
    private float airborneDuration;
    private float stunDuration;

    private float lifeTimer = 0f;

    private readonly HashSet<CommonBase> processedTargets = new HashSet<CommonBase>();

    public void Initialize(Vector3 moveDirection,
                           float moveSpeed,
                           float lifeTime,
                           float airborneHeight,
                           float airborneDuration,
                           float stunDuration)
    {
        this.moveDirection = moveDirection.normalized;
        this.moveSpeed = moveSpeed;
        this.lifeTime = lifeTime;
        this.airborneHeight = airborneHeight;
        this.airborneDuration = airborneDuration;
        this.stunDuration = stunDuration;
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        CommonBase commonBase = other.GetComponentInParent<CommonBase>();
        if (commonBase == null) return;
        if (commonBase.IsDead) return;
        if (processedTargets.Contains(commonBase)) return;

        processedTargets.Add(commonBase);

        StructureBase structure = commonBase as StructureBase;
        if (structure != null)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(structure.maxHp * 0.05f));
            structure.TakeDamage(damage, null);
            return;
        }

        CreatureBase creature = commonBase as CreatureBase;
        if (creature != null)
        {
            WindAirborne windAirborne = creature.GetComponent<WindAirborne>();
            if (windAirborne == null)
                windAirborne = creature.gameObject.AddComponent<WindAirborne>();

            windAirborne.ApplyAirborne(airborneHeight, airborneDuration, moveDirection, 2f);

            StunHandler stunHandler = creature.GetComponent<StunHandler>();
            if (stunHandler != null)
            {
                stunHandler.ApplyStun(stunDuration);
            }
        }
    }
}