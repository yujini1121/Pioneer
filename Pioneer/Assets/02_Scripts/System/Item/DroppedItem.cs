using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public bool isCanPickUp = false;
    public SItemStack itemValue;
    public Transform transformCanvas;
    public ItemSlotUI slotUI;
    [SerializeField] float pickUpRadius = 1.5f;
    [SerializeField] float collectDistance = 0.2f;
    [SerializeField] float attractSpeed = 10f;
    float pickUpTime = 0f;
    bool isAttracting = false;

    public void SetItem(SItemStack item, float pickUpTime)
    {
        UtilityFunctions.Log($">> DroppedItem.SetItem(SItemStack item) : 호출됨 / isItemNull : {item == null}");

        isCanPickUp = false;
        isAttracting = false;
        itemValue = item;
        if (slotUI != null) slotUI.Show(item);
        this.pickUpTime = pickUpTime;
        IEnumerator EnablePickUpAfterTime()
        {
            yield return new WaitForSeconds(pickUpTime);
            isCanPickUp = true;
        }
        if (pickUpTime >= 0f) StartCoroutine(EnablePickUpAfterTime());
    }

    public void FlyToDeck(Vector3 landing, float stagger)
    {
        StartCoroutine(FishingFlight(landing, stagger));
    }

    public void FlyToPlayer(Transform target, Vector3 sideways, float stagger)
    {
        ItemDropManager.FishingPickupSettings settings = ItemDropManager.instance != null
            ? ItemDropManager.instance.FishingPickup : ItemDropManager.FishingPickupSettings.Default;
        StartCoroutine(FishingToPlayer(target, sideways, stagger, settings));
    }

    private IEnumerator FishingToPlayer(Transform target, Vector3 sideways, float stagger, ItemDropManager.FishingPickupSettings settings)
    {
        isCanPickUp = false;
        bool slotWasActive = slotUI != null && slotUI.gameObject.activeSelf;
        if (slotWasActive) slotUI.gameObject.SetActive(false);
        if (stagger > 0f) yield return new WaitForSeconds(stagger);
        if (slotWasActive && slotUI != null) slotUI.gameObject.SetActive(true);

        Vector3 start = transform.position;
        Vector3 towardPlayer = target != null ? target.position - start : Vector3.zero;
        towardPlayer.y = 0f;
        Vector3 peak = start + towardPlayer.normalized * settings.popForwardDistance + Vector3.up * settings.popHeight;
        float elapsed = 0f;
        while (elapsed < settings.popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settings.popDuration);
            transform.position = Vector3.Lerp(start, peak, 1f - (1f - t) * (1f - t));
            yield return null;
        }

        if (settings.magnetDelay > 0f) yield return new WaitForSeconds(settings.magnetDelay);
        Vector3 control = peak + Vector3.up * settings.curveHeight + sideways * settings.curveSideOffset;
        elapsed = 0f;
        while (elapsed < settings.magnetDuration && target != null && target.gameObject.activeInHierarchy
            && (PlayerCore.Instance == null || !PlayerCore.Instance.IsDead)
            && (GameManager.Instance == null || !GameManager.Instance.IsGameResultActive))
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settings.magnetDuration);
            t *= t;
            Vector3 endpoint = target.position + settings.pickupOffset;
            transform.position = (1f - t) * (1f - t) * peak
                + 2f * (1f - t) * t * control + t * t * endpoint;
            yield return null;
        }

        if (elapsed >= settings.magnetDuration && target != null && target.gameObject.activeInHierarchy
            && InventoryManager.Instance != null
            && (PlayerCore.Instance == null || !PlayerCore.Instance.IsDead)
            && (GameManager.Instance == null || !GameManager.Instance.IsGameResultActive))
        {
            isCanPickUp = true;
            PickUp();
            yield break;
        }

        if (ItemDropManager.instance != null
            && ItemDropManager.instance.TryGetFishingFloor(transform.position, out RaycastHit floor))
            transform.position = floor.point + Vector3.up * 0.2f;
        isCanPickUp = true;
    }

    private IEnumerator FishingFlight(Vector3 landing, float stagger)
    {
        isCanPickUp = false;
        Vector3 start = transform.position;
        bool slotWasActive = slotUI != null && slotUI.gameObject.activeSelf;
        if (slotWasActive) slotUI.gameObject.SetActive(false);
        if (stagger > 0f) yield return new WaitForSeconds(stagger);
        if (slotWasActive && slotUI != null) slotUI.gameObject.SetActive(true);

        const float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (ItemDropManager.instance != null
                && !Physics.Raycast(landing + Vector3.up * 3f, Vector3.down, 8f,
                    LayerMask.GetMask("Platform"), QueryTriggerInteraction.Ignore)
                && ItemDropManager.instance.TryGetFishingFloor(landing, out RaycastHit floor))
                landing = floor.point + Vector3.up * 0.2f;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, landing, t) + Vector3.up * (4f * t * (1f - t));
            yield return null;
        }
        transform.position = landing;
        yield return new WaitForSeconds(0.3f);
        isCanPickUp = true;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (ThisIsPlayer.IsThisPlayer(collision) && isCanPickUp)
        {
            PickUp();
        }
    }

    private void Update()
    {
        if (isCanPickUp == false || ThisIsPlayer.Player == null)
        {
            return;
        }

        Transform playerTransform = ThisIsPlayer.Player.transform;

        if (isAttracting)
        {
            MoveToPlayer(playerTransform);
            return;
        }

        if (GetPlanarSqrDistance(playerTransform.position) <= pickUpRadius * pickUpRadius)
        {
            isAttracting = true;
        }
    }

    private void MoveToPlayer(Transform playerTransform)
    {
        Vector3 targetPosition = playerTransform.position;
        float lerp = 1f - Mathf.Exp(-attractSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, lerp);

        if (GetPlanarSqrDistance(targetPosition) <= collectDistance * collectDistance)
        {
            PickUp();
        }
    }

    private float GetPlanarSqrDistance(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    private void PickUp()
    {
        if (isCanPickUp == false || InventoryManager.Instance == null
            || (PlayerCore.Instance != null && PlayerCore.Instance.IsDead)
            || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive))
        {
            return;
        }

        isCanPickUp = false;

        int itemId = itemValue.id;
        bool isRepairCharge = itemId == 40007 && RepairSystem.instance != null;
        int before = isRepairCharge ? RepairSystem.instance.remainRepairCount : InventoryManager.Instance.Get(itemId);
        InventoryManager.Instance.Add(itemValue);
        InventoryManager.Instance.UpdateSlot();
        int after = isRepairCharge ? RepairSystem.instance.remainRepairCount : InventoryManager.Instance.Get(itemId);
        if (after > before) AudioManager.instance?.PlaySfx(AudioManager.SFX.ItemPickup);
        if (PlayerStatUI.Instance != null) PlayerStatUI.Instance.UpdateBasicStatUI();
        if (InventoryUiMain.instance != null) InventoryUiMain.instance.IconRefresh();

        Destroy(gameObject);
    }
}
