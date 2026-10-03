using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    static public ItemDropManager instance;

    [SerializeField] GameObject prefabDroppedItemDefault;
    [SerializeField] float pickUpTime = 2f;
    [SerializeField] private FishingPickupSettings fishingPickup = FishingPickupSettings.Default;
    private float fishingItemInterval = 0.08f;

    [System.Serializable]
    public struct FishingPickupSettings
    {
        [Min(0f)] public float spawnDistance;
        public float spawnSideOffset;
        public float waterHeightOffset;
        public float fallbackHeightOffset;
        [Min(0f)] public float spawnSpread;
        public Vector3 pickupOffset;
        [Min(0f)] public float startDelay;
        [Min(0f)] public float itemInterval;
        [Min(0.01f)] public float popDuration;
        [Min(0f)] public float popHeight;
        [Min(0f)] public float popForwardDistance;
        [Min(0f)] public float magnetDelay;
        [Min(0.01f)] public float magnetDuration;
        public float curveHeight;
        public float curveSideOffset;

        public static FishingPickupSettings Default => new FishingPickupSettings
        {
            spawnDistance = 1.5f,
            waterHeightOffset = 0.05f,
            fallbackHeightOffset = -0.35f,
            spawnSpread = 0.25f,
            pickupOffset = new Vector3(0f, 0.85f, 0f),
            itemInterval = 0.08f,
            popDuration = 0.22f,
            popHeight = 0.65f,
            popForwardDistance = 0.15f,
            magnetDuration = 0.25f,
            curveHeight = 0.45f,
            curveSideOffset = 0.3f
        };

        public FishingPickupSettings Validated()
        {
            FishingPickupSettings value = this;
            value.spawnDistance = Mathf.Max(0f, value.spawnDistance);
            value.spawnSpread = Mathf.Max(0f, value.spawnSpread);
            value.startDelay = Mathf.Max(0f, value.startDelay);
            value.itemInterval = Mathf.Max(0f, value.itemInterval);
            value.popDuration = Mathf.Max(0.01f, value.popDuration);
            value.popHeight = Mathf.Max(0f, value.popHeight);
            value.popForwardDistance = Mathf.Max(0f, value.popForwardDistance);
            value.magnetDelay = Mathf.Max(0f, value.magnetDelay);
            value.magnetDuration = Mathf.Max(0.01f, value.magnetDuration);
            return value;
        }
    }

    public FishingPickupSettings FishingPickup => fishingPickup.Validated();

    private void OnValidate()
    {
        fishingPickup = fishingPickup.Validated();
    }

    public void Drop(SItemStack target, Vector3 worldPosition)
    {
        CreateDrop(target, worldPosition, pickUpTime);
    }

    private DroppedItem CreateDrop(SItemStack target, Vector3 position, float delay)
    {
        if (prefabDroppedItemDefault == null || SItemStack.IsEmpty(target) || target.amount <= 0) return null;
        GameObject itemObject = Instantiate(prefabDroppedItemDefault, position, quaternion.identity);
        DroppedItem item = itemObject.GetComponent<DroppedItem>();
        if (item == null)
        {
            Destroy(itemObject);
            Debug.LogError("DroppedItem component is missing on the drop prefab.");
            return null;
        }
        item.SetItem(target, delay);
        return item;
    }

    public int CatchFishing(SItemStack target, Transform player, Vector3 seaDirection, int firstIndex = 0)
    {
        if (player == null || SItemStack.IsEmpty(target) || target.amount <= 0) return 0;
        seaDirection.y = 0f;
        if (seaDirection.sqrMagnitude < 0.001f) return 0;
        seaDirection.Normalize();
        FishingPickupSettings settings = FishingPickup;
        if (firstIndex <= 0) fishingItemInterval = UnityEngine.Random.Range(0.08f, 0.25f);
        Vector3 sideways = Vector3.Cross(Vector3.up, seaDirection);
        Vector3 seaPosition = player.position + seaDirection * settings.spawnDistance
            + sideways * settings.spawnSideOffset;
        int mask = LayerMask.GetMask("Platform");
        for (int step = 0; step < 12; step++)
        {
            if (!Physics.Raycast(seaPosition + Vector3.up * 3f, Vector3.down, 8f,
                mask, QueryTriggerInteraction.Ignore)) break;
            seaPosition += seaDirection * 0.25f;
        }
        seaPosition.y = player.position.y + settings.fallbackHeightOffset;
        if (Physics.Raycast(seaPosition + Vector3.up * 3f, Vector3.down, out RaycastHit water,
            8f, LayerMask.GetMask("Water"), QueryTriggerInteraction.Collide))
            seaPosition.y = water.point.y + settings.waterHeightOffset;

        int visualCount = Mathf.Min(target.amount, Mathf.Max(1, 8 - firstIndex));
        for (int i = 0; i < visualCount; i++)
        {
            int index = firstIndex + i;
            float scatter = ((index % 3) - 1) * settings.spawnSpread;
            int amount = i == visualCount - 1 ? target.amount - i : 1;
            DroppedItem item = CreateDrop(new SItemStack(target.id, amount, target.duability),
                seaPosition + sideways * scatter, -1f);
            if (item != null) item.FlyToPlayer(player, sideways * ((index % 2) == 0 ? 1f : -1f), settings.startDelay + index * fishingItemInterval);
        }
        return visualCount;
    }

    public int DropFishing(SItemStack target, Vector3 playerPosition, Vector3 seaDirection, int firstIndex = 0)
    {
        if (SItemStack.IsEmpty(target) || target.amount <= 0) return 0;
        seaDirection.y = 0f;
        if (seaDirection.sqrMagnitude < 0.001f) return 0;
        seaDirection.Normalize();
        int platformMask = LayerMask.GetMask("Platform");
        if (!TryGetFishingFloor(playerPosition, out RaycastHit floor)) return 0;

        Bounds deckBounds = floor.collider.bounds;
        Vector3 seaPosition = playerPosition + seaDirection * 1.5f;
        for (int step = 0; step < 12; step++)
        {
            if (!Physics.Raycast(seaPosition + Vector3.up * 3f, Vector3.down, 8f,
                platformMask, QueryTriggerInteraction.Ignore)) break;
            seaPosition += seaDirection * 0.25f;
        }
        seaPosition.y = floor.point.y - 0.35f;
        if (Physics.Raycast(seaPosition + Vector3.up * 3f, Vector3.down, out RaycastHit water,
            8f, LayerMask.GetMask("Water"), QueryTriggerInteraction.Collide))
            seaPosition.y = water.point.y + 0.05f;

        Vector3 sideways = Vector3.Cross(Vector3.up, seaDirection);
        int visualCount = Mathf.Min(target.amount, 8);
        for (int i = 0; i < visualCount; i++)
        {
            int index = firstIndex + i;
            float scatter = ((index % 3) - 1) * 0.3f;
            Vector3 landing = playerPosition - seaDirection * 0.4f;
            float marginX = Mathf.Min(0.25f + Mathf.Abs(sideways.x) * 0.3f, deckBounds.extents.x);
            float marginZ = Mathf.Min(0.25f + Mathf.Abs(sideways.z) * 0.3f, deckBounds.extents.z);
            landing.x = Mathf.Clamp(landing.x, deckBounds.min.x + marginX, deckBounds.max.x - marginX);
            landing.z = Mathf.Clamp(landing.z, deckBounds.min.z + marginZ, deckBounds.max.z - marginZ);
            landing += sideways * scatter - seaDirection * ((index / 3) * 0.25f);
            landing.x = Mathf.Clamp(landing.x, deckBounds.min.x + 0.2f, deckBounds.max.x - 0.2f);
            landing.z = Mathf.Clamp(landing.z, deckBounds.min.z + 0.2f, deckBounds.max.z - 0.2f);
            landing.y = deckBounds.max.y;
            if (Physics.Raycast(landing + Vector3.up * 3f, Vector3.down, out RaycastHit surface,
                8f, platformMask, QueryTriggerInteraction.Ignore))
                landing = surface.point;
            else
                landing = floor.point;
            landing.y += 0.2f;

            int amount = i == visualCount - 1 ? target.amount - i : 1;
            DroppedItem item = CreateDrop(new SItemStack(target.id, amount, target.duability), seaPosition, -1f);
            if (item != null) item.FlyToDeck(landing, index * 0.11f);
        }
        return visualCount;
    }

    public bool TryGetFishingFloor(Vector3 position, out RaycastHit floor)
    {
        int mask = LayerMask.GetMask("Platform");
        if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out floor,
            8f, mask, QueryTriggerInteraction.Ignore)) return true;

        float nearest = float.MaxValue;
        bool found = false;
        foreach (ItemDeck deck in FindObjectsOfType<ItemDeck>())
        {
            if (deck.IsDead || !deck.TryGetComponent(out Collider collider) || !collider.enabled) continue;
            Bounds bounds = collider.bounds;
            Vector3 point = position;
            float marginX = Mathf.Min(0.25f, bounds.extents.x);
            float marginZ = Mathf.Min(0.25f, bounds.extents.z);
            point.x = Mathf.Clamp(point.x, bounds.min.x + marginX, bounds.max.x - marginX);
            point.z = Mathf.Clamp(point.z, bounds.min.z + marginZ, bounds.max.z - marginZ);
            point.y = bounds.max.y;
            float distance = (point - position).sqrMagnitude;
            if (distance >= nearest || !Physics.Raycast(point + Vector3.up * 3f, Vector3.down,
                out RaycastHit candidate, 8f, mask, QueryTriggerInteraction.Ignore)) continue;
            floor = candidate;
            nearest = distance;
            found = true;
        }
        return found;
    }

    private void Awake()
    {
        instance = this;
    }
}
