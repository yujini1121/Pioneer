using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    static public ItemDropManager instance;

    [SerializeField] GameObject prefabDroppedItemDefault;
    [SerializeField] float pickUpTime = 2f;

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
